using System.Runtime.InteropServices;
using System.Text;

internal static class RawConsoleMac
{
    private const int STDIN_FILENO = 0;

    private static bool isRawModeEnabled = false;

    private static Libc.Termios originalTermios;

    private static StdinReader stdinReader = new StdinReader(Console.InputEncoding);

    private static Action atExitDelegate = new Action(DisableRawMode);

    public static char? ReadKey()
    {
        return stdinReader.ReadChar();
    }

    public static void EnableRawMode()
    {
        if (isRawModeEnabled)
        {
            // Already enabled. Skip.
            return;
        }

        // read current terminal mode
        if (Libc.tcgetattr(STDIN_FILENO, ref originalTermios) == -1)
        {
            throw new Exception($"Failed to call tcgetattr. Error code: {Marshal.GetLastSystemError()}.");
        }

        if (Libc.atexit(Marshal.GetFunctionPointerForDelegate(atExitDelegate)) != 0)
        {
            throw new Exception($"Failed to register exit function (atexit). Error code: {Marshal.GetLastSystemError()}.");
        }

        Libc.Termios modTermios = originalTermios;

        MakeRawMode(ref modTermios);

        // Modify blocking parameters for terminal raw mode:
        // return 1 byte or nothing with 100ms timeout (the same way it's done in original kilo).
        modTermios.c_cc[(int)Libc.ControlCharacters.VMIN] = 0;  /* Return each byte, or zero for timeout. */
        modTermios.c_cc[(int)Libc.ControlCharacters.VTIME] = 1; /* 100 ms timeout (unit is tens of second). */

        // put terminal in raw mode after flushing
        if (Libc.tcsetattr(STDIN_FILENO, (int)Libc.OptionalActions.TCSAFLUSH, ref modTermios) == -1)
        {
            throw new Exception($"Failed to call tcsetattr. Error code: {Marshal.GetLastSystemError()}");
        }

        isRawModeEnabled = true;
    }

    public static void DisableRawMode()
    {
        if (!isRawModeEnabled)
        {
            // Wasn't enabled. Skip.
            return;
        }

        if (Libc.tcsetattr(STDIN_FILENO, (int)Libc.OptionalActions.TCSAFLUSH, ref originalTermios) == -1)
        {
            throw new Exception($"Failed to call tcsetattr. Error code: {Marshal.GetLastSystemError()}");
        }

        isRawModeEnabled = false;
    }

    private static void MakeRawMode(ref Libc.Termios termios)
    {
        Libc.cfmakeraw(ref termios);
    }

    private static void MakeRawModeInline(ref Libc.Termios termios)
    {
        // input modes: no break, no interrupt on break, ignore parity and framing errors, 
        // no strip char, no NL to CR, do not ignore CR, no CR to NL, no start/stop output control.
        termios.c_iflag &= (ulong)(~(Libc.InputFlags.IGNBRK | Libc.InputFlags.BRKINT | Libc.InputFlags.PARMRK
            | Libc.InputFlags.ISTRIP | Libc.InputFlags.INLCR | Libc.InputFlags.IGNCR | Libc.InputFlags.ICRNL | Libc.InputFlags.IXON));
        // output modes: disable post processing
        termios.c_oflag &= (ulong)(~Libc.OutputFlags.OPOST);
        // local modes: echo off, echo nl off, canonical (kill, erase, etc) off, no signal chars (^Z,^C), no extended functions
        termios.c_lflag &= (ulong)(~(Libc.LocalFlags.ECHO | Libc.LocalFlags.ECHONL | Libc.LocalFlags.ICANON
            | Libc.LocalFlags.ISIG | Libc.LocalFlags.IEXTEN));
        // control modes: clear size bit, parity off
        termios.c_cflag &= (ulong)(~(Libc.ControlFlags.CSIZE | Libc.ControlFlags.PARENB));
        // control modes: set 8 bit chars
        termios.c_cflag |= (ulong)(Libc.ControlFlags.CS8);

        termios.c_cc[(int)Libc.ControlCharacters.VMIN] = 1; // 1 character
        termios.c_cc[(int)Libc.ControlCharacters.VTIME] = 0; // infinite timeout
    }

    /// <summary>
    /// Allows to read chars from stdin in the specified encoding.
    /// </summary>
    internal class StdinReader
    {
        private const int STDIN_FILENO = 0;

        private const int EAGAIN = 11;

        private const int BytesToBeRead = 1024;

        private readonly Encoding encoding;

        private readonly byte[] bytesBufferToBeRead;

        private readonly char[] unprocessedBuffer;

        private int unprocessedBufferStartIndex;

        private int unprocessedBufferEndIndex;

        public StdinReader(Encoding encoding)
        {
            this.encoding = encoding;

            bytesBufferToBeRead = new byte[BytesToBeRead];
            unprocessedBuffer = new char[encoding.GetMaxCharCount(BytesToBeRead)];
            unprocessedBufferStartIndex = 0;
            unprocessedBufferEndIndex = 0;
        }

        public char? ReadChar()
        {
            if (unprocessedBufferStartIndex >= unprocessedBufferEndIndex)
            {
                while (true)
                {
                    int bytesRead = Libc.read(STDIN_FILENO, bytesBufferToBeRead, BytesToBeRead);
                    if (bytesRead == -1)
                    {
                        var errorCode = Marshal.GetLastSystemError();
                        if (errorCode != EAGAIN)
                        {
                            throw new Exception($"read failed. Error code: {errorCode}.");
                        }

                        continue;
                    }

                    if (bytesRead > 0)
                    {
                        unprocessedBufferStartIndex = 0;
                        unprocessedBufferEndIndex = encoding.GetChars(bytesBufferToBeRead, 0, bytesRead, unprocessedBuffer, 0);
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        return null;
                    }
                }
            }

            return unprocessedBuffer[unprocessedBufferStartIndex++];
        }
    }

    internal static class Libc
    {
        // By some reasons libc does not export atexit function
        // https://stackoverflow.com/questions/43825971/call-atexit-when-linking-to-libc-dynamically-on-linux
        [DllImport("libc", EntryPoint = "__cxa_atexit")]
        public static extern int atexit(IntPtr function);

        [DllImport("libc", SetLastError = true)]
        public static extern int tcgetattr(int fd, ref Termios termios);

        [DllImport("libc", SetLastError = true)]
        public static extern int tcsetattr(int fd, int optional_actions, ref Termios termios);

        /// <summary>
        /// cfmakeraw()  sets  the  terminal  to something like the "raw" mode of the old Version 7 terminal driver: input is available
        /// character by character, echoing is disabled, and all special processing of terminal input and  output  characters  is  disabled.
        /// The terminal attributes are set as follows:
        // termios_p->c_iflag &= ~(IGNBRK | BRKINT | PARMRK | ISTRIP | INLCR | IGNCR | ICRNL | IXON);
        // termios_p->c_oflag &= ~OPOST;
        // termios_p->c_lflag &= ~(ECHO | ECHONL | ICANON | ISIG | IEXTEN);
        // termios_p->c_cflag &= ~(CSIZE | PARENB);
        // termios_p->c_cflag |= CS8;
        /// </summary>
        /// <param name="termios"></param>
        [DllImport("libc")]
        public static extern void cfmakeraw(ref Termios termios);

        [DllImport("libc", SetLastError = true)]
        public static extern int read(int fd, byte[] buf, int count);

        public const int NCCS = 20;

        /// <summary>
        /// TODO: This struct layout is platform dependent
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Termios
        {
            public Termios()
            {
                c_cc = new byte[NCCS];
            }

            public ulong c_iflag;      /* input modes */
            public ulong c_oflag;      /* output modes */
            public ulong c_cflag;      /* control modes */
            public ulong c_lflag;      /* local modes */

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NCCS)]
            public byte[] c_cc;      /* special characters */

            public ulong c_ispeed;     /* input speed */
            public ulong c_ospeed;     /* output speed */
        }

        /// <summary>
        /// Linux\general flags sysdeps\unix\sysv\linux\bits\termios-c_iflag.h
        /// </summary>
        [Flags]
        public enum InputFlags : ulong
        {
            IGNBRK =         0x00000001,      /* ignore BREAK condition */
            BRKINT =         0x00000002,      /* map BREAK to SIGINTR */
            IGNPAR =         0x00000004,      /* ignore (discard) parity errors */
            PARMRK =         0x00000008,      /* mark parity and framing errors */
            INPCK  =         0x00000010,      /* enable checking of parity errors */
            ISTRIP =         0x00000020,      /* strip 8th bit off chars */
            INLCR  =         0x00000040,      /* map NL into CR */
            IGNCR  =         0x00000080,      /* ignore CR */
            ICRNL  =         0x00000100,      /* map CR to NL (ala CRMOD) */
            IXON   =         0x00000200,      /* enable output flow control */
            IXOFF  =         0x00000400,      /* enable input flow control */
            IXANY  =         0x00000800,      /* any char will restart after stop */
            IMAXBEL=         0x00002000,      /* ring bell on input queue full */
            IUTF8  =         0x00004000,      /* maintain state for UTF-8 VERASE */
        }

        /// <summary>
        /// Linux\general flags sysdeps\unix\sysv\linux\bits\termios-c_oflag.h
        /// </summary>
        [Flags]
        public enum OutputFlags : ulong
        {
            OPOST =          0x00000001,      /* enable following output processing */
            ONLCR =          0x00000002,      /* map NL to CR-NL (ala CRMOD) */

            OXTABS =         0x00000004,      /* expand tabs to spaces */
            ONOEOT =         0x00000008,      /* discard EOT's (^D) on output) */
        }

        /// <summary>
        /// Linux\general flags sysdeps\unix\sysv\linux\bits\termios-c_cflag.h
        /// </summary>
        [Flags]
        public enum ControlFlags : ulong
        {
            CIGNORE = 0x00000001,     /* ignore control flags */
            CSIZE = 0x00000300,      /* character size mask */
            CS5 = 0x00000000,      /* 5 bits (pseudo) */
            CS6 = 0x00000100,      /* 6 bits */
            CS7 = 0x00000200,      /* 7 bits */
            CS8 = 0x00000300,      /* 8 bits */
            CSTOPB          = 0x00000400,      /* send 2 stop bits */
            CREAD           = 0x00000800,      /* enable receiver */
            PARENB          = 0x00001000,      /* parity enable */
            PARODD          = 0x00002000,      /* odd parity, else even */
            HUPCL           = 0x00004000,      /* hang up on last close */
            CLOCAL          = 0x00008000,      /* ignore modem status lines */
            CCTS_OFLOW      = 0x00010000,      /* CTS flow control of output */
            CRTSCTS         = (CCTS_OFLOW | CRTS_IFLOW),
            CRTS_IFLOW      = 0x00020000,      /* RTS flow control of input */
            CDTR_IFLOW      = 0x00040000,      /* DTR flow control of input */
            CDSR_OFLOW      = 0x00080000,      /* DSR flow control of output */
            CCAR_OFLOW      = 0x00100000,      /* DCD flow control of output */
            MDMBUF          = 0x00100000,      /* old name for CCAR_OFLOW */
        }

        /// <summary>
        /// Linux\general flags sysdeps\unix\sysv\linux\bits\termios-c_lflag.h
        /// </summary>
        [Flags]
        public enum LocalFlags : ulong
        {
            ECHOKE         = 0x00000001,     /* visual erase for line kill */
            ECHOE          = 0x00000002,     /* visually erase chars */
            ECHOK          = 0x00000004,     /* echo NL after line kill */
            ECHO           = 0x00000008,     /* enable echoing */
            ECHONL         = 0x00000010,     /* echo NL even if ECHO is off */
            ECHOPRT        = 0x00000020,     /* visual erase mode for hardcopy */
            ECHOCTL        = 0x00000040,     /* echo control chars as ^(Char) */
            ISIG           = 0x00000080,     /* enable signals INTR, QUIT, [D]SUSP */
            ICANON         = 0x00000100,     /* canonicalize input lines */
            ALTWERASE      = 0x00000200,     /* use alternate WERASE algorithm */
            IEXTEN         = 0x00000400,     /* enable DISCARD and LNEXT */
            EXTPROC        = 0x00000800,     /* external processing */
            TOSTOP         = 0x00400000,     /* stop background jobs from output */
            FLUSHO         = 0x00800000,     /* output being flushed (state) */
            NOKERNINFO     = 0x02000000,     /* no kernel output from VSTATUS */
            PENDIN         = 0x20000000,     /* XXX retype pending input (state) */
            NOFLSH         = 0x80000000,     /* don't flush after interrupt */
        }

        public enum ControlCharacters : byte
        {
            VEOF = 0,       /* ICANON */
            VEOL = 1,       /* ICANON */
            VEOL2 = 2,      /* ICANON together with IEXTEN */
            VERASE = 3,     /* ICANON */
            VWERASE = 4,    /* ICANON together with IEXTEN */
            VKILL = 5,      /* ICANON */
            VREPRINT = 6,   /* ICANON together with IEXTEN */
            VINTR = 8,      /* ISIG */
            VQUIT = 9,      /* ISIG */
            VSUSP = 10,     /* ISIG */
            VDSUSP = 11,    /* ISIG together with IEXTEN */
            VSTART = 12,    /* IXON, IXOFF */
            VSTOP = 13,     /* IXON, IXOFF */
            VLNEXT = 14,    /* IEXTEN */
            VDISCARD = 15,  /* IEXTEN */
            VMIN = 16,      /* !ICANON */
            VTIME = 17,     /* !ICANON */
            VSTATUS = 18,   /* ICANON together with IEXTEN */
        }

        public enum OptionalActions
        {
            /* Change immediately.  */
            TCSANOW = 0,
            /* Change when pending output is written.  */
            TCSADRAIN = 1,
            /* Flush pending input before changing.  */
            TCSAFLUSH = 2,
            /* Flag: Don't alter hardware state.  */
            TCSASOFT = 0x10
        }
    }
}