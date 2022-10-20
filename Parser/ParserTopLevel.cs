using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using Rust2SharpTranslator.Lexer;

namespace Rust2SharpTranslator.Parser;

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
            var paramName = ParseName();
            Debug.Assert(_stream.Next() is Punctuation { Value: PunctuationType.Colon });
            var type = ParseExpression();
            parameters.Add(new RsParameter(paramName, type));
            _stream.IfMatchConsume(new Punctuation(PunctuationType.Comma));
        }

        RsExpression returnType = new RsLiteralUnit();
        if (_stream.IfMatchConsume(new Punctuation(PunctuationType.RArrow)))
            returnType = ParseExpression();

        var body = ParseBlock();

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
            {
                variants.Add(new RsStruct(
                    variantName,
                    Array.Empty<RsLifetime>(),
                    Array.Empty<RsGeneric>(),
                    ParseStructFields()
                ));
            }
            else
            {
                variants.Add(new RsStruct(
                    variantName,
                    Array.Empty<RsLifetime>(),
                    Array.Empty<RsGeneric>(),
                    Array.Empty<RsStructField>()
                ));
            }
            
            _stream.IfMatchConsume(new Punctuation(PunctuationType.Comma));
        }
        
        return new RsEnum(name, lifetimesAndGenerics.Item1, lifetimesAndGenerics.Item2, variants.ToArray());
    }
}

public class __TestTopLevelParser__
{
    [Test]
    public void TopLevelParser_TestModParsing()
    {
        new Parser(new Lexer.Lexer("mod a { mod b {} }").Lex()).ParseTopLevel()
            .Should().BeEquivalentTo(
                new RsModule(null, new RsNode[] {
                    new RsModule(new RsName("a"), new RsNode[] {
                        new RsModule(new RsName("b"), new RsNode[] { })
                    })
                }));
    }

    [Test]
    public void TopLevelParser_TestFunctionParsing()
    {
        new Parser(new Lexer.Lexer("fn foo<'a, T>(x: &'a str, b: T) -> &T { x }").Lex()).ParseTopLevel()
            .Should().BeEquivalentTo(
                new RsModule(null, new RsNode[] {
                    new RsFunction(
                        new RsName("foo"),
                        new[] {
                            new RsLifetime(new RsLabel("a"))
                        },
                        new[] {
                            new RsGeneric(new RsName("T"), Array.Empty<RsExpression>())
                        },
                        new [] {
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
        new Parser(new Lexer.Lexer("struct Foo<'a, Bar> { x: Bar, y: i32, f: &'a Baz }").Lex()).ParseStruct()
            .Should().BeEquivalentTo(
                new RsStruct(new RsName("Foo"), new [] {
                    new RsLifetime(new RsLabel("a"))
                }, new [] {
                    new RsGeneric(new RsName("Bar"), Array.Empty<RsExpression>())
                }, new [] {
                    new RsStructField(new RsName("x"), new RsName("Bar")),
                    new RsStructField(new RsName("y"), new RsName("i32")),
                    new RsStructField(new RsName("f"), new RsRef(new RsLabel("a"), false, new RsName("Baz")))
                }));
    }

    [Test]
    public void TopLevelParser_TestEnumParsing()
    {
        new Parser(new Lexer.Lexer("enum Option<T> {Some {value: T}, None}").Lex()).ParseEnum()
            .Should().BeEquivalentTo(
                new RsEnum(new RsName("Option"), Array.Empty<RsLifetime>(), new [] {
                    new RsGeneric(new RsName("T"), Array.Empty<RsExpression>())
                }, new [] {
                    new RsStruct(new RsName("Some"), Array.Empty<RsLifetime>(), Array.Empty<RsGeneric>(), new [] {
                        new RsStructField(new RsName("value"), new RsName("T"))
                    }),
                    new RsStruct(new RsName("None"), Array.Empty<RsLifetime>(), Array.Empty<RsGeneric>(), Array.Empty<RsStructField>())
                }));
    }
}
