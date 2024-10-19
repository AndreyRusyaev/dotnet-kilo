class EditorContext
{
    private readonly Editor editor;

    public EditorContext(Editor editor)
    {
        this.editor = editor;
    }

    public EditorSyntax? EditorSyntax => editor.EditorSyntax;

    public IReadOnlyList<EditorRow> EditorRows => editor.EditorRows;
}