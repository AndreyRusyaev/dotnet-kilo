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

    List<EditorRow> editorRows = new();

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

            if (!ProcessTerminalEvent())
            {
                break;
            }
        }
    }

    void Init()
    {
        screenRows = Console.WindowHeight - 2; // 2 additional lines used by status and message bar
        screenColumns = Console.WindowWidth;

        var builder = new StringBuilder();

        builder.Append(VT100.SwitchToAlternateScreen());
        builder.Append(VT100.SaveCursorPosition());

        Console.Write(builder.ToString());

        SetStatusMessage("HELP: Ctrl+Q = quit | Ctrl+S = save | Ctrl+F = find");
    }

    void Restore()
    {
        var builder = new StringBuilder();

        builder.Append(VT100.EraseDisplay(2));
        builder.Append(VT100.RestoreCursorPosition());
        builder.Append(VT100.SwitchToMainScreen());

        Console.Write(builder.ToString());
    }

    void SelectSyntax()
    {
        editorSyntax = null;

        if (fileName == null)
        {
            return;
        }

        editorSyntax = HighlightDatabase.ResolveSyntax(fileName);

        foreach (var editorRow in editorRows)
        {
            UpdateSyntax(editorRow);
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

        StringBuilder builder = new();
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

    void FindCallback(string query, KeyTerminalEvent keyEvent)
    {
        if (findSavedLine != null)
        {
            var editorRow = editorRows[findSavedLine.Value];
            UpdateRow(editorRow);
            findSavedLine = null;
        }

        if (keyEvent.KeyCode == KeyCodes.Enter || keyEvent.KeyCode == KeyCodes.Escape)
        {
            findLastMatch = -1;
            findDirection = 1;
            return;
        }
        else if (keyEvent.KeyCode == KeyCodes.Right || keyEvent.KeyCode == KeyCodes.Down)
        {
            findDirection = 1;
        }
        else if (keyEvent.KeyCode == KeyCodes.Left || keyEvent.KeyCode == KeyCodes.Up)
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

            var resultIndex = editorRow.Find(query);
            if (resultIndex != -1)
            {
                findLastMatch = current;
                findSavedLine = current;
                cursorPosY = current;
                cursorPosX = resultIndex;
                rowOffset = editorRows.Count;
                editorRow.SetHighlightRange(resultIndex, query.Length, HighlightMode.Match);
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
        }

        SetStatusMessage("HELP: Ctrl+Q = quit | Ctrl+S = save | Ctrl+F = find");
    }

    void RefreshScreen()
    {
        Scroll();

        StringBuilder builder = new();

        builder.Append(VT100.HideCursor());
        builder.Append(VT100.SetCursorPosition(1, 1));

        DrawRaws(builder);
        DrawStatusBar(builder);
        DrawMessageBar(builder);

        builder.Append(VT100.SetCursorPosition(renderPosY - rowOffset + 1, renderPosX - columnOffset + 1));
        builder.Append(VT100.ShowCursor());

        Console.Write(builder.ToString());
    }

    string? Prompt(string messageFormat, Action<string, KeyTerminalEvent>? callback = null)
    {
        StringBuilder result = new();

        while (true)
        {
            SetStatusMessage(string.Format(messageFormat, result.ToString()));
            RefreshScreen();

            var terminalEvent = Terminal.WaitEvent(TimeSpan.FromMilliseconds(50));
            switch (terminalEvent)
            {
                case KeyTerminalEvent keyEvent when keyEvent.KeyCode == KeyCodes.Escape:
                    {
                        SetStatusMessage("");
                        callback?.Invoke(result.ToString(), keyEvent);
                        return null;
                    }
                case KeyTerminalEvent keyEvent when keyEvent.KeyCode == KeyCodes.Delete
                            || (keyEvent.KeyCode == KeyCodes.H && keyEvent.KeyModifiers == KeyModifiers.Control)
                            || keyEvent.KeyCode == KeyCodes.Backspace:
                    {
                        if (result.Length > 0)
                        {
                            result.Remove(result.Length - 1, 1);
                        }
                        callback?.Invoke(result.ToString(), keyEvent);
                        break;
                    }
                case KeyTerminalEvent keyEvent when keyEvent.KeyCode == KeyCodes.Enter:
                    {
                        if (result.Length > 0)
                        {
                            SetStatusMessage("");
                        }
                        callback?.Invoke(result.ToString(), keyEvent);
                        return result.ToString();
                    }
                case KeyTerminalEvent keyEvent:
                    {
                        if (keyEvent.KeyChar != null && !char.IsControl(keyEvent.KeyChar.Value))
                        {
                            result.Append(keyEvent.KeyChar.Value);
                        }

                        callback?.Invoke(result.ToString(), keyEvent);
                        break;
                    }
                    
                default:
                    // Unsupported event
                    break;
            }
        }
    }

    bool ProcessTerminalEvent()
    {
        var terminalEvent = Terminal.WaitEvent(TimeSpan.FromMilliseconds(50));
        switch (terminalEvent)
        {
            case KeyTerminalEvent keyEvent:
                return ProcessKeyPress(keyEvent);
            default:
                break;
        }

        return true;
    }

    bool ProcessKeyPress(KeyTerminalEvent keyEvent)
    {
        switch (keyEvent.KeyCode)
        {
            case KeyCodes.Enter:
                InsertNewLine();
                break;

            case KeyCodes.Backspace:
            case KeyCodes.H when keyEvent.KeyModifiers == KeyModifiers.Control:
            case KeyCodes.Delete:
                if (keyEvent.KeyCode == KeyCodes.Delete)
                {
                    MoveCursor(KeyCodes.Right);
                }
                RemoveChar();
                break;

            case KeyCodes.S when keyEvent.KeyModifiers == KeyModifiers.Control:
                Save();
                break;

            case KeyCodes.F when keyEvent.KeyModifiers == KeyModifiers.Control:
                Find();
                break;

            case KeyCodes.Q when keyEvent.KeyModifiers == KeyModifiers.Control:
                {
                    if (dirty > 0 && quitRequestedCount > 0)
                    {
                        SetStatusMessage($"WARNING!!! File has unsaved changes. Press Ctrl-Q {quitRequestedCount} more times to quit.");
                        quitRequestedCount -= 1;
                        return true;
                    }

                    Restore();
                    return false;
                }

            case KeyCodes.PageUp:
            case KeyCodes.PageDown:
                {
                    if (keyEvent.KeyCode == KeyCodes.PageUp)
                    {
                        cursorPosY = rowOffset;
                    }
                    else if (keyEvent.KeyCode == KeyCodes.PageDown)
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
                        MoveCursor(keyEvent.KeyCode == KeyCodes.PageUp ? KeyCodes.Up : KeyCodes.Down);
                    }
                }
                break;

            case KeyCodes.Home:
                cursorPosX = 0;
                break;
            case KeyCodes.End:
                if (cursorPosY < editorRows.Count)
                {
                    var editorRow = editorRows[cursorPosY];
                    cursorPosX = editorRow.CharSize;
                }
                break;

            case KeyCodes.Up:
            case KeyCodes.Down:
            case KeyCodes.Left:
            case KeyCodes.Right:
                MoveCursor(keyEvent.KeyCode.Value);
                break;

            case KeyCodes.L when keyEvent.KeyModifiers == KeyModifiers.Control:
            case KeyCodes.Escape:
                break;

            default:
                if (keyEvent.KeyChar != null)
                {
                    InsertChar(keyEvent.KeyChar.Value);
                }
                break;
        }

        quitRequestedCount = editorSettings.KILO_QUIT_TIMES;
        return true;
    }

    void MoveCursor(KeyCodes keyCode)
    {
        switch (keyCode)
        {
            case KeyCodes.Up:
                {
                    if (cursorPosY > 0)
                    {
                        cursorPosY -= 1;
                    }
                    break;
                }
            case KeyCodes.Down:
                {
                    if (cursorPosY + 1 < editorRows.Count)
                    {
                        cursorPosY += 1;
                    }
                    break;
                }
            case KeyCodes.Left:
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
            case KeyCodes.Right:
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
            renderPosX = editorRows[cursorPosY].GetRenderIndex(cursorPosX);
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
            + $", Column {cursorPosX + 1}"
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

    void UpdateRow(EditorRow editorRow)
    {
        editorRow.UpdateRow();

        UpdateSyntax(editorRow);
    }

    void InsertRow(int position, IEnumerable<char> chars)
    {
        var editorRow = new EditorRow(editorSettings, position, chars);
        editorRows.Insert(position, editorRow);

        for (int rowIndex = position + 1; rowIndex < editorRows.Count; rowIndex += 1)
        {
            editorRows[rowIndex].RowIndex = rowIndex;
        }

        UpdateRow(editorRow);
        dirty += 1;
    }

    void AppendRow(IEnumerable<char> chars)
    {
        var editorRow = new EditorRow(editorSettings, editorRows.Count, chars);
        editorRows.Add(editorRow);
        UpdateRow(editorRow);
        dirty += 1;
    }

    void RemoveRow(int position)
    {
        if (position < 0 || position > editorRows.Count)
        {
            return;
        }

        editorRows.RemoveAt(position);

        for (int rowIndex = position; rowIndex < editorRows.Count; rowIndex += 1)
        {
            editorRows[rowIndex].RowIndex = rowIndex;
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
            UpdateRow(currentRow);
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
        currentRow.InsertCharAt(cursorPosX, ch);
        UpdateRow(currentRow);
        cursorPosX += 1;
        dirty += 1;
    }

    void RemoveChar()
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
            currentRow.RemoveCharAt(cursorPosX - 1);
            UpdateRow(currentRow);
            cursorPosX -= 1;
        }
        else
        {
            var prevRow = editorRows[cursorPosY - 1];
            var originalSize = prevRow.CharSize;

            prevRow.Append(currentRow.Chars);
            RemoveRow(cursorPosY);
            UpdateRow(prevRow);

            cursorPosX = originalSize;
            cursorPosY -= 1;
        }

        dirty += 1;
    }

    void UpdateSyntax(EditorRow editorRow)
    {
        if (editorSyntax == null)
        {
            return;
        }

        bool prevSeparator = true;
        char? isInString = null;
        bool isInMultiLineComment = editorRow.RowIndex > 0 ? editorRows[editorRow.RowIndex - 1].HasHighlightOpenComment : false;

        for (int renderIndex = 0; renderIndex < editorRow.RenderChars.Count; renderIndex += 1)
        {
            var renderChar = editorRow.RenderChars[renderIndex];
            var prevHighlightMode = renderIndex > 0 ? editorRow.HighlightModes[renderIndex - 1] : HighlightMode.Normal;

            if (editorSyntax.SingleLineCommentStart != null
                && editorSyntax.SingleLineCommentStart.Length > 0
                && isInString == null
                && !isInMultiLineComment)
            {
                if (editorRow.StartsWithAtPosition(editorSyntax.SingleLineCommentStart, renderIndex))
                {
                    editorRow.SetHighlightRange(renderIndex, HighlightMode.Comment);
                    break;
                }
            }

            if (editorSyntax.MultiLineCommentStart != null && editorSyntax.MultiLineCommentStart.Length > 0
                && editorSyntax.MultiLineCommentEnd != null && editorSyntax.MultiLineCommentEnd.Length > 0
                && isInString == null)
            {
                if (isInMultiLineComment)
                {
                    editorRow.SetHighlightRange(renderIndex, HighlightMode.MultiLineComment);

                    if (editorRow.StartsWithAtPosition(editorSyntax.MultiLineCommentEnd, renderIndex))
                    {
                        editorRow.SetHighlightRange(renderIndex, editorSyntax.MultiLineCommentEnd.Length, HighlightMode.MultiLineComment);
                        renderIndex += editorSyntax.MultiLineCommentEnd.Length - 1;

                        isInMultiLineComment = false;
                        prevSeparator = true;
                        continue;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    if (editorRow.StartsWithAtPosition(editorSyntax.MultiLineCommentStart, renderIndex))
                    {
                        editorRow.SetHighlightRange(renderIndex, editorSyntax.MultiLineCommentStart.Length, HighlightMode.MultiLineComment);
                        renderIndex += editorSyntax.MultiLineCommentStart.Length - 1;

                        isInMultiLineComment = true;
                        prevSeparator = false;
                        continue;
                    }
                }
            }

            if ((editorSyntax.HighlightTypes & HighlightTypes.Strings) != 0)
            {
                if (isInString != null)
                {
                    editorRow.SetHighlightAt(renderIndex, HighlightMode.String);
                    if (renderChar == '\\' && renderIndex + 1 < editorRow.RenderChars.Count)
                    {
                        editorRow.SetHighlightAt(renderIndex + 1, HighlightMode.String);
                        renderIndex += 1;
                        continue;
                    }

                    if (renderChar == isInString.Value)
                    {
                        isInString = null;
                    }

                    prevSeparator = true;
                    continue;
                }
                else
                {
                    if (renderChar == '"' || renderChar == '\'')
                    {
                        isInString = renderChar;
                        editorRow.SetHighlightAt(renderIndex, HighlightMode.String);
                        continue;
                    }
                }
            }

            if ((editorSyntax.HighlightTypes & HighlightTypes.Numbers) != 0)
            {
                if (char.IsDigit(renderChar)
                    && (prevSeparator || prevHighlightMode == HighlightMode.Number)
                    || (renderChar == '.' && prevHighlightMode == HighlightMode.Number))
                {
                    if (renderIndex > 0 && (editorRow.RenderChars[renderIndex - 1] == '-' || editorRow.RenderChars[renderIndex - 1] == '+'))
                    {
                        editorRow.SetHighlightAt(renderIndex - 1, HighlightMode.Number);
                    }

                    editorRow.SetHighlightAt(renderIndex, HighlightMode.Number);
                }
            }

            if (prevSeparator)
            {
                foreach (var keyword1 in editorSyntax.Keyword1Items)
                {
                    if (!editorRow.StartsWithAtPosition(keyword1, renderIndex))
                    {
                        continue;
                    }

                    char? nextChar = renderIndex + keyword1.Length < editorRow.RenderChars.Count ? editorRow.RenderChars[renderIndex + keyword1.Length] : null;
                    if (nextChar == null || IsCharSeparator(nextChar.Value))
                    {
                        editorRow.SetHighlightRange(renderIndex, keyword1.Length, HighlightMode.Keyword1);
                        renderIndex += keyword1.Length - 1;
                        break;
                    }
                }

                foreach (var keyword2 in editorSyntax.Keyword2Items)
                {
                    if (!editorRow.StartsWithAtPosition(keyword2, renderIndex))
                    {
                        continue;
                    }

                    char? nextChar = renderIndex + keyword2.Length < editorRow.RenderChars.Count ? editorRow.RenderChars[renderIndex + keyword2.Length] : null;
                    if (nextChar == null || IsCharSeparator(nextChar.Value) || nextChar == '?')
                    {
                        editorRow.SetHighlightRange(renderIndex, keyword2.Length, HighlightMode.Keyword2);
                        renderIndex += keyword2.Length - 1;

                        if (nextChar == '?')
                        {
                            editorRow.SetHighlightAt(renderIndex, HighlightMode.Keyword2);
                            renderIndex += 1;
                        }

                        break;
                    }
                }
            }

            prevSeparator = IsCharSeparator(renderChar);
        }

        bool isChanged = editorRow.HasHighlightOpenComment != isInMultiLineComment;
        editorRow.HasHighlightOpenComment = isInMultiLineComment;

        if (isChanged && editorRow.RowIndex + 1 < editorRows.Count)
        {
            UpdateSyntax(editorRows[editorRow.RowIndex + 1]);
        }
    }

    bool IsCharSeparator(char ch)
    {
        return char.IsSeparator(ch) || ",.()+-/*=~%<>[];".IndexOf(ch) != -1;
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
}