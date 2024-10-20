using System.ComponentModel;
using System.Runtime.InteropServices;

internal static partial class RawConsole
{
    private static int STD_INPUT_HANDLE = -10;

    private static int STD_OUTPUT_HANDLE = -11;

    internal const short KEY_EVENT = 1;

    private static char[] windowsKeyBuf = new char[1];

    private static void EnableRawModeWindows()
    {
        var stdIn = GetStdHandle(STD_INPUT_HANDLE);
        var stdOut = GetStdHandle(STD_OUTPUT_HANDLE);

        if (!GetConsoleMode(stdIn, out int nativeInputMode))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var originalInputMode = (ConsoleInputModes)nativeInputMode;

        var newInputMode = originalInputMode;
        newInputMode &= ~ConsoleInputModes.ENABLE_ECHO_INPUT;
        newInputMode &= ~ConsoleInputModes.ENABLE_LINE_INPUT;
        newInputMode &= ~ConsoleInputModes.ENABLE_PROCESSED_INPUT;
        newInputMode |= ConsoleInputModes.ENABLE_VIRTUAL_TERMINAL_INPUT;

        if (!GetConsoleMode(stdOut, out int nativeOutputMode))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        var originalOutputMode = (ConsoleOutputModes)nativeOutputMode;
        
        var newOutputMode = originalOutputMode;
        newOutputMode &= ~ConsoleOutputModes.ENABLE_WRAP_AT_EOL_OUTPUT;
        newOutputMode |= ConsoleOutputModes.ENABLE_PROCESSED_OUTPUT;
        newOutputMode |= ConsoleOutputModes.ENABLE_VIRTUAL_TERMINAL_PROCESSING;

        if (!SetConsoleMode(stdIn, (int)newInputMode))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!SetConsoleMode(stdOut, (int)newOutputMode))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private static char ReadKeyWindows()
    {
        while(true)
        {
            if (!ReadConsoleW(GetStdHandle(STD_INPUT_HANDLE), windowsKeyBuf, 1, out int readBytes, IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (readBytes == 0)
            {
                continue;
            }

            return windowsKeyBuf[0];
        }
    }
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool ReadConsoleW(IntPtr hConsoleHandle, char[] buffer, int nNumberOfCharsToRead, out int lpNumberOfCharsRead, IntPtr pInputControl);

    [Flags]
    private enum ConsoleInputModes : uint
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
    private enum ConsoleOutputModes : uint
    {
        ENABLE_PROCESSED_OUTPUT = 0x0001,
        ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002,
        ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004,
        DISABLE_NEWLINE_AUTO_RETURN = 0x0008,
        ENABLE_LVB_GRID_WORLDWIDE = 0x0010
    }
}