internal record KeyTerminalEvent : TerminalEvent
{
    public KeyCodes? KeyCode { get; init; }

    public char? KeyChar { get; init; }

    public KeyModifiers KeyModifiers { get; init; }

    public static KeyTerminalEvent FromChar(char keyChar, KeyModifiers keyModifiers = KeyModifiers.None)
    {
        return new KeyTerminalEvent
        {
            KeyChar = keyChar,
            KeyCode = ByKeyChar(keyChar),
            KeyModifiers = keyModifiers,
        };
    }

    public static KeyTerminalEvent FromKeyCode(KeyCodes keyCode, KeyModifiers keyModifiers = KeyModifiers.None)
    {
        return new KeyTerminalEvent
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
