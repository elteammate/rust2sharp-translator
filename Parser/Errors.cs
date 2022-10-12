namespace Rust2SharpTranslator.Parser;

public abstract class ParserException : Exception {
    protected ParserException(string message): base($"Parser error: {message}") {}
}

public class ExpressionException : ParserException {
    public ExpressionException(): base("Expected Expression") {}
}

public class UnexpectedLiteralException : ParserException {
    public UnexpectedLiteralException(string literal): base($"Unexpected literal: {literal}") {}
}
