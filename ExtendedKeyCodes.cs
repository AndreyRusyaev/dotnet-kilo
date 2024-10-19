public static class ExtendedKeyCodes
{
    private const int ExtendedCodesBase = 1000;

    public const int CTRL_F = 'f' & 0x1f;

    public const int CTRL_H = 'h' & 0x1f;

    public const int CTRL_L = 'l' & 0x1f;

    public const int CTRL_S = 's' & 0x1f;

    public const int CTRL_Q = 'q' & 0x1f;

    public const int ENTER = '\r';

    public const int ESCAPE = '\x1b';

    public const int BACKSPACE = 127;    

    public const int ARROW_UP = ExtendedCodesBase + 1;

    public const int ARROW_DOWN = ExtendedCodesBase + 2;

    public const int ARROW_LEFT = ExtendedCodesBase + 3;

    public const int ARROW_RIGHT = ExtendedCodesBase + 4;

    public const int HOME_KEY = ExtendedCodesBase + 5;

    public const int END_KEY = ExtendedCodesBase + 6;

    public const int DEL_KEY = ExtendedCodesBase + 7;

    public const int PAGE_UP = ExtendedCodesBase + 8;

    public const int PAGE_DOWN = ExtendedCodesBase + 9;
}