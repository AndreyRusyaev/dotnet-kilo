using System.Runtime.InteropServices;

internal static partial class RawConsole
{
    public static IDisposable EnableRawMode()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            RawConsoleWindows.EnableRawMode();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            RawConsoleLinux.EnableRawMode();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            RawConsoleMac.EnableRawMode();
        }
        else
        {
            throw new NotSupportedException($"Platform '{RuntimeInformation.OSDescription}' is not supported.");
        }

        return new RawConsoleModeHandle(DisableRawMode);
    }

    public static void DisableRawMode()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            RawConsoleWindows.DisableRawMode();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            RawConsoleLinux.DisableRawMode();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            RawConsoleMac.DisableRawMode();
        }
        else
        {
            throw new NotSupportedException($"Platform '{RuntimeInformation.OSDescription}' is not supported.");
        }
    }

    public static char? ReadKey()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return RawConsoleWindows.ReadKey();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RawConsoleLinux.ReadKey();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RawConsoleMac.ReadKey();
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