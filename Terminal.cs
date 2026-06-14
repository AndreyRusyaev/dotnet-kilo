enum KeyCodes
{
    Null,
    Up,
    Down,
    Left,
    Right,
    PageUp,
    PageDown,
    Home,
    End,
    Insert,
    Delete,
    Escape,
    Enter,
    Tab,
    Backspace,
    Space,

    D0,
    D1,
    D2,
    D3,
    D4,
    D5,
    D6,
    D7,
    D8,
    D9,

    A,
    B,
    C,
    D,
    E,
    F,
    G,
    H,
    I,
    J,
    K,
    L,
    M,
    N,
    O,
    P,
    Q,
    R,
    S,
    T,
    U,
    V,
    W,
    X,
    Y,
    Z,
}

[Flags]
enum KeyModifiers
{
    None,
    Alt,
    Control
}

struct KeyEvent : TerminalEvent
{
    public KeyCodes? KeyCode { get; init; }

    public char? KeyChar { get; init; }

    public KeyModifiers KeyModifiers { get; init; }

    public static KeyEvent FromChar(char keyChar, KeyModifiers keyModifiers = KeyModifiers.None)
    {
        return new KeyEvent
        {
            KeyChar = keyChar,
            KeyCode = ByKeyChar(keyChar),
            KeyModifiers = keyModifiers,
        };
    }

    public static KeyEvent FromKeyCode(KeyCodes keyCode, KeyModifiers keyModifiers = KeyModifiers.None)
    {
        return new KeyEvent
        {
            KeyChar = ByKeyCode(keyCode),
            KeyCode = keyCode,
            KeyModifiers = keyModifiers,
        };
    }

    private static KeyCodes? ByKeyChar(char keyChar)
    {
        switch (keyChar)
        {
            case '\0':
                return KeyCodes.Null;
            case '\x1b':
                return KeyCodes.Escape;
            case '\r':
                return KeyCodes.Enter;
            case '\t':
                return KeyCodes.Tab;
            case ' ':
                return KeyCodes.Space;

            case '0':
                return KeyCodes.D0;
            case '1':
                return KeyCodes.D1;
            case '2':
                return KeyCodes.D2;
            case '3':
                return KeyCodes.D3;
            case '4':
                return KeyCodes.D4;
            case '5':
                return KeyCodes.D5;
            case '6':
                return KeyCodes.D6;
            case '7':
                return KeyCodes.D7;
            case '8':
                return KeyCodes.D8;
            case '9':
                return KeyCodes.D9;

            case 'a':
            case 'A':
                return KeyCodes.A;
            case 'b':
            case 'B':
                return KeyCodes.B;
            case 'c':
            case 'C':
                return KeyCodes.C;
            case 'd':
            case 'D':
                return KeyCodes.D;
            case 'e':
            case 'E':
                return KeyCodes.E;
            case 'f':
            case 'F':
                return KeyCodes.F;
            case 'g':
            case 'G':
                return KeyCodes.G;
            case 'h':
            case 'H':
                return KeyCodes.H;
            case 'i':
            case 'I':
                return KeyCodes.I;
            case 'j':
            case 'J':
                return KeyCodes.J;
            case 'k':
            case 'K':
                return KeyCodes.K;
            case 'l':
            case 'L':
                return KeyCodes.L;
            case 'm':
            case 'M':
                return KeyCodes.M;
            case 'n':
            case 'N':
                return KeyCodes.N;
            case 'o':
            case 'O':
                return KeyCodes.O;
            case 'p':
            case 'P':
                return KeyCodes.P;
            case 'q':
            case 'Q':
                return KeyCodes.Q;
            case 'r':
            case 'R':
                return KeyCodes.R;
            case 's':
            case 'S':
                return KeyCodes.S;
            case 't':
            case 'T':
                return KeyCodes.T;
            case 'u':
            case 'U':
                return KeyCodes.U;
            case 'v':
            case 'V':
                return KeyCodes.V;
            case 'w':
            case 'W':
                return KeyCodes.W;
            case 'x':
            case 'X':
                return KeyCodes.X;
            case 'y':
            case 'Y':
                return KeyCodes.Y;
            case 'z':
            case 'Z':
                return KeyCodes.Z;
        }

        return null;
    }

    private static char? ByKeyCode(KeyCodes keyCode)
    {
        switch (keyCode)
        {
            case KeyCodes.Null: return '0';
            case KeyCodes.Escape: return '\x1b';
            case KeyCodes.Enter: return '\r';
            case KeyCodes.Tab: return '\t';
            case KeyCodes.Space: return ' ';

            case KeyCodes.D0: return '0';
            case KeyCodes.D1: return '1';
            case KeyCodes.D2: return '2';
            case KeyCodes.D3: return '3';
            case KeyCodes.D4: return '4';
            case KeyCodes.D5: return '5';
            case KeyCodes.D6: return '6';
            case KeyCodes.D7: return '7';
            case KeyCodes.D8: return '8';
            case KeyCodes.D9: return '9';
            case KeyCodes.A: return 'a';
            case KeyCodes.B: return 'b';
            case KeyCodes.C: return 'c';
            case KeyCodes.D: return 'd';
            case KeyCodes.E: return 'e';
            case KeyCodes.F: return 'f';
            case KeyCodes.G: return 'g';
            case KeyCodes.H: return 'h';
            case KeyCodes.I: return 'i';
            case KeyCodes.J: return 'j';
            case KeyCodes.K: return 'k';
            case KeyCodes.L: return 'l';
            case KeyCodes.M: return 'm';
            case KeyCodes.N: return 'n';
            case KeyCodes.O: return 'o';
            case KeyCodes.P: return 'p';
            case KeyCodes.Q: return 'q';
            case KeyCodes.R: return 'r';
            case KeyCodes.S: return 's';
            case KeyCodes.T: return 't';
            case KeyCodes.U: return 'u';
            case KeyCodes.V: return 'v';
            case KeyCodes.W: return 'w';
            case KeyCodes.X: return 'x';
            case KeyCodes.Y: return 'y';
            case KeyCodes.Z: return 'z';
        }

        return null;
    }
}

interface TerminalEvent
{    
}

static class Terminal
{
    public static IDisposable EnableRawMode()
    {
        return RawConsole.EnableRawMode();
    }

    public static TerminalEvent ReadEvent()
    {
        while (true)
        {
            var keyCode = RawConsole.ReadChar();
            if (keyCode == null)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
                continue;
            }
            
            if (keyCode.Value == 0)
            {
                return KeyEvent.FromKeyCode(KeyCodes.Space, KeyModifiers.Control);
            }
            else if (keyCode.Value == '\r')
            {
                return KeyEvent.FromKeyCode(KeyCodes.Enter);
            }
            else if (keyCode.Value == '\t')
            {
                return KeyEvent.FromKeyCode(KeyCodes.Tab);
            }
            else if (keyCode.Value == '\x1b') // ESC sequence
            {
                var nextKey1 = RawConsole.ReadChar();
                if (nextKey1 == null)
                {
                    return KeyEvent.FromKeyCode(KeyCodes.Escape);
                }

                var nextKey2 = RawConsole.ReadChar();
                if (nextKey2 == null)
                {
                    return KeyEvent.FromKeyCode(KeyCodes.Escape);
                }

                if (nextKey1.Value == '[') // CSI sequence
                {
                    if (nextKey2.Value >= '0' && nextKey2.Value <= '9')
                    {
                        var nextKey3 = RawConsole.ReadChar();
                        if (nextKey3 == null)
                        {
                            return KeyEvent.FromKeyCode(KeyCodes.Escape);
                        }

                        if (nextKey3.Value == '~')
                        {
                            switch (nextKey2.Value)
                            {
                                case '1': return KeyEvent.FromKeyCode(KeyCodes.Home);
                                case '2': return KeyEvent.FromKeyCode(KeyCodes.Insert);
                                case '3': return KeyEvent.FromKeyCode(KeyCodes.Delete);
                                case '4': return KeyEvent.FromKeyCode(KeyCodes.End);
                                case '5': return KeyEvent.FromKeyCode(KeyCodes.PageUp);
                                case '6': return KeyEvent.FromKeyCode(KeyCodes.PageDown);
                                case '7': return KeyEvent.FromKeyCode(KeyCodes.Home);
                                case '8': return KeyEvent.FromKeyCode(KeyCodes.End);
                            }
                        }
                    }
                    else
                    {
                        switch (nextKey2.Value)
                        {
                            case 'A': return KeyEvent.FromKeyCode(KeyCodes.Up);
                            case 'B': return KeyEvent.FromKeyCode(KeyCodes.Down);
                            case 'C': return KeyEvent.FromKeyCode(KeyCodes.Right);
                            case 'D': return KeyEvent.FromKeyCode(KeyCodes.Left);
                            case 'H': return KeyEvent.FromKeyCode(KeyCodes.Home);
                            case 'F': return KeyEvent.FromKeyCode(KeyCodes.End);
                        }
                    }
                }
                else if (nextKey1.Value == 'O')
                {
                    switch (nextKey2.Value)
                    {
                        case 'A': return KeyEvent.FromKeyCode(KeyCodes.Up);
                        case 'B': return KeyEvent.FromKeyCode(KeyCodes.Down);
                        case 'C': return KeyEvent.FromKeyCode(KeyCodes.Right);
                        case 'D': return KeyEvent.FromKeyCode(KeyCodes.Left);
                        case 'H': return KeyEvent.FromKeyCode(KeyCodes.Home);
                        case 'F': return KeyEvent.FromKeyCode(KeyCodes.End);
                    }
                }
                else if (nextKey1.Value == '\x1b')
                {
                    // TODO: ALT modifier
                }

                // TODO: Unrecognized sequence
                return KeyEvent.FromKeyCode(KeyCodes.Escape);
            }
            else if (keyCode.Value <= 0x1a)
            {
                // CTRL + A, ..., CTRL + Z (except CTRL + I and CTRL + M)
                return KeyEvent.FromChar((char)(keyCode.Value - 1 + 'a'), KeyModifiers.Control);
            }
            else if (keyCode.Value >= 0x1c && keyCode.Value <= 0x1f)
            {
                // CTRL + 4, CTRL + 5, CTRL + 6, CTRL + 7
                // CTRL + /, CTRL + ], CTRL + ~, CTRL + ?
                return KeyEvent.FromChar((char)(keyCode.Value - 0x1c + '4'), KeyModifiers.Control);
            }
            else if (keyCode.Value == 0x7f)
            {
                return KeyEvent.FromKeyCode(KeyCodes.Backspace);
            }

            return KeyEvent.FromChar(keyCode.Value);
        }
    }
}