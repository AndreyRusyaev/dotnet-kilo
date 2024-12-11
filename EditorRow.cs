class EditorRow
{
    private readonly EditorSettings editorSettings;

    private readonly List<char> rawChars = new List<char>();

    private readonly List<char> renderChars = new List<char>();

    private HighlightMode[] highlightModes = Array.Empty<HighlightMode>();

    public EditorRow(EditorSettings editorSettings, int index, IEnumerable<char> chars)
    {
        this.editorSettings = editorSettings;
        RowIndex = index;
        rawChars.AddRange(chars);
    }

    public int RowIndex { get; set; }

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

    public bool HasHighlightOpenComment { get; set; }

    public void InsertCharAt(int index, char ch)
    {
        rawChars.Insert(index, ch);
    }

    public void RemoveCharAt(int index)
    {
        rawChars.RemoveAt(index);
    }

    public void Append(IEnumerable<char> newChars)
    {
        rawChars.AddRange(newChars);
    }

    public void Truncate(int startIndex)
    {
        if (startIndex < rawChars.Count)
        {
            rawChars.RemoveRange(startIndex, rawChars.Count - startIndex);
        }
    }

    public int GetRenderIndex(int charIndex)
    {
        int renderIndex = 0;
        for (int ii = 0; ii < rawChars.Count; ii += 1)
        {
            if (ii == charIndex)
            {
                return renderIndex;
            }

            if (rawChars[ii] == '\t')
            {
                renderIndex += (editorSettings.KILO_TAB_STOP - 1) - (renderIndex % editorSettings.KILO_TAB_STOP);
            }

            renderIndex += 1;
        }

        return renderIndex;
    }

    public int Find(string query)
    {
        return new string(rawChars.ToArray()).IndexOf(query, StringComparison.OrdinalIgnoreCase);
    }

    public bool StartsWithAtPosition(string input, int position)
    {
        return CompareOrdinalString(renderChars, position, input.Length, input);
    }

    public void SetHighlightAt(int index, HighlightMode highlightMode)
    {
        highlightModes[index] = highlightMode;
    }

    public void SetHighlightRange(int startIndex, HighlightMode highlightMode)
    {
        for (int ii = startIndex; ii < highlightModes.Length; ii += 1)
        {
            highlightModes[ii] = highlightMode;
        }
    }

    public void SetHighlightRange(int startIndex, int length, HighlightMode highlightMode)
    {
        for (int ii = startIndex; ii < startIndex + length && ii < highlightModes.Length; ii += 1)
        {
            highlightModes[ii] = highlightMode;
        }
    }

    public void UpdateRow()
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

        if (highlightModes.Length != renderChars.Count)
        {
            highlightModes = new HighlightMode[renderChars.Count];
        }

        for (int ii = 0; ii < renderChars.Count; ii += 1)
        {
            highlightModes[ii] = HighlightMode.Normal;
        }
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