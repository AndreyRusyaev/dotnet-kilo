internal static partial class RawConsole
{
    public static IDisposable EnableRawMode()
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

        return new RawConsoleModeHandle(DisableRawMode);
    }

    public static void DisableRawMode()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            DisableRawModeWindows();
        }
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            DisableRawModeUnix();
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

    private class RawConsoleModeHandle : IDisposable
    {
        private Action disposeAction;

        private bool disposed;

        public RawConsoleModeHandle(Action disposeAction)
        {
            this.disposeAction = disposeAction;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposeAction();
                disposed = true;
            }
        }
    }
}