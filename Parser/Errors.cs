using Rust2SharpTranslator.Lexer;

namespace Rust2SharpTranslator.Parser;

public abstract class ParserException : Exception {
    protected ParserException(string message): base($"Parser error: {message}") {}
}

public class ExpressionException : ParserException {
    public ExpressionException(): base("Expected Expression") {}
}

public class UnexpectedEndOfStreamException : ParserException {
    public UnexpectedEndOfStreamException(): base("Unexpected end of stream") {}
}

public class UnexpectedTokenException : ParserException {
    public UnexpectedTokenException(Token token): base($"Unexpected punctuation: {token}") {}
}
