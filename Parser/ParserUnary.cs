using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using Rust2SharpTranslator.Lexer;

namespace Rust2SharpTranslator.Parser;

public partial class Parser
{
    public RsName ParseName()
    {
        var token = _stream.Next();
        return token switch
        {
            Identifier identifier => new RsName(identifier.Value),

            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            Keyword keyword => keyword.Value switch
            {
                KeywordType.Self => new RsSelf(),
                KeywordType.SelfType => new RsSelfType(),
                KeywordType.Super => new RsSuper(),
                KeywordType.Crate => new RsCrate(),
                KeywordType.Underscore => new RsUnderscore(),
                _ => throw new UnexpectedTokenException(keyword)
            },

            Literal literal => new RsName(literal.Value),

            null => throw new UnexpectedEndOfStreamException(),
            _ => throw new UnexpectedTokenException(token)
        };
    }

    public (RsLifetime[], RsGeneric[]) ParseLifetimesAndGenerics()
    {
        if (_stream.Peek() is not Punctuation { Value: PunctuationType.Lt })
            return (Array.Empty<RsLifetime>(), Array.Empty<RsGeneric>());

        _stream.Next();

        var lifetimes = new List<RsLifetime>();
        var generics = new List<RsGeneric>();

        while (!_stream.IfMatchConsume(new Punctuation(PunctuationType.Gt)))
        {
            var token = _stream.Peek();
            switch (token)
            {
                case Identifier:
                    generics.Add(new RsGeneric(ParseName(), Array.Empty<RsExpression>()));
                    break;
                case Literal { Type: LiteralType.Label } label:
                    _stream.Next();
                    lifetimes.Add(new RsLifetime(new RsLabel(label.Value)));
                    break;
                default:
                    throw new UnexpectedTokenException(token);
            }

            _stream.IfMatchConsume(new Punctuation(PunctuationType.Comma));
        }

        return (lifetimes.ToArray(), generics.ToArray());
    }

    public RsLiteral ParseLiteral()
    {
        var token = _stream.Next();
        return token switch
        {
            Literal { Type: LiteralType.String } literal => new RsLiteralString(literal.Value),
            Literal { Type: LiteralType.ByteString } literal => new RsLiteralByteString(
                literal.Value),
            Literal { Type: LiteralType.Char } literal => new RsLiteralChar(literal.Value),
            Literal { Type: LiteralType.Byte } literal => new RsLiteralByte(literal.Value),
            Keyword { Value: KeywordType.True } => new RsLiteralBool("true"),
            Keyword { Value: KeywordType.False } => new RsLiteralBool("false"),
            Literal { Type: LiteralType.Integer } literal => new RsLiteralInt(literal.Value),
            Literal { Type: LiteralType.Float } literal => new RsLiteralFloat(literal.Value),
            _ => throw new UnexpectedTokenException(token)
        };
    }

    public RsExpression[] ParseArguments()
    {
        Debug.Assert(_stream.IfMatchConsume(new Punctuation(PunctuationType.OpenParen)));

        var parameters = new List<RsExpression>();
        while (!_stream.IfMatchConsume(new Punctuation(PunctuationType.CloseParen)))
        {
            parameters.Add(ParseExpression());
            _stream.IfMatchConsume(new Punctuation(PunctuationType.Comma));
        }

        return parameters.ToArray();
    }

    public RsExpression ParsePrimaryExpressionNoPrefixUnary()
    {
        var token = _stream.Peek();
        RsExpression expr = token switch
        {
            Literal => ParseLiteral(),
            Keyword { Value: KeywordType.True } => ParseLiteral(),
            Keyword { Value: KeywordType.False } => ParseLiteral(),
            Identifier or Keyword => ParseName(),
            _ => throw new UnexpectedTokenException(token)
        };

        var keepGoing = true;

        while (keepGoing)
        {
            token = _stream.Peek();
            switch (token)
            {
                case Punctuation { Value: PunctuationType.OpenParen }:
                    expr = new RsCall(expr, ParseArguments());
                    break;

                case Punctuation { Value: PunctuationType.OpenBracket }:
                    _stream.Next();
                    expr = new RsIndex(expr, ParseExpression());
                    Debug.Assert(
                        _stream.IfMatchConsume(new Punctuation(PunctuationType.CloseBracket)));
                    break;

                case Punctuation { Value: PunctuationType.OpenBrace }:
                    if (!(
                            _stream.Peek(1) is Punctuation { Value: PunctuationType.CloseBrace } ||
                            _stream.Peek(2) is Punctuation { Value: PunctuationType.Colon }
                        ))
                    {
                        keepGoing = false;
                        break;
                    }

                    _stream.Next();
                    var fields = new List<(RsName, RsExpression)>();
                    while (!_stream.IfMatchConsume(new Punctuation(PunctuationType.CloseBrace)))
                    {
                        var name = ParseName();
                        Debug.Assert(
                            _stream.IfMatchConsume(new Punctuation(PunctuationType.Colon)));
                        var value = ParseExpression();
                        fields.Add((name, value));
                        _stream.IfMatchConsume(new Punctuation(PunctuationType.Comma));
                    }

                    expr = new RsConstructor(expr, fields.ToArray());
                    break;

                case Punctuation { Value: PunctuationType.Dot }:
                    _stream.Next();
                    expr = new RsField(expr, ParseName());
                    break;

                case Punctuation { Value: PunctuationType.PathSep }:
                    _stream.Next();
                    expr = new RsPath(expr, ParseName());
                    break;

                case Punctuation { Value: PunctuationType.Lt }:
                    var fork = _stream.Fork();
                    var seenClose = false;
                    while (fork.Peek() is not Punctuation
                           {
                               Value: PunctuationType.Semi or
                               PunctuationType.CloseParen or
                               PunctuationType.CloseBracket or
                               PunctuationType.CloseBrace
                           })
                    {
                        fork.Next();
                        if (fork.Peek() is not Punctuation { Value: PunctuationType.Gt }) continue;
                        seenClose = true;
                        break;
                    }

                    if (seenClose)
                    {
                        var (lifetimes, generics) = ParseLifetimesAndGenerics();
                        expr = new RsWithGenerics(expr, lifetimes, generics);
                    }
                    else
                        keepGoing = false;

                    break;
                default:
                    keepGoing = false;
                    break;
            }
        }

        return expr;
    }

    public RsLiteralArray ParseArray()
    {
        Debug.Assert(_stream.Next() == new Punctuation(PunctuationType.OpenBracket));
        var elements = new List<RsExpression>();

        while (!_stream.IfMatchConsume(new Punctuation(PunctuationType.CloseBracket)))
        {
            elements.Add(ParseExpression());
            _stream.IfMatchConsume(new Punctuation(PunctuationType.Comma));
        }

        return new RsLiteralArray(elements.ToArray());
    }

    private RsExpression ParseParenthesizedExpression()
    {
        Debug.Assert(_stream.Next() == new Punctuation(PunctuationType.OpenParen));
        if (_stream.IfMatchConsume(new Punctuation(PunctuationType.CloseParen)))
            return new RsLiteralUnit();

        var expr = ParseExpression();
        Debug.Assert(_stream.Next() == new Punctuation(PunctuationType.CloseParen));
        return expr;
    }

    public RsExpression ParsePrimaryExpression()
    {
        var token = _stream.Peek();

        switch (token)
        {
            case Punctuation { Value: PunctuationType.Minus }:
                _stream.Next();
                return new RsUnaryMinus(ParsePrimaryExpression());

            case Punctuation { Value: PunctuationType.Not }:
                _stream.Next();
                return new RsNot(ParsePrimaryExpression());

            case Punctuation { Value: PunctuationType.And }:
                _stream.Next();
                var lifetime = _stream.Peek() is Literal {Type: LiteralType.Label}
                    ? new RsLabel(((Literal)_stream.Next()!).Value)
                    : null;
                return _stream.IfMatchConsume(new Keyword(KeywordType.Mut))
                    ? new RsRef(lifetime, true, ParsePrimaryExpression())
                    : new RsRef(lifetime, false, ParsePrimaryExpression());

            case Punctuation { Value: PunctuationType.Star }:
                _stream.Next();
                return new RsDeref(ParsePrimaryExpression());

            case Punctuation { Value: PunctuationType.OpenParen }:
                return ParseParenthesizedExpression();
            case Punctuation { Value: PunctuationType.OpenBrace }:
                return ParseBlock();
            case Punctuation { Value: PunctuationType.OpenBracket }:
                return ParseArray();

            case Keyword { Value: KeywordType.If }:
                return ParseIf();
            case Keyword { Value: KeywordType.Loop }:
                return ParseLoop();
            case Keyword { Value: KeywordType.Match }:
                return ParseMatch();
            default:
                return ParsePrimaryExpressionNoPrefixUnary();
        }
    }
}

public class __TestParserPrimary__
{
    [Test]
    public void TestPrimary_Path_ParsedCorrectly()
    {
        var parser = new Parser(new Lexer.Lexer("foo::bar::baz").Lex());
        var expr = parser.ParsePrimaryExpression();
        Assert.AreEqual(
            new RsPath(
                new RsPath(
                    new RsName("foo"),
                    new RsName("bar")
                ),
                new RsName("baz")),
            expr);
    }

    [Test]
    public void TestPrimary_ComplexPath_ParsedCorrectly()
    {
        var parser = new Parser(new Lexer.Lexer("foo(1, 2)::bar<T> {a: 1, b: 2}[0]").Lex());
        var expr = parser.ParsePrimaryExpression();
        expr.Should().BeEquivalentTo(
            new RsIndex(
                new RsConstructor(
                    new RsWithGenerics(
                        new RsPath(new RsCall(
                            new RsName("foo"),
                            new RsExpression[]
                            {
                                new RsLiteralInt("1"),
                                new RsLiteralInt("2")
                            }), new RsName("bar")),
                        Array.Empty<RsLifetime>(),
                        new[]
                        {
                            new RsGeneric(
                                new RsName("T"),
                                Array.Empty<RsExpression>()
                            )
                        }
                    ),
                    new (RsName, RsExpression)[]
                    {
                        (new RsName("a"), new RsLiteralInt("1")),
                        (new RsName("b"), new RsLiteralInt("2"))
                    }
                ),
                new RsLiteralInt("0")
            )
        );
    }
}
