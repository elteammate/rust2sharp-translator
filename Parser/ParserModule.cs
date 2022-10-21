using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using Rust2SharpTranslator.Lexer;
using Rust2SharpTranslator.Utils;

namespace Rust2SharpTranslator.Parser;

/// <summary>
///     Parses everything that might be on top level
/// </summary>
public partial class Parser
{
    public RsModule ParseTopLevel()
    {
        var result = new List<RsNode>();

        while (_stream.HasNext())
            result.Add(ParseTopLevelObject());

        return new RsModule(null, result.ToArray());
    }

    public RsNode ParseTopLevelObject()
    {
        var token = _stream.Peek();

        return token switch
        {
            Keyword { Value: KeywordType.Mod } => ParseModule(),
            Keyword { Value: KeywordType.Fn } => ParseFunction(),
            Keyword { Value: KeywordType.Struct } => ParseStruct(),
            Keyword { Value: KeywordType.Trait } => ParseTrait(),
            Keyword { Value: KeywordType.Impl } => ParseImpl(),
            Keyword { Value: KeywordType.Enum } => ParseEnum(),
            Keyword { Value: KeywordType.Type } => ParseTypeDecl(),

            Comment { Type: CommentType.DocLine } comment =>
                _stream.Skip().And(() => new RsDocumented(
                    new RsLineDocComment(comment.Value),
                    ParseTopLevelObject())),

            Comment { Type: CommentType.DocBlock } comment =>
                _stream.Skip().And(() => new RsDocumented(
                    new RsBlockDocComment(comment.Value),
                    ParseTopLevelObject())),

            Keyword { Value: KeywordType.Pub } => _stream.Skip()
                .And(() => new RsPub(ParseTopLevelObject())),
            _ => throw new UnexpectedTokenException(token)
        };
    }

    public RsModule ParseModule()
    {
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.Mod));

        var name = ParseName();
        var result = new List<RsNode>();

        Debug.Assert(_stream.Next() is Punctuation { Value: PunctuationType.OpenBrace });

        while (!_stream.IfMatchConsume(new Punctuation(PunctuationType.CloseBrace)))
            result.Add(ParseTopLevelObject());

        return new RsModule(name, result.ToArray());
    }

    public RsFunction ParseFunction()
    {
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.Fn));

        var name = ParseName();
        var lifetimesAndGenerics = ParseLifetimesAndGenerics();

        var parameters = new List<RsParameter>();

        Debug.Assert(_stream.Next() is Punctuation { Value: PunctuationType.OpenParen });

        while (!_stream.IfMatchConsume(new Punctuation(PunctuationType.CloseParen)))
        {
            var paramName = ParseExpression();
            if (_stream.Peek() is Punctuation { Value: PunctuationType.Colon })
            {
                _stream.Next();
                Debug.Assert(paramName is RsName);
                var type = ParseExpression();
                parameters.Add(new RsParameter((paramName as RsName).Unwrap(), type));
            }
            else
                parameters.Add(new RsSelfParameter(paramName));

            _stream.IfMatchConsume(new Punctuation(PunctuationType.Comma));
        }

        RsExpression returnType = new RsLiteralUnit();
        if (_stream.IfMatchConsume(new Punctuation(PunctuationType.RArrow)))
            returnType = ParseExpression();

        var body = _stream.IfMatchConsume(new Punctuation(PunctuationType.Semi))
            ? null
            : ParseBlock();

        return new RsFunction(
            name,
            lifetimesAndGenerics.Item1,
            lifetimesAndGenerics.Item2,
            parameters.ToArray(),
            returnType,
            body
        );
    }

    private RsStructField[] ParseStructFields()
    {
        var fields = new List<RsStructField>();

        Debug.Assert(_stream.Next() is Punctuation { Value: PunctuationType.OpenBrace });

        while (!_stream.IfMatchConsume(new Punctuation(PunctuationType.CloseBrace)))
        {
            var fieldName = ParseName();
            Debug.Assert(_stream.Next() is Punctuation { Value: PunctuationType.Colon });
            var fieldType = ParseExpression();
            fields.Add(new RsStructField(fieldName, fieldType));
            _stream.IfMatchConsume(new Punctuation(PunctuationType.Comma));
        }

        return fields.ToArray();
    }

    public RsStruct ParseStruct()
    {
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.Struct));
        var name = ParseName();
        var lifetimesAndGenerics = ParseLifetimesAndGenerics();

        return new RsStruct(
            name,
            lifetimesAndGenerics.Item1,
            lifetimesAndGenerics.Item2,
            ParseStructFields()
        );
    }

    public RsEnum ParseEnum()
    {
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.Enum));
        var name = ParseName();
        var lifetimesAndGenerics = ParseLifetimesAndGenerics();

        var variants = new List<RsStruct>();

        Debug.Assert(_stream.Next() is Punctuation { Value: PunctuationType.OpenBrace });

        while (!_stream.IfMatchConsume(new Punctuation(PunctuationType.CloseBrace)))
        {
            var variantName = ParseName();
            if (_stream.Peek() == new Punctuation(PunctuationType.OpenBrace))
                variants.Add(new RsStruct(
                    variantName,
                    Array.Empty<RsLifetime>(),
                    Array.Empty<RsGeneric>(),
                    ParseStructFields()
                ));
            else
                variants.Add(new RsStruct(
                    variantName,
                    Array.Empty<RsLifetime>(),
                    Array.Empty<RsGeneric>(),
                    Array.Empty<RsStructField>()
                ));

            _stream.IfMatchConsume(new Punctuation(PunctuationType.Comma));
        }

        return new RsEnum(name, lifetimesAndGenerics.Item1, lifetimesAndGenerics.Item2,
            variants.ToArray());
    }

    public RsTrait ParseTrait()
    {
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.Trait));
        var name = ParseName();
        var lifetimesAndGenerics = ParseLifetimesAndGenerics();

        var functions = new List<RsFunction>();

        Debug.Assert(_stream.Next() is Punctuation { Value: PunctuationType.OpenBrace });

        while (!_stream.IfMatchConsume(new Punctuation(PunctuationType.CloseBrace)))
        {
            var fn = ParseFunction();
            functions.Add(fn);
            _stream.IfMatchConsume(new Punctuation(PunctuationType.Comma));
        }

        return new RsTrait(name, lifetimesAndGenerics.Item1, lifetimesAndGenerics.Item2,
            functions.ToArray());
    }

    public RsImpl ParseImpl()
    {
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.Impl));
        var lifetimesAndGenerics = ParseLifetimesAndGenerics();
        var what = ParsePrimaryExpression();
        RsExpression? forWhat = null;

        if (_stream.IfMatchConsume(new Keyword(KeywordType.For)))
            forWhat = ParsePrimaryExpression();

        Debug.Assert(_stream.Next() == new Punctuation(PunctuationType.OpenBrace));

        var functions = new List<RsFunction>();
        while (!_stream.IfMatchConsume(new Punctuation(PunctuationType.CloseBrace)))
            functions.Add(ParseFunction());

        var type = forWhat ?? what;
        var trait = forWhat == null ? null : what;
        return new RsImpl(type, trait, functions.ToArray());
    }

    private RsTypeDecl ParseTypeDecl()
    {
        Debug.Assert(_stream.Next() == new Keyword(KeywordType.Type));
        var name = ParseName();
        var lifetimesAndGenerics = ParseLifetimesAndGenerics();
        Debug.Assert(_stream.Next() is Punctuation { Value: PunctuationType.Eq });
        var type = ParseExpression();
        Debug.Assert(_stream.Next() is Punctuation { Value: PunctuationType.Semi });
        return new RsTypeDecl(name, lifetimesAndGenerics.Item1, lifetimesAndGenerics.Item2, type);
    }
}

public class __TestTopLevelParser__
{
    [Test]
    public void TopLevelParser_TestModParsing()
    {
        new Parser(new Lexer.Lexer("mod a { mod b {} }").Lex()).ParseTopLevel()
            .Should().BeEquivalentTo(
                new RsModule(null, new RsNode[]
                {
                    new RsModule(new RsName("a"), new RsNode[]
                    {
                        new RsModule(new RsName("b"), Array.Empty<RsNode>())
                    })
                }));
    }

    [Test]
    public void TopLevelParser_TestFunctionParsing()
    {
        new Parser(new Lexer.Lexer("fn foo<'a, T>(x: &'a str, b: T) -> &T { x }").Lex())
            .ParseTopLevel()
            .Should().BeEquivalentTo(
                new RsModule(null, new RsNode[]
                {
                    new RsFunction(
                        new RsName("foo"),
                        new[]
                        {
                            new RsLifetime(new RsLabel("a"))
                        },
                        new[]
                        {
                            new RsGeneric(new RsName("T"), Array.Empty<RsExpression>())
                        },
                        new[]
                        {
                            new RsParameter(new RsName("x"), new RsRef(
                                new RsLabel("a"),
                                false,
                                new RsName("str")
                            )),
                            new RsParameter(new RsName("b"), new RsName("T"))
                        },
                        new RsRef(null, false, new RsName("T")),
                        new RsBlock(Array.Empty<RsStatement>(), new RsName("x"))
                    )
                }));
    }

    [Test]
    public void TopLevelParser_TestStructParsing()
    {
        new Parser(new Lexer.Lexer("struct Foo<'a, Bar> { x: Bar, y: i32, f: &'a Baz }").Lex())
            .ParseStruct()
            .Should().BeEquivalentTo(
                new RsStruct(new RsName("Foo"), new[]
                {
                    new RsLifetime(new RsLabel("a"))
                }, new[]
                {
                    new RsGeneric(new RsName("Bar"), Array.Empty<RsExpression>())
                }, new[]
                {
                    new RsStructField(new RsName("x"), new RsName("Bar")),
                    new RsStructField(new RsName("y"), new RsName("i32")),
                    new RsStructField(new RsName("f"),
                        new RsRef(new RsLabel("a"), false, new RsName("Baz")))
                }));
    }

    [Test]
    public void TopLevelParser_TestEnumParsing()
    {
        new Parser(new Lexer.Lexer("enum Option<T> {Some {value: T}, None}").Lex()).ParseEnum()
            .Should().BeEquivalentTo(
                new RsEnum(new RsName("Option"), Array.Empty<RsLifetime>(), new[]
                {
                    new RsGeneric(new RsName("T"), Array.Empty<RsExpression>())
                }, new[]
                {
                    new RsStruct(new RsName("Some"), Array.Empty<RsLifetime>(),
                        Array.Empty<RsGeneric>(), new[]
                        {
                            new RsStructField(new RsName("value"), new RsName("T"))
                        }),
                    new RsStruct(new RsName("None"), Array.Empty<RsLifetime>(),
                        Array.Empty<RsGeneric>(), Array.Empty<RsStructField>())
                }));
    }

    [Test]
    public void TopLevelParser_TestTraitParsing()
    {
        new Parser(new Lexer.Lexer("trait Foo<'a, T> { fn bar(&self, x: T) -> T; }").Lex())
            .ParseTrait()
            .Should().BeEquivalentTo(
                new RsTrait(new RsName("Foo"), new[]
                {
                    new RsLifetime(new RsLabel("a"))
                }, new[]
                {
                    new RsGeneric(new RsName("T"), Array.Empty<RsExpression>())
                }, new[]
                {
                    new RsFunction(new RsName("bar"), Array.Empty<RsLifetime>(),
                        Array.Empty<RsGeneric>(), new[]
                        {
                            new RsParameter(new RsName("self"),
                                new RsRef(null, true, new RsName("Self"))),
                            new RsParameter(new RsName("x"), new RsName("T"))
                        }, new RsName("T"), null)
                }));
    }

    [Test]
    public void TopLevelParser_TestImplParsing()
    {
        new Parser(new Lexer.Lexer("impl<'a, T> Foo<'a, T> { fn bar(&self, x: T) -> T {x} }").Lex())
            .ParseImpl()
            .Should().BeEquivalentTo(
                new RsImpl(new RsName("Foo"), null, new[]
                {
                    new RsFunction(new RsName("bar"), Array.Empty<RsLifetime>(),
                        Array.Empty<RsGeneric>(), new[]
                        {
                            new RsParameter(new RsName("self"),
                                new RsRef(null, true, new RsName("Self"))),
                            new RsParameter(new RsName("x"), new RsName("T"))
                        }, new RsName("T"),
                        new RsBlock(Array.Empty<RsStatement>(), new RsName("x")))
                }));
    }
}
