internal static partial class RawConsole
{
    public static void EnableRawMode()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            EnableRawModeWindows();
        }
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            EnableRawModeUnix();
        }
        else
        {
            throw new NotSupportedException($"Platform '{Environment.OSVersion.Platform}' is not supported.");
        }
    }

    public static char? ReadKey()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            return ReadKeyWindows();
        }
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            return ReadKeyUnix();
        }
        else 
        {
            throw new NotSupportedException($"Platform '{Environment.OSVersion.Platform}' is not supported.");
        }
    }    
}