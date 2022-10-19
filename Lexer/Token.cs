namespace Rust2SharpTranslator.Lexer;

public abstract record Token;

public enum KeywordType
{
    As,
    Break,
    Const,
    Crate,
    Continue,
    Else,
    Enum,
    Extern,
    False,
    Fn,
    For,
    If,
    Impl,
    In,
    Let,
    Loop,
    Match,
    Mod,
    Move,
    Mut,
    Pub,
    Ref,
    Return,
    Self,
    SelfType,
    Static,
    Struct,
    Super,
    Trait,
    True,
    Type,
    Unsafe,
    Use,
    Where,
    While,
    Async,
    Await,
    Dyn,
    Underscore
}

public record Keyword(KeywordType Value) : Token;

public record Identifier(string Value) : Token;

public enum LiteralType
{
    Char,
    String,
    Byte,
    ByteString,
    Integer,
    Float,
    Label
}

public record Literal(LiteralType Type, string Value) : Token;

public enum PunctuationType
{
    Plus,
    Minus,
    Star,
    Slash,
    Percent,
    Caret,
    Not,
    And,
    Or,
    AndAnd,
    OrOr,
    Shl,
    Shr,
    PlusEq,
    MinusEq,
    StarEq,
    SlashEq,
    PercentEq,
    CaretEq,
    AndEq,
    OrEq,
    ShlEq,
    ShrEq,
    Eq,
    EqEq,
    Ne,
    Gt,
    Lt,
    Ge,
    Le,
    At,
    Dot,
    DotDot,
    DotDotDot,
    DotDotEq,
    Comma,
    Semi,
    Colon,
    PathSep,
    RArrow,
    FatArrow,
    Pound,
    Dollar,
    Question,
    Tilde,
    OpenParen,
    CloseParen,
    OpenBrace,
    CloseBrace,
    OpenBracket,
    CloseBracket
}

public record Punctuation(PunctuationType Value) : Token;

public enum CommentType
{
    Line,
    Block,
    DocLine,
    DocBlock
}

public record Comment(CommentType Type, string Value) : Token;
