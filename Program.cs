Console.OutputEncoding = System.Text.Encoding.UTF8;

var terminal = Terminal.Current;

using var _ = terminal.EnableRawMode();

var editor = new Editor(terminal, new EditorSettings());
editor.Start(args.Length > 0 ? args[0] : null);