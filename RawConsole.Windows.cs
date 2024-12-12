using System.ComponentModel;
using System.Runtime.InteropServices;

internal static partial class RawConsole
{
    private static bool isRawModeWindowsEnabled = false;

    private static int originalStdInMode;

    private static int originalStdOutMode;

    private static void EnableRawModeWindows()
    {
        if (isRawModeWindowsEnabled)
        {
            // Already enabled. Skip.
            return;
        }

        var stdIn = Kernel32.GetStdHandle(Kernel32.STD_INPUT_HANDLE);
        var stdOut = Kernel32.GetStdHandle(Kernel32.STD_OUTPUT_HANDLE);

        if (!Kernel32.GetConsoleMode(stdIn, out originalStdInMode))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!Kernel32.GetConsoleMode(stdOut, out originalStdOutMode))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var originalInputMode = (Kernel32.ConsoleInputModes)originalStdInMode;
        var originalOutputMode = (Kernel32.ConsoleOutputModes)originalStdOutMode;

        var newInputMode = originalInputMode;
        var newOutputMode = originalOutputMode;

        MakeRawInputMode(ref newInputMode);
        MakeRawOutputMode(ref newOutputMode);

        if (!Kernel32.SetConsoleMode(stdIn, (int)newInputMode))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set stdin console mode.");
        }

        if (!Kernel32.SetConsoleMode(stdOut, (int)newOutputMode))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set stdout console mode.");
        }

        isRawModeWindowsEnabled = true;
    }

    private static void DisableRawModeWindows()
    {
        if (!isRawModeWindowsEnabled)
        {
            return;
        }

        var stdIn = Kernel32.GetStdHandle(Kernel32.STD_INPUT_HANDLE);
        var stdOut = Kernel32.GetStdHandle(Kernel32.STD_OUTPUT_HANDLE);

        if (!Kernel32.SetConsoleMode(stdIn, originalStdInMode))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set stdin console mode.");
        }

        if (!Kernel32.SetConsoleMode(stdOut, originalStdOutMode))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to set stdout console mode.");
        }

        isRawModeWindowsEnabled = false;
    }

    private static char? ReadKeyWindows()
    {
        var stdIn = Kernel32.GetStdHandle(Kernel32.STD_INPUT_HANDLE);

        Kernel32.INPUT_RECORD winInputRecord = new Kernel32.INPUT_RECORD();

        while (true)
        {
            if (!Kernel32.ReadConsoleInputExW(stdIn, ref winInputRecord, 1, out int readEvents, Kernel32.CONSOLE_READ_FLAGS.CONSOLE_READ_NOWAIT))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (readEvents == 0)
            {
                return null;
            }

            if (winInputRecord.EventType == Kernel32.INPUT_RECORD_EVENT_TYPES.KEY_EVENT)
            {
                return (char)winInputRecord.KeyEvent.uChar;
            }
        }
    }

    private static void MakeRawInputMode(ref Kernel32.ConsoleInputModes inputMode)
    {
        inputMode &= ~Kernel32.ConsoleInputModes.ENABLE_ECHO_INPUT;
        inputMode &= ~Kernel32.ConsoleInputModes.ENABLE_LINE_INPUT;
        inputMode &= ~Kernel32.ConsoleInputModes.ENABLE_PROCESSED_INPUT;
        inputMode |= Kernel32.ConsoleInputModes.ENABLE_VIRTUAL_TERMINAL_INPUT;
    }

    private static void MakeRawOutputMode(ref Kernel32.ConsoleOutputModes outputMode)
    {
        outputMode &= ~Kernel32.ConsoleOutputModes.ENABLE_WRAP_AT_EOL_OUTPUT;
        outputMode |= Kernel32.ConsoleOutputModes.ENABLE_PROCESSED_OUTPUT;
        outputMode |= Kernel32.ConsoleOutputModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING;
    }

    internal static class Kernel32
    {
        public const int STD_INPUT_HANDLE = -10;

        public const int STD_OUTPUT_HANDLE = -11;


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ReadConsoleInputExW(
            IntPtr hConsoleHandle,
            ref INPUT_RECORD lpBuffer,
            int nLength_ShouldBeExactlyOne,
            out int lpNumberOfEventsRead,
            CONSOLE_READ_FLAGS wFlags);

        [Flags]
        public enum ConsoleInputModes : uint
        {
            ENABLE_PROCESSED_INPUT = 0x0001,
            ENABLE_LINE_INPUT = 0x0002,
            ENABLE_ECHO_INPUT = 0x0004,
            ENABLE_WINDOW_INPUT = 0x0008,
            ENABLE_MOUSE_INPUT = 0x0010,
            ENABLE_INSERT_MODE = 0x0020,
            ENABLE_QUICK_EDIT_MODE = 0x0040,
            ENABLE_EXTENDED_FLAGS = 0x0080,
            ENABLE_AUTO_POSITION = 0x0100,
            ENABLE_VIRTUAL_TERMINAL_INPUT = 0x200
        }

        [Flags]
        public enum ConsoleOutputModes : uint
        {
            ENABLE_PROCESSED_OUTPUT = 0x0001,
            ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002,
            ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004,
            DISABLE_NEWLINE_AUTO_RETURN = 0x0008,
            ENABLE_LVB_GRID_WORLDWIDE = 0x0010
        }
    
        [StructLayout(LayoutKind.Explicit)]
        public struct INPUT_RECORD 
        {
            [FieldOffset(0)]
            public INPUT_RECORD_EVENT_TYPES EventType;

            [FieldOffset(4)]
            public KEY_EVENT_RECORD KeyEvent;

            [FieldOffset(4)]
            public MOUSE_EVENT_RECORD MouseEvent;

            [FieldOffset(4)]
            public WINDOW_BUFFER_SIZE_RECORD WindowBufferSizeEvent;

            [FieldOffset(4)]
            public MENU_EVENT_RECORD MenuEvent;

            [FieldOffset(4)]
            public FOCUS_EVENT_RECORD FocusEvent;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct KEY_EVENT_RECORD
        {
            public bool bKeyDown;
            public ushort wRepeatCount;
            public ushort wVirtualKeyCode;
            public ushort wVirtualScanCode;
            public ushort uChar; // union { WCHAR UnicodeChar; CHAR  AsciiChar; } uChar;
            public uint dwControlKeyState;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSE_EVENT_RECORD
        {
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MENU_EVENT_RECORD
        {
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FOCUS_EVENT_RECORD
        {            
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOW_BUFFER_SIZE_RECORD
        {            
        }

        public enum INPUT_RECORD_EVENT_TYPES : ushort
        {
            KEY_EVENT  = 0x1,
            MOUSE_EVENT = 0x2,
            WINDOW_BUFFER_SIZE_EVENT = 0x4,
            FOCUS_EVENT = 0x10,
            MENU_EVENT = 0x8
        }

        [Flags]
        public enum CONSOLE_READ_FLAGS : ushort
        {
            NONE = 0,
            CONSOLE_READ_NOREMOVE = 0x0001,
            CONSOLE_READ_NOWAIT      = 0x0002
        }
    }
}