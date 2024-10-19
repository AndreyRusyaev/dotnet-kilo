class EditorSyntax
{
    public EditorSyntax(
        string name,
        string[] fileMatches,
        string singleLineCommentStart,
        string multiLineCommentStart,
        string multiLineCommentEnd,
        string[] keyword1Items,
        string[] keyword2Items,
        HighlightTypes supportedHighlightTypes)
    {
        Name = name;
        FileMatches = fileMatches;
        SingleLineCommentStart = singleLineCommentStart;
        MultiLineCommentStart = multiLineCommentStart;
        MultiLineCommentEnd = multiLineCommentEnd;
        Keyword1Items = keyword1Items;
        Keyword2Items = keyword2Items;
        HighlightTypes = supportedHighlightTypes;
    }

    public string Name { get; }

    public string[] FileMatches { get; }

    public string SingleLineCommentStart { get; }

    public string MultiLineCommentStart { get; }

    public string MultiLineCommentEnd { get; }

    public string[] Keyword1Items { get; }

    public string[] Keyword2Items { get; }

    public HighlightTypes HighlightTypes { get; }
}