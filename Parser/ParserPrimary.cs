using Rust2SharpTranslator.Lexer;

namespace Rust2SharpTranslator.Parser;

public partial class Parser
{
    public RsExpression ParsePrimaryExpression()
    {
        var token = _stream.Next();
        return token switch
        {
            Identifier identifier => new RsName(identifier.Value),
            
            Literal literal => literal.Type switch
            {
                LiteralType.Char => new RsLiteralChar(literal.Value),
                LiteralType.String => new RsLiteralString(literal.Value),
                LiteralType.Byte => new RsLiteralByte(literal.Value),
                LiteralType.ByteString => new RsLiteralByteString(literal.Value),
                LiteralType.Integer => new RsLiteralInt(literal.Value),
                LiteralType.Float => new RsLiteralFloat(literal.Value),
                LiteralType.Label => throw new UnexpectedTokenException(literal),
                _ => throw new ArgumentOutOfRangeException()
            },

            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            Keyword keyword => keyword.Value switch
            {
                KeywordType.Self => new RsSelf(),
                KeywordType.Super => new RsSuper(),
                KeywordType.True => new RsLiteralBool("true"),
                KeywordType.False => new RsLiteralBool("false"),
                KeywordType.Underscore => new RsUnderscore(),
                _ => throw new UnexpectedTokenException(keyword)
            },
            
            null => throw new UnexpectedEndOfStreamException(),
            _ => throw new UnexpectedTokenException(token)
        };
    }
}
