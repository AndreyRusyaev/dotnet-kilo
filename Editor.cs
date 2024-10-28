using System.Text;

class Editor
{
    EditorSettings editorSettings;

    string? fileName;

    EditorSyntax? editorSyntax;

    int screenRows, screenColumns;

    int rowOffset, columnOffset;

    int cursorPosX, cursorPosY;

    int renderPosX, renderPosY;

    string? statusMessage;

    List<EditorRow> editorRows = new List<EditorRow>();

    int dirty;

    int quitRequestedCount;

    int findLastMatch = -1;

    int findDirection = -1;

    int? findSavedLine = null;

    public Editor(EditorSettings editorSettings)
    {
        this.editorSettings = editorSettings;

        this.quitRequestedCount = editorSettings.KILO_QUIT_TIMES;
    }

    public EditorSyntax? EditorSyntax => editorSyntax;

    public IReadOnlyList<EditorRow> EditorRows => editorRows.AsReadOnly();

    public void Start(string? fileName)
    {
        this.fileName = fileName;

        Init();

        Open();

        while (true)
        {
            RefreshScreen();

            if (!ProcessKeyPress())
            {
                break;
            }
        }
    }

    void Init()
    {
        screenRows = Console.WindowHeight - 2; // 2 additional lines used by status and message bar
        screenColumns = Console.WindowWidth;
        
        Console.Write(VT100.SwitchToAlternateScreen());
        Console.Write(VT100.SaveCursorPosition());

        SetStatusMessage("HELP: Ctrl+Q = quit | Ctrl+S = save | Ctrl+F = find");
    }

    void SelectSyntax()
    {
        editorSyntax = null;

        if (fileName == null)
        {
            return;
        }

        var extension = Path.GetExtension(fileName);
        foreach (var entry in HighlightDatabase.Entries)
        {
            foreach (var fileMatch in entry.FileMatches)
            {
                var isMatchWithExtension = fileMatch.Length > 0 && fileMatch[0] == '.';

                if ((isMatchWithExtension && string.CompareOrdinal(extension, fileMatch) == 0)
                    || (!isMatchWithExtension && string.CompareOrdinal(fileName, fileMatch) == 0))
                {
                    editorSyntax = entry;
                    break;
                }
            }
        }

        foreach (var editorRow in editorRows)
        {
            editorRow.UpdateRow(new EditorContext(this));
        }
    }

    void Open()
    {
        SelectSyntax();

        if (fileName != null)
        {
            if (File.Exists(fileName))
            {
                editorRows = new List<EditorRow>();

                int rowIndex = 0;
                foreach (var line in File.ReadAllLines(fileName))
                {
                    AppendRow(line);
                    rowIndex += 1;
                }
            }
        }

        dirty = 0;
    }

    void Save()
    {
        if (fileName == null)
        {
            fileName = Prompt("Save as: {0} (ESC to abort)");
            if (fileName == null)
            {
                SetStatusMessage("Save aborted");
                return;
            }

            SelectSyntax();
        }

        StringBuilder builder = new StringBuilder();
        for (var rowIndex = 0; rowIndex < editorRows.Count; rowIndex += 1)
        {
            builder.Append(editorRows[rowIndex].Chars.ToArray());
            if (rowIndex < editorRows.Count)
            {
                builder.Append(Environment.NewLine);
            }
        }

        if (fileName != null)
        {
            File.WriteAllText(fileName, builder.ToString());
            SetStatusMessage($"{builder.Length} bytes written to disk");

            dirty = 0;
        }
    }

    void FindCallback(string query, int keyCode)
    {
        if (findSavedLine != null)
        {
            var editorRow = editorRows[findSavedLine.Value];
            editorRow.UpdateRow(new EditorContext(this));
            findSavedLine = null;
        }

        if (keyCode == ExtendedKeyCodes.ENTER || keyCode == ExtendedKeyCodes.ESCAPE)
        {
            findLastMatch = -1;
            findDirection = 1;
            return;
        }
        else if (keyCode == ExtendedKeyCodes.ARROW_RIGHT || keyCode == ExtendedKeyCodes.ARROW_DOWN)
        {
            findDirection = 1;
        }
        else if (keyCode == ExtendedKeyCodes.ARROW_LEFT || keyCode == ExtendedKeyCodes.ARROW_UP)
        {
            findDirection = -1;
        }
        else
        {
            findLastMatch = -1;
            findDirection = 1;
        }

        if (findLastMatch == -1)
        {
            findDirection = 1;
        }

        var current = findLastMatch;

        for (int rowIndex = 0; rowIndex < editorRows.Count; rowIndex += 1)
        {
            current += findDirection;
            if (current == -1)
            {
                current = editorRows.Count - 1;
            }
            else if (current == editorRows.Count)
            {
                current = 0;
            }

            var editorRow = editorRows[current];

            var renderIndex = editorRow.Find(query);
            if (renderIndex != -1)
            {
                findLastMatch = current;
                findSavedLine = current;
                cursorPosY = current;
                cursorPosX = editorRow.ToCharPosX(renderIndex);
                rowOffset = editorRows.Count;
                editorRow.SetHighlight(renderIndex, query.Length, HighlightMode.Match);
                break;
            }
        }
    }

    void Find()
    {
        int savedCursorPosX = cursorPosX;
        int savedCursorPosY = cursorPosY;
        int savedColumnOffset = columnOffset;
        int savedRowOffset = rowOffset;

        string? query = Prompt("Search: {0} (Use ESC/Arrows/Enter)", FindCallback);
        if (query == null)
        {
            cursorPosX = savedCursorPosX;
            cursorPosY = savedCursorPosY;
            columnOffset = savedColumnOffset;
            rowOffset = savedRowOffset;
            return;
        }
    }

    void RefreshScreen()
    {
        Scroll();

        StringBuilder builder = new StringBuilder();

        builder.Append(VT100.HideCursor());
        builder.Append(VT100.SetCursorPosition(1, 1));

        DrawRaws(builder);
        DrawStatusBar(builder);
        DrawMessageBar(builder);

        builder.Append(VT100.SetCursorPosition(renderPosY - rowOffset + 1, renderPosX - columnOffset + 1));
        builder.Append(VT100.ShowCursor());

        Console.Write(builder.ToString());
    }

    string? Prompt(string messageFormat, Action<string, int>? callback = null)
    {
        StringBuilder result = new StringBuilder();

        while (true)
        {
            SetStatusMessage(string.Format(messageFormat, result.ToString()));
            RefreshScreen();

            int keyCode = ReadKey();

            if (keyCode == ExtendedKeyCodes.ESCAPE)
            {
                SetStatusMessage("");
                callback?.Invoke(result.ToString(), keyCode);
                return null;
            }
            else if (keyCode == ExtendedKeyCodes.DEL_KEY || keyCode == ExtendedKeyCodes.CTRL_H || keyCode == ExtendedKeyCodes.BACKSPACE)
            {
                if (result.Length > 0)
                {
                    result.Remove(result.Length - 1, 1);
                    callback?.Invoke(result.ToString(), keyCode);
                }
            }
            else if (keyCode == ExtendedKeyCodes.ENTER)
            {
                if (result.Length > 0)
                {
                    SetStatusMessage("");
                    callback?.Invoke(result.ToString(), keyCode);
                    return result.ToString();
                }
            }
            else
            {
                char keyChar = (char)keyCode;
                if (!char.IsControl(keyChar) && keyChar < 128)
                {
                    result.Append(keyChar);
                }
            }

            callback?.Invoke(result.ToString(), keyCode);
        }
    }

    int ReadKey()
    {
        while (true)
        {
            var keyCode = RawConsole.ReadKey();
            if (keyCode == null)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(50));
                continue;
            }

            if ((int)keyCode.Value == ExtendedKeyCodes.ESCAPE) // ESC sequence
            {
                var nextKey1 = RawConsole.ReadKey();
                if (nextKey1 == null)
                {
                    return ExtendedKeyCodes.ESCAPE;
                }

                var nextKey2 = RawConsole.ReadKey();
                if (nextKey2 == null)
                {
                    return ExtendedKeyCodes.ESCAPE;
                }

                if (nextKey1.Value == '[')
                {
                    if (nextKey2.Value >= '0' && nextKey2.Value <= '9')
                    {
                        var nextKey3 = RawConsole.ReadKey();
                        if (nextKey3 == null)
                        {
                            return ExtendedKeyCodes.ESCAPE;
                        }

                        if (nextKey3.Value == '~')
                        {
                            switch (nextKey2.Value)
                            {
                                case '1': return ExtendedKeyCodes.HOME_KEY;
                                case '3': return ExtendedKeyCodes.DEL_KEY;
                                case '4': return ExtendedKeyCodes.END_KEY;
                                case '5': return ExtendedKeyCodes.PAGE_UP;
                                case '6': return ExtendedKeyCodes.PAGE_DOWN;
                                case '7': return ExtendedKeyCodes.HOME_KEY;
                                case '8': return ExtendedKeyCodes.END_KEY;
                            }
                        }
                    }
                    else
                    {
                        switch (nextKey2.Value)
                        {
                            case 'A':
                                return ExtendedKeyCodes.ARROW_UP;
                            case 'B':
                                return ExtendedKeyCodes.ARROW_DOWN;
                            case 'C':
                                return ExtendedKeyCodes.ARROW_RIGHT;
                            case 'D':
                                return ExtendedKeyCodes.ARROW_LEFT;
                            case 'H':
                                return ExtendedKeyCodes.HOME_KEY;
                            case 'F':
                                return ExtendedKeyCodes.END_KEY;
                        }
                    }
                }
                else if (nextKey1.Value == 'O')
                {
                    switch (nextKey2.Value)
                    {
                        case 'A': return ExtendedKeyCodes.ARROW_UP;
                        case 'B': return ExtendedKeyCodes.ARROW_DOWN;
                        case 'C': return ExtendedKeyCodes.ARROW_RIGHT;
                        case 'D': return ExtendedKeyCodes.ARROW_LEFT;
                        case 'H': return ExtendedKeyCodes.HOME_KEY;
                        case 'F': return ExtendedKeyCodes.END_KEY;
                    }
                }
            }

            return keyCode.Value;
        }
    }

    bool ProcessKeyPress()
    {
        var keyCode = ReadKey();

        switch (keyCode)
        {
            case ExtendedKeyCodes.ENTER:
                InsertNewLine();
                break;

            case ExtendedKeyCodes.BACKSPACE:
            case ExtendedKeyCodes.CTRL_H:
            case ExtendedKeyCodes.DEL_KEY:
                if (keyCode == ExtendedKeyCodes.DEL_KEY)
                {
                    MoveCursor(ExtendedKeyCodes.ARROW_RIGHT);
                }
                DelChar();
                break;

            case ExtendedKeyCodes.CTRL_S:
                Save();
                break;

            case ExtendedKeyCodes.CTRL_F:
                Find();
                break;

            case ExtendedKeyCodes.CTRL_Q:
                {
                    if (dirty > 0 && quitRequestedCount > 0)
                    {
                        SetStatusMessage($"WARNING!!! File has unsaved changes. Press Ctrl-Q {quitRequestedCount} more times to quit.");
                        quitRequestedCount -= 1;
                        return true;
                    }

                    var builder = new StringBuilder();
                    builder.Append(VT100.EraseDisplay(2));
                    builder.Append(VT100.RestoreCursorPosition());
                    builder.Append(VT100.SwitchToMainScreen());
                    
                    Console.Write(builder.ToString());
                    return false;
                }

            case ExtendedKeyCodes.PAGE_UP:
            case ExtendedKeyCodes.PAGE_DOWN:
                {
                    if (keyCode == ExtendedKeyCodes.PAGE_UP)
                    {
                        cursorPosY = rowOffset;
                    }
                    else if (keyCode == ExtendedKeyCodes.PAGE_DOWN)
                    {
                        cursorPosY = rowOffset + screenRows - 1;
                        if (cursorPosY > editorRows.Count)
                        {
                            cursorPosY = editorRows.Count;
                        }
                    }

                    int times = 0;
                    while (times++ < screenRows)
                    {
                        MoveCursor(keyCode == ExtendedKeyCodes.PAGE_UP ? ExtendedKeyCodes.ARROW_UP : ExtendedKeyCodes.ARROW_DOWN);
                    }
                }
                break;

            case ExtendedKeyCodes.HOME_KEY:
                cursorPosX = 0;
                break;
            case ExtendedKeyCodes.END_KEY:
                if (cursorPosY < editorRows.Count)
                {
                    var editorRow = editorRows[cursorPosY];
                    cursorPosX = editorRow.CharSize;
                }
                break;

            case ExtendedKeyCodes.ARROW_UP:
            case ExtendedKeyCodes.ARROW_DOWN:
            case ExtendedKeyCodes.ARROW_LEFT:
            case ExtendedKeyCodes.ARROW_RIGHT:
                MoveCursor(keyCode);
                break;

            case ExtendedKeyCodes.CTRL_L:
            case ExtendedKeyCodes.ESCAPE:
                break;

            default:
                InsertChar((char)keyCode);
                break;
        }

        quitRequestedCount = editorSettings.KILO_QUIT_TIMES;
        return true;
    }

    void MoveCursor(int keyCode)
    {
        switch (keyCode)
        {
            case ExtendedKeyCodes.ARROW_UP:
                {
                    if (cursorPosY > 0)
                    {
                        cursorPosY -= 1;
                    }
                    break;
                }
            case ExtendedKeyCodes.ARROW_DOWN:
                {
                    if (cursorPosY + 1 < editorRows.Count)
                    {
                        cursorPosY += 1;
                    }
                    break;
                }
            case ExtendedKeyCodes.ARROW_LEFT:
                {
                    if (cursorPosX > 0)
                    {
                        cursorPosX -= 1;
                    }
                    else if (cursorPosY > 0)
                    {
                        cursorPosY -= 1;
                        if (cursorPosY < editorRows.Count)
                        {
                            var editorRow = editorRows[cursorPosY];
                            cursorPosX = editorRow.CharSize;
                        }
                    }
                    break;
                }
            case ExtendedKeyCodes.ARROW_RIGHT:
                {
                    if (cursorPosY < editorRows.Count)
                    {
                        var editorRow = editorRows[cursorPosY];
                        if (cursorPosX < editorRow.CharSize)
                        {
                            cursorPosX += 1;
                        }
                        else if (cursorPosY + 1 < editorRows.Count)
                        {
                            cursorPosY += 1;
                            cursorPosX = 0;
                        }
                    }
                    break;
                }
        }

        if (cursorPosY < editorRows.Count)
        {
            var editorRow = editorRows[cursorPosY];
            if (cursorPosX > editorRow.CharSize)
            {
                cursorPosX = editorRow.CharSize;
            }
        }
    }

    void Scroll()
    {
        renderPosX = 0;
        renderPosY = cursorPosY;
        if (cursorPosY < editorRows.Count)
        {
            renderPosX = editorRows[cursorPosY].ToRenderPosX(cursorPosX);
        }

        if (renderPosY < rowOffset)
        {
            rowOffset = renderPosY;
        }

        if (renderPosY >= rowOffset + screenRows)
        {
            rowOffset = renderPosY - screenRows + 1;
        }

        if (renderPosX < columnOffset)
        {
            columnOffset = renderPosX;
        }

        if (renderPosX >= columnOffset + screenColumns)
        {
            columnOffset = renderPosX - screenColumns + 1;
        }
    }

    void SetStatusMessage(string newMessage)
    {
        statusMessage = newMessage;
    }

    void DrawRaws(StringBuilder builder)
    {
        for (var ii = 0; ii < screenRows; ii += 1)
        {
            int rowIndex = rowOffset + ii;
            if (rowIndex < editorRows.Count)
            {
                var editorRow = editorRows[rowIndex];

                int offset = columnOffset;
                if (offset > editorRow.RenderSize)
                {
                    offset = editorRow.RenderSize;
                }

                int length = editorRow.RenderSize - columnOffset;
                if (length < 0)
                {
                    length = 0;
                }
                else if (length > screenColumns)
                {
                    length = screenColumns;
                }

                VT100.GraphicRendition currentMode = VT100.GraphicRendition.ForegroundColor_Default;
                for (int renderIndex = offset; renderIndex < offset + length; renderIndex += 1)
                {
                    char ch = editorRow.RenderChars[renderIndex];

                    if (char.IsControl(ch))
                    {
                        char sym = (ch <= 26) ? (char)('@' + ch) : '?';
                        builder.Append(VT100.SelectGraphicRendition(VT100.GraphicRendition.Inverted));
                        builder.Append(sym);
                        builder.Append(VT100.SelectGraphicRendition(VT100.GraphicRendition.Off));
                        builder.Append(VT100.SelectGraphicRendition(currentMode));
                    }
                    else
                    {
                        var newMode = SyntaxToForegroundColor(editorRow.HighlightModes[renderIndex]);
                        if (currentMode != newMode)
                        {
                            builder.Append(VT100.SelectGraphicRendition(newMode));
                            currentMode = newMode;
                        }

                        builder.Append(ch);
                    }
                }

                builder.Append(VT100.SelectGraphicRendition(VT100.GraphicRendition.ForegroundColor_Default));
            }
            else
            {
                builder.Append("~");
            }

            builder.Append(VT100.EraseLine());
            builder.Append("\r\n");
        }
    }

    void DrawStatusBar(StringBuilder builder)
    {
        builder.Append(VT100.SelectGraphicRendition(VT100.GraphicRendition.Inverted));

        string statusFileName = fileName != null ? fileName : "[No Name]";

        string modifiedStatus = dirty > 0 ? " (modified)" : "";

        string fileType = editorSyntax != null ? editorSyntax.Name : "unknown type";

        var statusMessage = $"{statusFileName}{modifiedStatus} ({fileType})"
            + $", Lines {cursorPosY + 1}/{editorRows.Count}"
            + $", Column {cursorPosX}"
            + $", screen={screenColumns}x{screenRows}, offset={columnOffset}x{rowOffset}";

        if (statusMessage.Length > screenColumns) 
        {
            statusMessage = statusMessage.Substring(0, screenColumns - 3) + "...";
        }

        int padding = 0;

        if (!string.IsNullOrEmpty(statusMessage))
        {
            padding = (screenColumns - statusMessage.Length) / 2;
            for (int ii = 0; ii < padding; ii += 1)
            {
                builder.Append(' ');
            }

            builder.Append(statusMessage);

            padding += statusMessage.Length;
        }

        for (int ii = padding; ii < screenColumns; ii += 1)
        {
            builder.Append(' ');
        }

        builder.Append(VT100.SelectGraphicRendition(VT100.GraphicRendition.Off));
        builder.Append("\r\n");
    }

    void DrawMessageBar(StringBuilder builder)
    {
        builder.Append(VT100.EraseLine());

        if (statusMessage != null)
        {
            int maxLen = statusMessage.Length;
            if (maxLen > screenColumns)
            {
                maxLen = screenColumns;
            }

            builder.Append(statusMessage.Substring(0, maxLen));
        }
    }

    VT100.GraphicRendition SyntaxToForegroundColor(HighlightMode highlightMode)
    {
        switch (highlightMode)
        {
            case HighlightMode.Number:
                return VT100.GraphicRendition.ForegroundColor_Red;
            case HighlightMode.String:
                return VT100.GraphicRendition.ForegroundColor_Magenta;
            case HighlightMode.Comment:
            case HighlightMode.MultiLineComment:
                return VT100.GraphicRendition.ForegroundColor_Cyan;
            case HighlightMode.Keyword1:
                return VT100.GraphicRendition.ForegroundColor_Yellow;
            case HighlightMode.Keyword2:
                return VT100.GraphicRendition.ForegroundColor_Green;
            case HighlightMode.Match:
                return VT100.GraphicRendition.ForegroundColor_Blue;
            default:
                return VT100.GraphicRendition.ForegroundColor_Default;
        }
    }

    void UpdateRow(EditorRow row)
    {
        row.UpdateRow(new EditorContext(this));
    }

    void InsertRow(int position, IEnumerable<char> chars)
    {
        var editorRow = new EditorRow(editorSettings, position, chars);
        editorRows.Insert(position, editorRow);

        for (int rowIndex = position + 1; rowIndex < editorRows.Count; rowIndex += 1)
        {
            editorRows[rowIndex].Index = rowIndex;
        }

        editorRow.UpdateRow(new EditorContext(this));
        dirty += 1;
    }

    void AppendRow(IEnumerable<char> chars)
    {
        var editorRow = new EditorRow(editorSettings, editorRows.Count, chars);
        editorRows.Add(editorRow);
        editorRow.UpdateRow(new EditorContext(this));
        dirty += 1;
    }

    void DelRow(int position)
    {
        if (position < 0 || position > editorRows.Count)
        {
            return;
        }

        editorRows.RemoveAt(position);

        for (int rowIndex = position; rowIndex < editorRows.Count; rowIndex += 1)
        {
            editorRows[rowIndex].Index = rowIndex;
        }

        dirty += 1;
    }

    void InsertNewLine()
    {
        if (cursorPosX == 0)
        {
            InsertRow(cursorPosY, Array.Empty<char>());
        }
        else
        {
            var currentRow = editorRows[cursorPosY];
            InsertRow(cursorPosY + 1, currentRow.Chars.Skip(cursorPosX).Take(currentRow.CharSize - cursorPosX).ToArray());
            currentRow.Truncate(cursorPosX);
            currentRow.UpdateRow(new EditorContext(this));
        }

        cursorPosX = 0;
        cursorPosY += 1;
    }

    void InsertChar(char ch)
    {
        if (cursorPosY == editorRows.Count)
        {
            AppendRow("");
        }

        var currentRow = editorRows[cursorPosY];
        currentRow.InsertChar(cursorPosX, ch);
        currentRow.UpdateRow(new EditorContext(this));
        cursorPosX += 1;
        dirty += 1;
    }

    void DelChar()
    {
        if (cursorPosY >= editorRows.Count)
        {
            return;
        }

        if (cursorPosX == 0 && cursorPosY == 0)
        {
            return;
        }

        var currentRow = editorRows[cursorPosY];

        if (cursorPosX > 0)
        {
            currentRow.RemoveChar(cursorPosX - 1);
            currentRow.UpdateRow(new EditorContext(this));
            cursorPosX -= 1;
        }
        else
        {
            var prevRow = editorRows[cursorPosY - 1];
            var originalSize = prevRow.CharSize;

            prevRow.AppendString(currentRow.Chars);
            DelRow(cursorPosY);
            prevRow.UpdateRow(new EditorContext(this));

            cursorPosX = originalSize;
            cursorPosY -= 1;
        }

        dirty += 1;
    }
}