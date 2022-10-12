using System.Globalization;
using System.Numerics;
using Rust2SharpTranslator.Lexer;
using Rust2SharpTranslator.Utils;

namespace Rust2SharpTranslator.Parser;

public partial class Parser
{
    public RsLiteral ParseLiteral()
    {
        var literal = (Literal)_stream.Next().Unwrap();
        return literal.Type switch
        {
            LiteralType.Char => new RsLiteralChar(literal.Value[0]),
            LiteralType.String => new RsLiteralString(literal.Value),
            LiteralType.Byte => new RsLiteralByte(literal.Value[0]),
            LiteralType.ByteString => new RsLiteralByteString(literal.Value),
            LiteralType.Integer => new RsLiteralInt(BigInteger.Parse(literal.Value, NumberStyles.Any)),
            LiteralType.Float => new RsLiteralFloat(double.Parse(literal.Value, NumberStyles.Any)),
            LiteralType.Label => throw new ArgumentException("Label literal is not expected here"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
