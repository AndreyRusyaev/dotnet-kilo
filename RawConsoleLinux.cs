using System.Runtime.InteropServices;
using System.Text;

internal static class RawConsoleLinux
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
        termios.c_iflag &= (uint)(~(Libc.InputFlags.IGNBRK | Libc.InputFlags.BRKINT | Libc.InputFlags.PARMRK
            | Libc.InputFlags.ISTRIP | Libc.InputFlags.INLCR | Libc.InputFlags.IGNCR | Libc.InputFlags.ICRNL | Libc.InputFlags.IXON));
        // output modes: disable post processing
        termios.c_oflag &= (uint)(~Libc.OutputFlags.OPOST);
        // local modes: echo off, echo nl off, canonical (kill, erase, etc) off, no signal chars (^Z,^C), no extended functions
        termios.c_lflag &= (uint)(~(Libc.LocalFlags.ECHO | Libc.LocalFlags.ECHONL | Libc.LocalFlags.ICANON
            | Libc.LocalFlags.ISIG | Libc.LocalFlags.IEXTEN));
        // control modes: clear size bit, parity off
        termios.c_cflag &= (uint)(~(Libc.ControlFlags.CSIZE | Libc.ControlFlags.PARENB));
        // control modes: set 8 bit chars
        termios.c_cflag |= (uint)(Libc.ControlFlags.CS8);

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

        public const int NCCS = 32;

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

            public uint c_iflag;      /* input modes */
            public uint c_oflag;      /* output modes */
            public uint c_cflag;      /* control modes */
            public uint c_lflag;      /* local modes */

            public byte c_line;	     /* line discipline */

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NCCS)]
            public byte[] c_cc;      /* special characters */

            public uint c_ispeed;     /* input speed */
            public uint c_ospeed;     /* output speed */
        }

        /// <summary>
        /// Linux\general flags sysdeps\unix\sysv\linux\bits\termios-c_iflag.h
        /// </summary>
        [Flags]
        public enum InputFlags : uint
        {
            IGNBRK = 1 << 0, /* Ignore break condition.  */
            BRKINT = 1 << 1, /* Signal interrupt on break.  */
            IGNPAR = 1 << 2, /* Ignore characters with parity errors.  */
            PARMRK = 1 << 3, /* Mark parity and framing errors.  */
            INPCK = 1 << 4, /* Enable input parity check.  */
            ISTRIP = 1 << 5, /* Strip 8th bit off characters.  */
            INLCR = 1 << 6, /* Map NL to CR on input.  */
            IGNCR = 1 << 7, /* Ignore CR.  */
            ICRNL = 1 << 8, /* Map CR to NL on input.  */
            IUCLC = 1 << 9, /* Map uppercase characters to lowercase on input (not in POSIX).  */
            IXON = 1 << 10, /* Enable start/stop output control.  */
            IXANY = 1 << 11, /* Enable any character to restart output.  */
            IXOFF = 1 << 12, /* Enable start/stop input control.  */
            IMAXBEL = 1 << 13, /* Ring bell when input queue is full (not in POSIX).  */
            IUTF8 = 1 << 14, /* Input is UTF8 (not in POSIX).  */
        }

        /// <summary>
        /// Linux\general flags sysdeps\unix\sysv\linux\bits\termios-c_oflag.h
        /// </summary>
        [Flags]
        public enum OutputFlags : uint
        {
            OPOST = 1 << 0, /* Post-process output.  */
            OLCUC = 1 << 1, /* Map lowercase characters to uppercase on out (= in POSIX).  */
            ONLCR = 1 << 2, /* Map NL to CR-NL on output.  */
            OCRNL = 1 << 3, /* Map CR to NL on output.  */
            ONOCR = 1 << 4, /* No CR output at column 0.  */
            ONLRE = 1 << 5, /* NL performs CR function.  */
            OFILL = 1 << 6, /* Use fill characters for delay.  */
            OFDEL = 1 << 7, /* Fill is DEL.  */
            NLDLY = 1 << 8,  /* Select newline delays:  */
            NL0 = 0,        /* Newline type 0.  */
            NL1 = 1 << 8,   /* Newline type 1.  */
            CRDLY = CR0 | CR1 | CR2,  /* Select carriage-return delays:  */
            CR0 = 0,        /* Carriage-return delay type 0.  */
            CR1 = 1 << 9,   /* Carriage-return delay type 1.  */
            CR2 = 1 << 10,  /* Carriage-return delay type 2.  */
            CR3 = CR1 | CR2,/* Carriage-return delay type 3.  */
            TABDLY = TAB0 | TAB2 | TAB3,  /* Select horizontal-tab delays:  */
            TAB0 = 0,  /* Horizontal-tab delay type 0.  */
            TAB1 = 1 << 11,  /* Horizontal-tab delay type 1.  */
            TAB2 = 1 << 12,  /* Horizontal-tab delay type 2.  */
            TAB3 = TAB1 | TAB2,  /* Expand tabs to spaces.  */
            BSDLY = 1 << 13, /* Select backspace delays:  */
            BS0 = 0, /* Backspace-delay type 0.  */
            BS1 = 1 << 13, /* Backspace-delay type 1.  */
            FFDLY = 1 << 15, /* Select form-feed delays:  */
            FF0 = 0, /* Form-feed delay type 0.  */
            FF1 = 1 << 15, /* Form-feed delay type 1.  */
            VTDLY = 1 << 14, /* Select vertical-tab delays:  */
            VT0 = 0, /* Vertical-tab delay type 0.  */
            VT1 = 1 << 14, /* Vertical-tab delay type 1.  */
            XTABS = TAB3
        }

        /// <summary>
        /// Linux\general flags sysdeps\unix\sysv\linux\bits\termios-c_cflag.h
        /// </summary>
        [Flags]
        public enum ControlFlags : uint
        {
            CSIZE = CS5 | CS6 | CS7 | CS8,  /* Number of bits per byte (mask).  */
            CS5 = 0,            /* 5 bits per byte.  */
            CS6 = 1 << 4,       /* 6 bits per byte.  */
            CS7 = 1 << 5,       /* 7 bits per byte.  */
            CS8 = CS6 | CS7,    /* 8 bits per byte.  */
            CSTOPB = 1 << 6,    /* Two stop bits instead of one.  */
            CREAD = 1 << 7,     /* Enable receiver.  */
            PARENB = 1 << 8,    /* Parity enable.  */
            PARODD = 1 << 9,    /* Odd parity instead of even.  */
            HUPCL = 1 << 10,    /* Hang up on last close.  */
            CLOCAL = 1 << 11,   /* Ignore modem status lines.  */
        }

        /// <summary>
        /// Linux\general flags sysdeps\unix\sysv\linux\bits\termios-c_lflag.h
        /// </summary>
        [Flags]
        public enum LocalFlags : uint
        {
            ISIG = (1 << 0), /* Enable signals.  */
            ICANON = (1 << 1), /* Canonical input (erase and kill processing).  */
            XCASE = (1 << 2),
            ECHO = (1 << 3), /* Enable echo.  */
            ECHOE = (1 << 4), /* Echo erase character as error-correcting backspace.  */
            ECHOK = (1 << 5), /* Echo KILL.  */
            ECHONL = (1 << 6), /* Echo NL.  */
            NOFLSH = (1 << 7), /* Disable flush after interrupt or quit.  */
            TOSTOP = (1 << 8), /* Send SIGTTOU for background output.  */
            ECHOCTL = (1 << 9), /* If ECHO is also set, terminal special characters
                                other than TAB, NL, START, and STOP are echoed as
                                ^X, where X is the character with ASCII code 0x40
                                greater than the special character
                                (not in POSIX).  */
            ECHOPRT = (1 << 10), /* If ICANON and ECHO are also set, characters are
                                printed as they are being erased
                                (not in POSIX).  */
            ECHOKE = (1 << 11), /* If ICANON is also set, KILL is echoed by erasing
                                each character on the line, as specified by ECHOE
                                and ECHOPRT (not in POSIX).  */
            FLUSHO = (1 << 12), /* Output is being flushed.  This flag is toggled by
                                typing the DISCARD character (not in POSIX).  */
            PENDIN = (1 << 14), /* All characters in the input queue are reprinted
                                when the next character is read
                                (not in POSIX).  */
            IEXTEN = (1 << 15), /* Enable implementation-defined input processing.  */
            EXTPROC = (1 << 16)
        }

        public enum ControlCharacters : byte
        {
            VINTR = 0,  /* Interrupt character [ISIG].  */
            VQUIT = 1,  /* Quit character [ISIG].  */
            VERASE = 2, /* Erase character [ICANON].  */
            VKILL = 3,  /* Kill-line character [ICANON].  */
            VEOF = 4,   /* End-of-file character [ICANON].  */
            VTIME = 5,  /* Time-out value (tenths of a second) [!ICANON].  */
            VMIN = 6,   /* Minimum number of bytes read at once [!ICANON].  */
            VSWTC = 7,  /* Switch character (SWTCH).  Used in System V to switch shells in shell layers, a predecessor to shell job control.*/
            VSTART = 8, /* Start (X-ON) character [IXON, IXOFF].  */
            VSTOP = 9,  /* Stop (X-OFF) character [IXON, IXOFF].  */
            VSUSP = 10, /* Suspend character [ISIG].  */
            VEOL = 11,  /* End-of-line character [ICANON].  */
            VREPRINT = 12,  /* Reprint-line character [ICANON].  */
            VDISCARD = 13,  /* Discard character [IEXTEN].  */
            VWERASE = 14,   /* Word-erase character [ICANON].  */
            VLNEXT = 15,    /* Literal-next character [IEXTEN].  */
            VEOL2 = 16      /* Second EOL character [ICANON].  */
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