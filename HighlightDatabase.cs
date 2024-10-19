[Flags]
enum HighlightTypes {
    Numbers = 1 << 0,
    Strings = 1 << 1
}

class HighlightDatabase
{
    public static EditorSyntax[] Entries = [
        new EditorSyntax(
            "CSharp", 
            [".cs"], 
            "//", "/*", "*/", 
            [ 
                // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/
                // non-contextual
                "abstract", "as", "base", "break", "case", "catch", "checked", "class", "const", "continue", "default", "delegate", "do", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "for", "foreach", "goto", "if", "implicit", "in", "interface", "internal", "is", "lock", "namespace", "new", "null", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sealed", "sizeof", "stackalloc", "static", "struct", "switch", "this", "throw", "true", "try", "typeof", "unchecked", "unsafe", "using", "virtual", "volatile", "while", 
                // contextual
                "add", "allows", "alias", "and", "ascending", "args", "async", "await", "by", "descending", "dynamic", "equals", "file", "from", "get", "global", "group", "init", "into", "join", "let", "managed", "nameof", "not", "notnull", "on", "or", "orderby", "partial", "record", "remove", "required", "scoped", "select", "set", "unmanaged", "value", "when", "where", "with", "yield", 
            ],
            [ 
                // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
                "var", "bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint", "nint", "nuint", "long", "ulong", "short", "ushort", "object", "string", "dynamic"
            ],
            HighlightTypes.Numbers | HighlightTypes.Strings)
    ];
}

