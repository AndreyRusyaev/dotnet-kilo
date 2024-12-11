class HighlightDatabase
{
    public static EditorSyntax[] Entries = [
        new EditorSyntax(
            "C/C++",
            [".c", ".h", ".cpp", ".hpp", ".cc"],
            "//", "/*", "*/",
            [ 
                /* C Keywords */
                "auto","break","case","continue","default","do","else","enum",
                "extern","for","goto","if","register","return","sizeof","static",
                "struct","switch","typedef","union","volatile","while","NULL",

                /* C++ Keywords */
                "alignas","alignof","and","and_eq","asm","bitand","bitor","class",
                "compl","constexpr","const_cast","deltype","delete","dynamic_cast",
                "explicit","export","false","friend","inline","mutable","namespace",
                "new","noexcept","not","not_eq","nullptr","operator","or","or_eq",
                "private","protected","public","reinterpret_cast","static_assert",
                "static_cast","template","this","thread_local","throw","true","try",
                "typeid","typename","virtual","xor","xor_eq",
            ],
            [ 
                /* C types */
                "int","long","double","float","char","unsigned","signed", "void","short","auto","const","bool",
            ],
            HighlightTypes.Numbers | HighlightTypes.Strings),
        new EditorSyntax(
            "CSharp",
            [".cs"],
            "//", "/*", "*/",
            [ 
                // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/
                // non-contextual
                "abstract", "as", "base", "break", "case", "catch", "checked", "class", "const", "continue", "default",
                "delegate", "do", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "for",
                "foreach", "goto", "if", "implicit", "in", "interface", "internal", "is", "lock", "namespace", "new",
                "null", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref",
                "return", "sealed", "sizeof", "stackalloc", "static", "struct", "switch", "this", "throw", "true",
                "try", "typeof", "unchecked", "unsafe", "using", "virtual", "volatile", "while", 
                // contextual
                "add", "allows", "alias", "and", "ascending", "args", "async", "await", "by", "descending", "dynamic",
                "equals", "file", "from", "get", "global", "group", "init", "into", "join", "let", "managed", "nameof",
                "not", "notnull", "on", "or", "orderby", "partial", "record", "remove", "required", "scoped", "select",
                "set", "unmanaged", "value", "when", "where", "with", "yield",
            ],
            [ 
                // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
                "var", "bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint", "nint", "nuint",
                "long", "ulong", "short", "ushort", "object", "string", "dynamic", "void"
            ],
            HighlightTypes.Numbers | HighlightTypes.Strings)
    ];

    public static EditorSyntax? ResolveSyntax(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        foreach (var entry in Entries)
        {
            foreach (var fileMatch in entry.FileMatches)
            {
                var isMatchWithExtension = fileMatch.Length > 0 && fileMatch[0] == '.';

                if ((isMatchWithExtension && string.CompareOrdinal(extension, fileMatch) == 0)
                    || (!isMatchWithExtension && string.CompareOrdinal(fileName, fileMatch) == 0))
                {
                    return entry;
                }
            }
        }

        return null;
    }
}

