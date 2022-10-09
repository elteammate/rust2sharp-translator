namespace Translator.Utils;

public static class Consumers
{
    public static bool IsSpace(char c) => c is ' ' or '\t' or '\r' or '\n';
    public static bool IsDigit(char c) => c is >= '0' and <= '9';
    public static bool IsHexDigit(char c) => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
    public static bool IsLetter(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    public static bool IsLetterOrDigit(char c) => IsLetter(c) || IsDigit(c);
    public static bool IsIdentifierStart(char c) => IsLetter(c) || c == '_';
    public static bool IsIdentifierPart(char c) => IsLetterOrDigit(c) || c == '_';

    public static bool IsPunctuation(char c) => c is
        '+' or '-' or '*' or '/' or '%' or '&' or '|' or
        '^' or '!' or '~' or '=' or '<' or '>' or '.' or
        ',' or ';' or '@' or ':' or '#' or '$' or '?' or 
        '(' or ')' or '{' or '}' or '[' or ']';
    
    public static bool IsStackablePunctuation(char c) => c is 
        '+' or '-' or '*' or '/' or '%' or '&' or '|' or 
        '^' or '~' or '=' or '<' or '>' or '.' or ':';
    
    public static bool IsSingleCharPunctuation(char c) => IsPunctuation(c) && !IsStackablePunctuation(c);

    public static KeywordType? TryGetKeyword(string identifierOrKeyword) =>
        identifierOrKeyword switch
        {
            "as" => KeywordType.As,
            "break" => KeywordType.Break,
            "const" => KeywordType.Const,
            "crate" => KeywordType.Crate,
            "continue" => KeywordType.Continue,
            "else" => KeywordType.Else,
            "enum" => KeywordType.Enum,
            "extern" => KeywordType.Extern,
            "false" => KeywordType.False,
            "fn" => KeywordType.Fn,
            "for" => KeywordType.For,
            "if" => KeywordType.If,
            "impl" => KeywordType.Impl,
            "in" => KeywordType.In,
            "let" => KeywordType.Let,
            "loop" => KeywordType.Loop,
            "match" => KeywordType.Match,
            "mod" => KeywordType.Mod,
            "move" => KeywordType.Move,
            "mut" => KeywordType.Mut,
            "pub" => KeywordType.Pub,
            "ref" => KeywordType.Ref,
            "return" => KeywordType.Return,
            "self" => KeywordType.Self,
            "Self" => KeywordType.SelfType,
            "static" => KeywordType.Static,
            "struct" => KeywordType.Struct,
            "super" => KeywordType.Super,
            "trait" => KeywordType.Trait,
            "true" => KeywordType.True,
            "type" => KeywordType.Type,
            "unsafe" => KeywordType.Unsafe,
            "use" => KeywordType.Use,
            "where" => KeywordType.Where,
            "while" => KeywordType.While,
            "async" => KeywordType.Async,
            "await" => KeywordType.Await,
            "dyn" => KeywordType.Dyn,
            "_" => KeywordType.Underscore,
            _ => null
        };

    public static PunctuationType? TryGetPunctuation(string op)
    {
        return op switch
        {
            "+" => PunctuationType.Plus,
            "-" => PunctuationType.Minus,
            "*" => PunctuationType.Star,
            "/" => PunctuationType.Slash,
            "%" => PunctuationType.Percent,
            "&" => PunctuationType.And,
            "|" => PunctuationType.Or,
            "^" => PunctuationType.Caret,
            "!" => PunctuationType.Not,
            "~" => PunctuationType.Tilde,
            "=" => PunctuationType.Eq,
            "<" => PunctuationType.Lt,
            ">" => PunctuationType.Gt,
            "." => PunctuationType.Dot,
            "," => PunctuationType.Comma,
            ";" => PunctuationType.Semi,
            "@" => PunctuationType.At,
            ":" => PunctuationType.Colon,
            "#" => PunctuationType.Pound,
            "$" => PunctuationType.Dollar,
            "?" => PunctuationType.Question,
            "+=" => PunctuationType.PlusEq,
            "-=" => PunctuationType.MinusEq,
            "*=" => PunctuationType.StarEq,
            "/=" => PunctuationType.SlashEq,
            "%=" => PunctuationType.PercentEq,
            "&=" => PunctuationType.AndEq,
            "|=" => PunctuationType.OrEq,
            "^=" => PunctuationType.CaretEq,
            "<<" => PunctuationType.Shl,
            ">>" => PunctuationType.Shr,
            "==" => PunctuationType.EqEq,
            "!=" => PunctuationType.Ne,
            "<=" => PunctuationType.Le,
            ">=" => PunctuationType.Ge,
            "&&" => PunctuationType.AndAnd,
            "||" => PunctuationType.OrOr,
            "<<=" => PunctuationType.ShlEq,
            ">>=" => PunctuationType.ShrEq,
            "=>" => PunctuationType.FatArrow,
            "->" => PunctuationType.RArrow,
            ".." => PunctuationType.DotDot,
            "..=" => PunctuationType.DotDotEq,
            "..." => PunctuationType.DotDotDot,
            "::" => PunctuationType.PathSep,
            "{" => PunctuationType.OpenBrace,
            "}" => PunctuationType.CloseBrace,
            "(" => PunctuationType.OpenParen,
            ")" => PunctuationType.CloseParen,
            "[" => PunctuationType.OpenBracket,
            "]" => PunctuationType.CloseBracket,
            _ => null
        };
    }
}
