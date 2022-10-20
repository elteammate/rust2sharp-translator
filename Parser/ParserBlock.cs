using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
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

    public RsIf ParseIf()
    {
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.If));
        
        var condition = ParseExpression();
        var thenClause = ParseBlock();
        

        if (!_stream.IfMatchConsume(new Keyword(KeywordType.Else)))
            return new RsIf(condition, thenClause, null);
        
        RsExpression? elseClause;
        
        if (_stream.Peek() == new Keyword(KeywordType.If))
            elseClause = ParseIf();
        else
            elseClause = ParseBlock();
        
        return new RsIf(condition, thenClause, elseClause);
    }

    public RsLoop ParseLoop()
    {
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.Loop));
        return new RsLoop(ParseBlock());
    }

    public RsWhile ParseWhile()
    {
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.While));
        
        var condition = ParseExpression();
        var body = ParseBlock();
        
        return new RsWhile(condition, body);
    }

    public RsFor ParseFor()
    {
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.For));
        
        var name = ParseName();
        
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.In));
        
        var iterable = ParseExpression();
        var body = ParseBlock();
        
        return new RsFor(name, iterable, body);
    }

    public RsMatch ParseMatch()
    {
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.Match));
        
        var expression = ParseExpression();
        Debug.Assert(_stream.Next() == new Punctuation(PunctuationType.OpenBrace));
        
        var arms = new List<RsMatchArm>();
        
        while (_stream.Peek() != new Punctuation(PunctuationType.CloseBrace))
        {
            var pattern = ParseExpression();
            Debug.Assert(_stream.Next() == new Punctuation(PunctuationType.FatArrow));
            if (_stream.Peek() == new Punctuation(PunctuationType.OpenBrace))
                arms.Add(new RsMatchArm(pattern, ParseBlock()));
            else
                arms.Add(new RsMatchArm(
                    pattern, 
                    new RsBlock(new RsStatement[] { new RsReturn(ParseExpression()) }, null)
                    ));

            _stream.IfMatchConsume(new Punctuation(PunctuationType.Comma));
        }
        
        Debug.Assert(_stream.Next() == new Punctuation(PunctuationType.CloseBrace));
        
        return new RsMatch(expression, arms.ToArray());
    }
    
    
}


public class __ParserBlockTests__
{
    [Test]
    public void ParserBlock_TestLetParsing()
    {
        new Parser(new Lexer.Lexer("let mut foo: &Bar<Bar> = 1 + 1;").Lex())
            .ParseLet().Should().BeEquivalentTo(
                    new RsLet(
                        new RsName("foo"),
                        true,
                        new RsRef(
                            false, 
                            new RsWithGenerics(
                                new RsName("Bar"),
                                Array.Empty<RsLifetime>(),
                                new[] {new RsGeneric(new RsName("Bar"), Array.Empty<RsExpression>()) }
                            )
                        ),
                        new RsAdd(
                            new RsLiteralInt("1"),
                            new RsLiteralInt("1")
                        )
                    )
                );
    }

    [Test]
    public void ParserBlock_TestIfParsing()
    {
        new Parser(new Lexer.Lexer("if x == 0 {0} else if x == 1 {1} else {2}").Lex())
            .ParseIf().Should().BeEquivalentTo(
                new RsIf(
                    new RsEq(new RsName("x"), new RsLiteralInt("0")),
                    new RsBlock(Array.Empty<RsStatement>(), new RsReturn(new RsLiteralInt("0"))),
                    new RsIf(
                        new RsEq(new RsName("x"), new RsLiteralInt("1")),
                        new RsBlock(Array.Empty<RsStatement>(), new RsReturn(new RsLiteralInt("1"))),
                        new RsBlock(Array.Empty<RsStatement>(), new RsReturn(new RsLiteralInt("2")))
                    )
                ));
    }

    [Test]
    public void ParserBlock_TestIfParsing2()
    {
        new Parser(new Lexer.Lexer("if x == 0 {0}").Lex())
            .ParseIf().Should().BeEquivalentTo(
                new RsIf(
                    new RsEq(new RsName("x"), new RsLiteralInt("0")),
                    new RsBlock(Array.Empty<RsStatement>(), new RsReturn(new RsLiteralInt("0"))),
                    null
                ));
    }

    [Test]
    public void ParserBlock_TestLoopParsing()
    {
        new Parser(new Lexer.Lexer("x = loop { break 0; }").Lex())
            .ParseExpression().Should().BeEquivalentTo(
                new RsAssign(
                    new RsName("x"),
                    new RsLoop(
                        new RsBlock(
                            new RsStatement[] {new RsBreak(new RsLiteralInt("0"))},
                            null
                        )
                    )
                )
                );
    }

    [Test]
    public void ParserBlock_TestLoopParsing2()
    {
        new Parser(new Lexer.Lexer("loop { }").Lex())
            .ParseExpression().Should().BeEquivalentTo(
                new RsLoop(
                    new RsBlock(
                        Array.Empty<RsStatement>(),
                        null
                    )
                )
            );
    }

    [Test]
    public void TestWhileParsing()
    {
        new Parser(new Lexer.Lexer("while x < 10 { x += 1; }").Lex())
            .ParseWhile().Should().BeEquivalentTo(
                new RsWhile(
                    new RsLt(new RsName("x"), new RsLiteralInt("10")),
                    new RsBlock(
                        new RsStatement[] {new RsAssign(new RsName("x"), new RsAdd(new RsName("x"), new RsLiteralInt("1")))},
                        null
                    )
                )
            );
    }

    [Test]
    public void ParserBlock_TestForParsing()
    {
        new Parser(new Lexer.Lexer("for x in 0..10 { x += 1; }").Lex())
            .ParseFor().Should().BeEquivalentTo(
                new RsFor(
                    new RsName("x"),
                    new RsRange(
                        new RsLiteralInt("0"),
                        new RsLiteralInt("10")
                    ),
                    new RsBlock(
                        new RsStatement[] {new RsAssign(new RsName("x"), new RsAdd(new RsName("x"), new RsLiteralInt("1")))},
                        null
                    )
                )
            );
    }

    [Test]
    public void ParserBlock_TestMatchParsing()
    {
        new Parser(new Lexer.Lexer("match x { 0 => 0, 1 => 1, _ => 2 }").Lex())
            .ParseExpression().Should().BeEquivalentTo(
                new RsMatch(
                    new RsName("x"),
                    new[]
                    {
                        new RsMatchArm(
                            new RsLiteralInt("0"),
                            new RsBlock(Array.Empty<RsStatement>(), new RsReturn(new RsLiteralInt("0")))
                        ),
                        new RsMatchArm(
                            new RsLiteralInt("1"),
                            new RsBlock(Array.Empty<RsStatement>(), new RsReturn(new RsLiteralInt("1")))
                        ),
                        new RsMatchArm(
                            new RsUnderscore(),
                            new RsBlock(Array.Empty<RsStatement>(), new RsReturn(new RsLiteralInt("2")))
                        )
                    }
                )
            );
    }

    [Test]
    public void ParserBlock_TestBlockParsing()
    {
        new Parser(new Lexer.Lexer("{ let mut foo: &Bar<Bar> = 1 + 1; }").Lex())
            .ParseBlock().Should().BeEquivalentTo(
                new RsBlock(
                    new RsStatement[]
                    {
                        new RsLet(
                            new RsName("foo"),
                            true,
                            new RsRef(
                                false, 
                                new RsWithGenerics(
                                    new RsName("Bar"),
                                    Array.Empty<RsLifetime>(),
                                    new[] {new RsGeneric(new RsName("Bar"), Array.Empty<RsExpression>()) }
                                )
                            ),
                            new RsAdd(
                                new RsLiteralInt("1"),
                                new RsLiteralInt("1")
                            )
                        )
                    },
                    null
                )
            );
    }
}
