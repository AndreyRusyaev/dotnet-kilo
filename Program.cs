Console.OutputEncoding = System.Text.Encoding.UTF8;

using var _ = Terminal.EnableRawMode();

while (Terminal.TryReadEvent(out var _))
{
    // Clear any pending input events after enabling raw mode.
}

var editor = new Editor(new EditorSettings());
editor.Start(args.Length > 0 ? args[0] : null);