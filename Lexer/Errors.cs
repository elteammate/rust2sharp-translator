namespace Rust2SharpTranslator.Lexer;

public abstract class LexerException : Exception {
    protected LexerException(string message): base($"Lexer error: {message}") {}
}

public class NoSuchEscapeInStringLiteralException : LexerException
{
    public NoSuchEscapeInStringLiteralException(char escape)
        : base($"No such escape sequence: \\{escape}")
    {}
}


public class UnterminatedStringLiteralException : LexerException
{
    public UnterminatedStringLiteralException()
        : base("Unterminated string literal")
    {}
}


public class UndefinedPunctuationException : LexerException
{
    public UndefinedPunctuationException(string punctuation)
        : base($"Undefined punctuation: {punctuation}")
    {}
}


public class UnexpectedCharacterException : LexerException
{
    public UnexpectedCharacterException(char c) : base("Unexpected character: " + c)
    {}
}
