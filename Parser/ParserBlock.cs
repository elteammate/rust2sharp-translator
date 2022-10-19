using System.Diagnostics;
using Rust2SharpTranslator.Lexer;
using Rust2SharpTranslator.Utils;

namespace Rust2SharpTranslator.Parser;

public partial class Parser
{
    public RsBlock ParseBlock()
    {
        Debug.Assert(_stream.IfMatchConsume(new Punctuation(PunctuationType.OpenBrace)));

        var statements = new List<RsStatement>();
        RsExpression? expression = null;

        while (!_stream.IfMatchConsume(new Punctuation(PunctuationType.CloseBrace)))
        {
            var statement = ParseStatement();

            if (_stream.Peek() == new Punctuation(PunctuationType.Semi))
            {
                _stream.Next();
                statements.Add(statement);
            }
            else if (_stream.Peek() == new Punctuation(PunctuationType.CloseBrace))
                expression = (statement as RsExpression).Unwrap();
            else
                throw new UnexpectedTokenException(_stream.Peek());
        }

        return new RsBlock(statements.ToArray(), expression);
    }

    public RsStatement ParseStatement()
    {
        var token = _stream.Peek();
        switch (token)
        {
            case Punctuation { Value: PunctuationType.Semi }:
                return new RsEmptyStatement();

            case Keyword { Value: KeywordType.Let }:
                return ParseLet();

            case Keyword { Value: KeywordType.Return }:
                _stream.Next();
                return new RsReturn(ParseExpression());

            case Keyword { Value: KeywordType.Break }:
                _stream.Next();
                return _stream.Peek() == new Punctuation(PunctuationType.Semi)
                    ? new RsBreak(null)
                    : new RsBreak(ParseExpression());

            case Keyword { Value: KeywordType.Continue }:
                _stream.Next();
                return new RsContinue();

            case Keyword { Value: KeywordType.If }:
                return ParseIf();

            case Keyword { Value: KeywordType.While }:
                return ParseWhile();

            case Keyword { Value: KeywordType.Loop }:
                return ParseLoop();

            case Keyword { Value: KeywordType.For }:
                return ParseFor();

            case Keyword { Value: KeywordType.Match }:
                return ParseMatch();

            default:
                return ParseExpression();
        }
    }

    public RsLet ParseLet()
    {
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.Let));
        var mut = _stream.IfMatchConsume(new Keyword(KeywordType.Mut));

        var name = ParseName();
        var type = _stream.IfMatchConsume(new Punctuation(PunctuationType.Colon))
            ? ParseExpression(BinaryPrecedence.Assign + 1)
            : null;

        Debug.Assert(_stream.Next() == new Punctuation(PunctuationType.Eq));
        var value = ParseExpression();

        return new RsLet(name, mut, type, value);
    }

    public RsIf ParseIf() => throw new NotImplementedException();

    public RsLoop ParseLoop() => throw new NotImplementedException();

    public RsWhile ParseWhile() => throw new NotImplementedException();

    public RsFor ParseFor() => throw new NotImplementedException();

    public RsMatch ParseMatch() => throw new NotImplementedException();
}
