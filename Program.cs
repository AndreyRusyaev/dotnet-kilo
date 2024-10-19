RawConsole.EnableRawMode();

var editor = new Editor(new EditorSettings());
editor.Start(args.Length > 0 ? args[0] : null);
