using Rust2SharpTranslator.Lexer;
using Rust2SharpTranslator.Utils;

namespace Rust2SharpTranslator.Parser;

/// <summary>
///     Base of the parser class
/// </summary>
public partial class Parser
{
    private readonly Stream<Token> _stream;

    public Parser(IEnumerable<Token> tokens)
    {
        _stream = new Stream<Token>(tokens);
    }
}
