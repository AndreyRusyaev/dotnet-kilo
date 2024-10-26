class EditorRow
{
    private readonly EditorSettings editorSettings;

    private readonly List<char> rawChars = new List<char>();

    private readonly List<char> renderChars = new List<char>();

    private HighlightMode[] highlightModes = Array.Empty<HighlightMode>();

    public EditorRow(EditorSettings editorSettings, int index, IEnumerable<char> chars)
    {
        this.editorSettings = editorSettings;
        Index = index;
        rawChars.AddRange(chars);
    }

    public int Index { get; set; }

    public IReadOnlyList<char> Chars { get { return rawChars.AsReadOnly(); } }

    public int CharSize
    {
        get { return rawChars.Count; }
    }

    public IReadOnlyList<char> RenderChars { get { return renderChars.AsReadOnly(); } }

    public IReadOnlyList<HighlightMode> HighlightModes { get { return highlightModes.AsReadOnly(); } }

    public int RenderSize
    {
        get { return renderChars.Count; }
    }

    public bool HasHighlightOpenComment { get; private set; }

    public void InsertChar(int position, char ch)
    {
        rawChars.Insert(position, ch);
    }

    public void RemoveChar(int position)
    {
        rawChars.RemoveAt(position);
    }

    public void AppendString(IEnumerable<char> newChars)
    {
        rawChars.AddRange(newChars);
    }

    public void Truncate(int position)
    {
        if (position < rawChars.Count)
        {
            rawChars.RemoveRange(position, rawChars.Count - position);
        }
    }

    public int ToRenderPosX(int targetCurrentPosX)
    {
        int currentPosX = 0;
        int renderPosX = 0;
        for (int rawCharIndex = 0; rawCharIndex < rawChars.Count; rawCharIndex += 1)
        {
            if (currentPosX == targetCurrentPosX)
            {
                return renderPosX;
            }

            currentPosX += 1;

            if (rawChars[rawCharIndex] == '\t')
            {
                renderPosX += (editorSettings.KILO_TAB_STOP - 1) - (renderPosX % editorSettings.KILO_TAB_STOP);
            }

            renderPosX += 1;
        }

        return renderPosX;
    }

    public int ToCharPosX(int targetRenderPosX)
    {
        int currentPosX = 0;
        int renderPosX = 0;
        for (int rawCharIndex = 0; rawCharIndex < rawChars.Count; rawCharIndex += 1)
        {
            if (renderPosX >= targetRenderPosX)
            {
                return currentPosX;
            }

            currentPosX += 1;

            if (rawChars[rawCharIndex] == '\t')
            {
                renderPosX += (editorSettings.KILO_TAB_STOP - 1) - (renderPosX % editorSettings.KILO_TAB_STOP);
            }

            renderPosX += 1;
        }

        return currentPosX;
    }

    public int Find(string query)
    {
        return new string(RenderChars.ToArray()).IndexOf(query, StringComparison.OrdinalIgnoreCase);
    }

    public void SetHighlight(int startIndex, int length, HighlightMode highlightMode)
    {
        for (int renderIndex = startIndex; renderIndex < startIndex + length; renderIndex += 1)
        {
            highlightModes[renderIndex] = highlightMode;
        }
    }

    public void UpdateRow(EditorContext editorContext)
    {
        renderChars.Clear();
        foreach (var ch in rawChars)
        {
            if (ch == '\t')
            {
                renderChars.Add(' ');
                while ((renderChars.Count % editorSettings.KILO_TAB_STOP) != 0)
                {
                    renderChars.Add(' ');
                }
            }
            else
            {
                renderChars.Add(ch);
            }
        }

        UpdateSyntax(editorContext);
    }

    private void UpdateSyntax(EditorContext editorContext)
    {
        if (highlightModes.Length != renderChars.Count)
        {
            highlightModes = new HighlightMode[renderChars.Count];
        }

        for (int renderIndex = 0; renderIndex < renderChars.Count; renderIndex += 1)
        {
            highlightModes[renderIndex] = HighlightMode.Normal;
        }

        var editorSyntax = editorContext.EditorSyntax;
        if (editorSyntax == null)
        {
            return;
        }

        string singleLineCommentStart = editorSyntax.SingleLineCommentStart;
        string multiLineCommentStart = editorSyntax.MultiLineCommentStart;
        string multiLineCommentEnd = editorSyntax.MultiLineCommentEnd;

        bool prevSeparator = true;
        char? inString = null;
        bool inComment = Index > 0 ? editorContext.EditorRows[Index - 1].HasHighlightOpenComment : false;

        for (int renderIndex = 0; renderIndex < renderChars.Count; renderIndex += 1)
        {
            var ch = renderChars[renderIndex];
            var prevHighlightMode = renderIndex > 0 ? highlightModes[renderIndex - 1] : HighlightMode.Normal;

            if (singleLineCommentStart != null && singleLineCommentStart.Length > 0 && inString == null && !inComment)
            {
                bool isSingleLineCommentStarted =
                    CompareOrdinalString(renderChars, renderIndex, singleLineCommentStart.Length, singleLineCommentStart);
                if (isSingleLineCommentStarted)
                {
                    while (renderIndex < renderChars.Count)
                    {
                        highlightModes[renderIndex++] = HighlightMode.Comment;
                    }

                    break;
                }
            }

            if (multiLineCommentStart != null && multiLineCommentStart.Length > 0
                && multiLineCommentEnd != null && multiLineCommentEnd.Length > 0
                && inString == null)
            {
                if (inComment)
                {
                    highlightModes[renderIndex] = HighlightMode.MultiLineComment;

                    bool isMultilineCommentEnded = CompareOrdinalString(renderChars, renderIndex, multiLineCommentEnd.Length, multiLineCommentEnd);
                    if (isMultilineCommentEnded)
                    {
                        for (int index = 0; index < multiLineCommentEnd.Length && renderIndex < renderChars.Count; index += 1)
                        {
                            highlightModes[renderIndex + index] = HighlightMode.MultiLineComment;
                        }

                        renderIndex += multiLineCommentEnd.Length - 1;

                        inComment = false;
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
                    bool isMultilineCommentStarted = CompareOrdinalString(renderChars, renderIndex, multiLineCommentStart.Length, multiLineCommentStart);
                    if (isMultilineCommentStarted)
                    {
                        for (int index = 0; index < multiLineCommentStart.Length && renderIndex < renderChars.Count; index += 1)
                        {
                            highlightModes[renderIndex + index] = HighlightMode.MultiLineComment;
                        }

                        renderIndex += multiLineCommentStart.Length - 1;

                        inComment = true;
                        prevSeparator = false;
                        continue;
                    }
                }
            }

            if ((editorSyntax.HighlightTypes & HighlightTypes.Strings) != 0)
            {
                if (inString != null)
                {
                    highlightModes[renderIndex] = HighlightMode.String;
                    if (ch == '\\' && renderIndex + 1 < renderChars.Count)
                    {
                        highlightModes[renderIndex + 1] = HighlightMode.String;
                        renderIndex += 1;
                        continue;
                    }

                    if (ch == inString.Value)
                    {
                        inString = null;
                    }

                    prevSeparator = true;
                    continue;
                }
                else
                {
                    if (ch == '"' || ch == '\'')
                    {
                        inString = ch;
                        highlightModes[renderIndex] = HighlightMode.String;
                        continue;
                    }
                }
            }

            if ((editorSyntax.HighlightTypes & HighlightTypes.Numbers) != 0)
            {
                if (char.IsDigit(ch)
                    && (prevSeparator || prevHighlightMode == HighlightMode.Number)
                    || (ch == '.' && prevHighlightMode == HighlightMode.Number))
                {
                    if (renderIndex > 0 && (renderChars[renderIndex - 1] == '-' || renderChars[renderIndex - 1] == '+'))
                    {
                        highlightModes[renderIndex - 1] = HighlightMode.Number;
                    }

                    highlightModes[renderIndex] = HighlightMode.Number;
                }
            }

            if (prevSeparator)
            {
                foreach (var keyword1 in editorSyntax.Keyword1Items)
                {
                    if (CompareOrdinalString(renderChars, renderIndex, keyword1.Length, keyword1))
                    {
                        char? nextChar = renderIndex + keyword1.Length < renderChars.Count ? renderChars[renderIndex + keyword1.Length] : null;
                        if (nextChar == null || IsSeparator(nextChar.Value))
                        {
                            var initialIndex = 0;
                            while (initialIndex < keyword1.Length)
                            {
                                highlightModes[renderIndex + initialIndex] = HighlightMode.Keyword1;
                                initialIndex += 1;
                            }

                            renderIndex += keyword1.Length - 1;

                            break;
                        }
                    }
                }

                foreach (var keyword2 in editorSyntax.Keyword2Items)
                {
                    if (CompareOrdinalString(renderChars, renderIndex, keyword2.Length, keyword2))
                    {
                        char? nextChar = renderIndex + keyword2.Length < renderChars.Count ? renderChars[renderIndex + keyword2.Length] : null;
                        if (nextChar == null || IsSeparator(nextChar.Value) || nextChar == '?')
                        {
                            var initialIndex = 0;
                            while (initialIndex < keyword2.Length)
                            {
                                highlightModes[renderIndex + initialIndex] = HighlightMode.Keyword2;
                                initialIndex += 1;
                            }

                            if (nextChar == '?')
                            {
                                highlightModes[renderIndex + initialIndex] = HighlightMode.Keyword2;
                                initialIndex += 1;
                            }

                            renderIndex += initialIndex - 1;

                            break;
                        }
                    }
                }
            }

            prevSeparator = IsSeparator(ch);
        }

        bool isChanged = HasHighlightOpenComment != inComment;
        HasHighlightOpenComment = inComment;

        if (isChanged && Index + 1 < editorContext.EditorRows.Count)
        {
            editorContext.EditorRows[Index + 1].UpdateSyntax(editorContext);
        }
    }

    private bool IsSeparator(char ch)
    {
        return char.IsSeparator(ch) || ",.()+-/*=~%<>[];".IndexOf(ch) != -1;
    }

    private bool CompareOrdinalString(IReadOnlyList<char> source, int sourceStartIndex, int sourceLength, string target)
    {
        if (target.Length == 0 || sourceLength == 0)
        {
            return false;
        }

        if (sourceLength != target.Length)
        {
            return false;
        }

        bool isEqual = false;
        for (int index = 0; index < target.Length; index += 1)
        {
            if (sourceStartIndex + index < source.Count)
            {
                if (source[sourceStartIndex + index] == target[index])
                {
                    isEqual = true;
                    continue;
                }
            }

            isEqual = false;
            break;
        }

        return isEqual;
    }
}