using System.Diagnostics.CodeAnalysis;

static class Terminal
{
    public static IDisposable EnableRawMode()
    {
        return RawConsole.EnableRawMode();
    }

    public static TerminalEvent WaitEvent(TimeSpan pollInterval)
    {
        while (true)
        {
            if (TryReadEvent(out TerminalEvent? terminalEvent))
            {
                return terminalEvent;
            }

            Thread.Sleep(pollInterval);
        }
    }

    public static bool TryReadEvent([NotNullWhen(true)] out TerminalEvent? terminalEvent)
    {   
        terminalEvent = null;

        var keyCode = RawConsole.ReadChar();
        if (keyCode == null)
        {
            return false;
        }
        
        if (keyCode.Value == 0)
        {
            terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Space, KeyModifiers.Control);
            return true;
        }
        else if (keyCode.Value == '\r')
        {
            terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Enter);
            return true;
        }
        else if (keyCode.Value == '\n')
        {
            terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Enter);
            return true;
        }
        else if (keyCode.Value == '\t')
        {
            terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Tab);
            return true;
        }
        else if (keyCode.Value == '\x1b') // ESC sequence
        {
            var nextKey1 = RawConsole.ReadChar();
            if (nextKey1 == null)
            {
                terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Escape);
                return true;
            }

            var nextKey2 = RawConsole.ReadChar();
            if (nextKey2 == null)
            {
                terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Escape);
                return true;
            }

            if (nextKey1.Value == '[') // CSI sequence
            {
                if (nextKey2.Value >= '0' && nextKey2.Value <= '9')
                {
                    var nextKey3 = RawConsole.ReadChar();
                    if (nextKey3 == null)
                    {
                        terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Escape);
                        return true;
                    }

                    if (nextKey3.Value == '~')
                    {
                        switch (nextKey2.Value)
                        {
                            case '1': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Home); return true;
                            case '2': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Insert); return true;
                            case '3': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Delete); return true;
                            case '4': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.End); return true;
                            case '5': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.PageUp); return true;
                            case '6': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.PageDown); return true;
                            case '7': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Home); return true;
                            case '8': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.End); return true;
                        }
                    }
                }
                else
                {
                    switch (nextKey2.Value)
                    {
                        case 'A': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Up); return true;
                        case 'B': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Down); return true;
                        case 'C': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Right); return true;
                        case 'D': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Left); return true;
                        case 'H': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Home); return true;
                        case 'F': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.End); return true;
                    }
                }
            }
            else if (nextKey1.Value == 'O')
            {
                switch (nextKey2.Value)
                {
                    case 'A': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Up); return true;
                    case 'B': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Down); return true;
                    case 'C': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Right); return true;
                    case 'D': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Left); return true;
                    case 'H': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Home); return true;
                    case 'F': terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.End); return true;
                }
            }
            else if (nextKey1.Value == '\x1b')
            {
                // TODO: ALT modifier
            }

            // TODO: Unrecognized sequence
            terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Escape);
            return true;
        }
        else if (keyCode.Value <= 0x1a)
        {
            // CTRL + A, ..., CTRL + Z (except CTRL + I and CTRL + M)
            terminalEvent = KeyTerminalEvent.FromChar((char)(keyCode.Value - 1 + 'a'), KeyModifiers.Control);
            return true;
        }
        else if (keyCode.Value >= 0x1c && keyCode.Value <= 0x1f)
        {
            // CTRL + 4, CTRL + 5, CTRL + 6, CTRL + 7
            // CTRL + /, CTRL + ], CTRL + ~, CTRL + ?
            terminalEvent = KeyTerminalEvent.FromChar((char)(keyCode.Value - 0x1c + '4'), KeyModifiers.Control);
            return true;
        }
        else if (keyCode.Value == 0x7f)
        {
            terminalEvent = KeyTerminalEvent.FromKeyCode(KeyCodes.Backspace);
            return true;
        }

        terminalEvent = KeyTerminalEvent.FromChar(keyCode.Value);
        return true;
    }
}