Console.OutputEncoding = System.Text.Encoding.UTF8;

using var _ = RawConsole.EnableRawMode();

var editor = new Editor(new EditorSettings());
editor.Start(args.Length > 0 ? args[0] : null);