class VT100
{
    public static string EnableKeypadApplicationMode()
    {
        // https://vt100.net/docs/vt100-ug/chapter3.html#DECKPAM 
        return $"\x1b=";
    }

    public static string EnableKeypadNumericMode()
    {
        // https://vt100.net/docs/vt100-ug/chapter3.html#DECKPNM
        return $"\x1b>";
    }

    public static string EraseDisplay(int ps = 0)
    {
        // Erase in Display https://vt100.net/docs/vt100-ug/chapter3.html#ED
        if (ps == 0)
        {
            return $"\x1b[J";
        }

        return $"\x1b[{ps}J";
    }

    public static string EraseLine(int ps = 0)
    {
        // Erase in Line http://vt100.net/docs/vt100-ug/chapter3.html#EL
        if (ps == 0)
        {
            return $"\x1b[K";
        }

        return $"\x1b[{ps}K";
    }

    public static string HideCursor()
    {
        return ResetPrivateDecMode(PrivateDecMode.DECTCEM);
    }

    public static string ShowCursor()
    {
        return SetPrivateDecMode(PrivateDecMode.DECTCEM);
    }

    public static string SetCursorPosition(int line, int column)
    {
        if (line < 1)
        {
            line = 1;
        }

        if (column < 1)
        {
            column = 1;
        }

        // Cursor Position https://vt100.net/docs/vt100-ug/chapter3.html#CUP
        if (line == 1 && column == 1)
        {
            return $"\x1b[H";
        }

        return $"\x1b[{line};{column}H";
    }

    public static string SetMode(int mode, bool isPrivateDecMode)
    {
        // https://vt100.net/docs/vt100-ug/chapter3.html#SM
        if (isPrivateDecMode)
        {
            return $"\x1b[?{mode}h";
        }

        return $"\x1b[{mode}h";
    }

    public static string ResetMode(int mode, bool isPrivateDecMode)
    {
        // https://vt100.net/docs/vt100-ug/chapter3.html#RM
        if (isPrivateDecMode)
        {
            return $"\x1b[?{mode}l";
        }

        return $"\x1b[{mode}l";
    }

    public static string SetPrivateDecMode(int privateDecMode)
    {
        return SetMode(privateDecMode, true);
    }

    public static string ResetPrivateDecMode(int privateDecMode)
    {
        return ResetMode(privateDecMode, true);
    }

    public static string SelectGraphicRendition(GraphicRendition ps = GraphicRendition.Off)
    {
        // https://vt100.net/docs/vt100-ug/chapter3.html#SGR
        if (ps == GraphicRendition.Off)
        {
            return "\x1b[m";
        }

        return $"\x1b[{(int)ps}m";
    }

    public static string SelectGraphicRendition(GraphicRendition[]? ps = null)
    {
        // https://vt100.net/docs/vt100-ug/chapter3.html#SGR
        if (ps == null || ps.Length == 0)
        {
            return "\x1b[m";
        }

        return $"\x1b[{string.Join(";", ps.Select(x => (int)x))}m";
    }

    public enum GraphicRendition
    {
        Off = 0,

        BoldOrIncreasedIntensity = 1,

        Underscore = 4,

        Blink = 5,

        Inverted = 7,

        // 3-4 bit colors
        ForegroundColor_Black = 30,

        ForegroundColor_Red = 31,

        ForegroundColor_Green = 32,

        ForegroundColor_Yellow = 33,

        ForegroundColor_Blue = 34,

        ForegroundColor_Magenta = 35,

        ForegroundColor_Cyan = 36,

        ForegroundColor_White = 37,

        ForegroundColor_BrightBlack = 90,

        ForegroundColor_BrightRed = 91,

        ForegroundColor_BrightGreen = 92,

        ForegroundColor_BrightYellow = 93,

        ForegroundColor_BrightBlue = 94,

        ForegroundColor_BrightMagenta = 95,

        ForegroundColor_BrightCyan = 96,

        ForegroundColor_BrightWhite = 97,

        ForegroundColor_Default = 39,

        BackgroundColor_Black = 40,

        BackgroundColor_Red = 41,

        BackgroundColor_Green = 42,

        BackgroundColor_Yellow = 43,

        BackgroundColor_Blue = 44,

        BackgroundColor_Magenta = 45,

        BackgroundColor_Cyan = 46,

        BackgroundColor_White = 47,

        BackgroundColor_BrightBlack = 100,

        BackgroundColor_BrightRed = 101,

        BackgroundColor_BrightGreen = 102,

        BackgroundColor_BrightYellow = 103,

        BackgroundColor_BrightBlue = 104,

        BackgroundColor_BrightMagenta = 105,

        BackgroundColor_BrightCyan = 106,

        BackgroundColor_BrightWhite = 107,
    }

    public static class PrivateDecMode
    {
        // Cursor key mode
        // https://vt100.net/docs/vt100-ug/chapter3.html#DECCKM
        public const int DECCKM = 1;

        // Text cursor enable
        // https://vt100.net/docs/vt510-rm/DECTCEM.html
        public const int DECTCEM = 25;
    }
}