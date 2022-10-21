using System.Globalization;
using System.Text;
using NUnit.Framework;
using Rust2SharpTranslator.Utils;

namespace Rust2SharpTranslator.Lexer;

/// <summary>
///     Tokenizer for Rust source code.
/// </summary>
public class Lexer
{
    private readonly Stream<char> _stream;

    public Lexer(string input)
    {
        _stream = new Stream<char>(input);
    }

    private char ReadEscapedChar()
    {
        var c = _stream.Next();
        if (c != '\\') return c;
        c = _stream.Next();
        return c switch
        {
            'r' => '\r',
            't' => '\t',
            'n' => '\n',
            '0' => '\0',
            '\\' => '\\',
            '\'' => '\'',
            '"' => '"',
            'x' => (char)int.Parse(string.Join("", _stream.Take(2)), NumberStyles.HexNumber),
            _ => throw new NoSuchEscapeInStringLiteralException(c)
        };
    }

    private Token? ReadNext()
    {
        if (Consumers.IsSpace(_stream.Peek()))
            _stream.SkipWhile(Consumers.IsSpace);

        if (!_stream.HasNext())
            return null;

        if (_stream.IfMatchConsume('\''))
        {
            var c = ReadEscapedChar();
            if (_stream.IfMatchConsume('\''))
                return new Literal(LiteralType.Char, c.ToString());
            if (Consumers.IsIdentifierStart(c))
            {
                var label = c + string.Join("", _stream.TakeWhile(Consumers.IsIdentifierPart));
                return new Literal(LiteralType.Label, label);
            }
        }

        if (_stream.IfMatchConsume('"'))
        {
            var builder = new StringBuilder();
            while (!_stream.IfMatchConsume('"'))
            {
                builder.Append(ReadEscapedChar());
                if (!_stream.HasNext())
                    throw new UnterminatedStringLiteralException();
            }

            return new Literal(LiteralType.String, builder.ToString());
        }

        if (_stream.Peek() == 'b' && _stream.Peek(1) == '\'')
        {
            _stream.Skip(2);
            var c = ReadEscapedChar();
            if (_stream.IfMatchConsume('\''))
                return new Literal(LiteralType.Byte, ((byte)c).ToString());

            throw new UnterminatedStringLiteralException();
        }

        if (_stream.Peek() == 'b' && _stream.Peek(1) == '\"')
        {
            _stream.Skip(2);
            var builder = new StringBuilder();
            while (!_stream.IfMatchConsume('"'))
            {
                builder.Append(ReadEscapedChar());
                if (!_stream.HasNext())
                    throw new UnterminatedStringLiteralException();
            }

            return new Literal(LiteralType.ByteString, builder.ToString());
        }

        if (Consumers.IsIdentifierStart(_stream.Peek()))
        {
            var identifier = string.Join("", _stream.TakeWhile(Consumers.IsIdentifierPart));
            var keyword = Consumers.TryGetKeyword(identifier);
            return keyword == null
                ? new Identifier(identifier)
                : new Keyword(keyword.Value);
        }

        if (Consumers.IsDigit(_stream.Peek()))
        {
            var number = string.Join("", _stream.TakeWhile(Consumers.IsDigit));
            if (_stream.Peek() == '.' && Consumers.IsDigit(_stream.Peek(1)))
            {
                _stream.Next();
                var fraction = string.Join("", _stream.TakeWhile(Consumers.IsDigit));
                return new Literal(LiteralType.Float, number + "." + fraction);
            }

            return new Literal(LiteralType.Integer, number);
        }

        if (_stream.Peek() == '/')
        {
            if (_stream.Peek(1) == '/')
            {
                if (_stream.Peek(2) == '/' && _stream.Peek(3) != '/')
                {
                    _stream.Skip(3);
                    return new Comment(CommentType.DocLine,
                        string.Join("", _stream.TakeWhile(c => c != '\n')));
                }

                _stream.Skip(2);
                return new Comment(CommentType.Line,
                    string.Join("", _stream.TakeWhile(c => c != '\n')));
            }

            if (_stream.Peek(1) == '*')
            {
                if (_stream.Peek(2) == '*')
                {
                    _stream.Skip(3);
                    var content = string.Join("", _stream.TakeUntil("*/".ToCharArray()));
                    _stream.Skip(2);
                    return new Comment(CommentType.DocBlock, content);
                }
                else
                {
                    _stream.Skip(2);
                    var content = string.Join("", _stream.TakeUntil("*/".ToCharArray()));
                    _stream.Skip(2);
                    return new Comment(CommentType.Block, content);
                }
            }
        }

        if (Consumers.IsPunctuation(_stream.Peek()))
        {
            var punctuation = Consumers.IsStackablePunctuation(_stream.Peek())
                ? string.Join("", _stream.TakeWhile(Consumers.IsStackablePunctuation))
                : _stream.Next().ToString();

            var op = Consumers.TryGetPunctuation(punctuation);
            if (op == null)
                throw new UndefinedPunctuationException(punctuation);

            return new Punctuation(op.Value);
        }

        throw new UnexpectedCharacterException(_stream.Next());
    }

    public List<Token> Lex()
    {
        var tokens = new List<Token>();
        if (tokens == null) throw new ArgumentNullException(nameof(tokens));
        while (_stream.HasNext())
        {
            var token = ReadNext();
            if (token != null)
                tokens.Add(token);
        }

        return tokens;
    }
}

public static class __LexerTests__
{
    [Test]
    public static void Lexer_TestOnRealProgram_ReturnsCorrectList()
    {
        // language=Rust
        const string program = @"
        use std::io::stdin;
        unsafe trait Foo { fn bar(&self) -> Self; }
        
        /** block doc comment */
        fn test<'a, T: Sized + Foo>(x: &'a T) -> &'a T {
            x.bar()
        }
        
        ///////// just comment
        /// doc comment
        struct Bar<T> {x: T, y: i32}
        enum Test {
            A(Bar<String>), B(Bar<i32>), C
        }
        
        fn main() {
            let x: Test = Test::B(Bar {x: 5, y: 6}); // comment
            let y = x + 10 + /* block comment */ b""test\xAB\n\0"".len();
            let mut z = 0 + """".len() * 3 << 1.02;
            println!(""{}"", z);
        }
        ";

        var tokens = new Stream<Token>(new Lexer(program).Lex());
        Assert.AreEqual(new Keyword(KeywordType.Use), tokens.Next());
        Assert.AreEqual(new Identifier("std"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.PathSep), tokens.Next());
        Assert.AreEqual(new Identifier("io"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.PathSep), tokens.Next());
        Assert.AreEqual(new Identifier("stdin"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Semi), tokens.Next());
        Assert.AreEqual(new Keyword(KeywordType.Unsafe), tokens.Next());
        Assert.AreEqual(new Keyword(KeywordType.Trait), tokens.Next());
        Assert.AreEqual(new Identifier("Foo"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenBrace), tokens.Next());
        Assert.AreEqual(new Keyword(KeywordType.Fn), tokens.Next());
        Assert.AreEqual(new Identifier("bar"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.And), tokens.Next());
        Assert.AreEqual(new Keyword(KeywordType.Self), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.RArrow), tokens.Next());
        Assert.AreEqual(new Keyword(KeywordType.SelfType), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Semi), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseBrace), tokens.Next());
        Assert.AreEqual(new Comment(CommentType.DocBlock, " block doc comment "), tokens.Next());
        Assert.AreEqual(new Keyword(KeywordType.Fn), tokens.Next());
        Assert.AreEqual(new Identifier("test"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Lt), tokens.Next());
        Assert.AreEqual(new Literal(LiteralType.Label, "a"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Comma), tokens.Next());
        Assert.AreEqual(new Identifier("T"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Colon), tokens.Next());
        Assert.AreEqual(new Identifier("Sized"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Plus), tokens.Next());
        Assert.AreEqual(new Identifier("Foo"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Gt), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenParen), tokens.Next());
        Assert.AreEqual(new Identifier("x"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Colon), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.And), tokens.Next());
        Assert.AreEqual(new Literal(LiteralType.Label, "a"), tokens.Next());
        Assert.AreEqual(new Identifier("T"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.RArrow), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.And), tokens.Next());
        Assert.AreEqual(new Literal(LiteralType.Label, "a"), tokens.Next());
        Assert.AreEqual(new Identifier("T"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenBrace), tokens.Next());
        Assert.AreEqual(new Identifier("x"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Dot), tokens.Next());
        Assert.AreEqual(new Identifier("bar"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseBrace), tokens.Next());
        Assert.AreEqual(new Comment(CommentType.Line, "/////// just comment"), tokens.Next());
        Assert.AreEqual(new Comment(CommentType.DocLine, " doc comment"), tokens.Next());
        Assert.AreEqual(new Keyword(KeywordType.Struct), tokens.Next());
        Assert.AreEqual(new Identifier("Bar"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Lt), tokens.Next());
        Assert.AreEqual(new Identifier("T"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Gt), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenBrace), tokens.Next());
        Assert.AreEqual(new Identifier("x"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Colon), tokens.Next());
        Assert.AreEqual(new Identifier("T"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Comma), tokens.Next());
        Assert.AreEqual(new Identifier("y"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Colon), tokens.Next());
        Assert.AreEqual(new Identifier("i32"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseBrace), tokens.Next());
        Assert.AreEqual(new Keyword(KeywordType.Enum), tokens.Next());
        Assert.AreEqual(new Identifier("Test"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenBrace), tokens.Next());
        Assert.AreEqual(new Identifier("A"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenParen), tokens.Next());
        Assert.AreEqual(new Identifier("Bar"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Lt), tokens.Next());
        Assert.AreEqual(new Identifier("String"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Gt), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Comma), tokens.Next());
        Assert.AreEqual(new Identifier("B"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenParen), tokens.Next());
        Assert.AreEqual(new Identifier("Bar"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Lt), tokens.Next());
        Assert.AreEqual(new Identifier("i32"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Gt), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Comma), tokens.Next());
        Assert.AreEqual(new Identifier("C"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseBrace), tokens.Next());
        Assert.AreEqual(new Keyword(KeywordType.Fn), tokens.Next());
        Assert.AreEqual(new Identifier("main"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenBrace), tokens.Next());
        Assert.AreEqual(new Keyword(KeywordType.Let), tokens.Next());
        Assert.AreEqual(new Identifier("x"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Colon), tokens.Next());
        Assert.AreEqual(new Identifier("Test"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Eq), tokens.Next());
        Assert.AreEqual(new Identifier("Test"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.PathSep), tokens.Next());
        Assert.AreEqual(new Identifier("B"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenParen), tokens.Next());
        Assert.AreEqual(new Identifier("Bar"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenBrace), tokens.Next());
        Assert.AreEqual(new Identifier("x"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Colon), tokens.Next());
        Assert.AreEqual(new Literal(LiteralType.Integer, "5"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Comma), tokens.Next());
        Assert.AreEqual(new Identifier("y"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Colon), tokens.Next());
        Assert.AreEqual(new Literal(LiteralType.Integer, "6"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseBrace), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Semi), tokens.Next());
        Assert.AreEqual(new Comment(CommentType.Line, " comment"), tokens.Next());
        Assert.AreEqual(new Keyword(KeywordType.Let), tokens.Next());
        Assert.AreEqual(new Identifier("y"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Eq), tokens.Next());
        Assert.AreEqual(new Identifier("x"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Plus), tokens.Next());
        Assert.AreEqual(new Literal(LiteralType.Integer, "10"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Plus), tokens.Next());
        Assert.AreEqual(new Comment(CommentType.Block, " block comment "), tokens.Next());
        Assert.AreEqual(new Literal(LiteralType.ByteString, "test\xAB\n\0"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Dot), tokens.Next());
        Assert.AreEqual(new Identifier("len"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Semi), tokens.Next());
        Assert.AreEqual(new Keyword(KeywordType.Let), tokens.Next());
        Assert.AreEqual(new Keyword(KeywordType.Mut), tokens.Next());
        Assert.AreEqual(new Identifier("z"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Eq), tokens.Next());
        Assert.AreEqual(new Literal(LiteralType.Integer, "0"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Plus), tokens.Next());
        Assert.AreEqual(new Literal(LiteralType.String, ""), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Dot), tokens.Next());
        Assert.AreEqual(new Identifier("len"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Star), tokens.Next());
        Assert.AreEqual(new Literal(LiteralType.Integer, "3"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Shl), tokens.Next());
        Assert.AreEqual(new Literal(LiteralType.Float, "1.02"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Semi), tokens.Next());
        Assert.AreEqual(new Identifier("println"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Not), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.OpenParen), tokens.Next());
        Assert.AreEqual(new Literal(LiteralType.String, "{}"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Comma), tokens.Next());
        Assert.AreEqual(new Identifier("z"), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseParen), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.Semi), tokens.Next());
        Assert.AreEqual(new Punctuation(PunctuationType.CloseBrace), tokens.Next());
        Assert.IsFalse(tokens.HasNext());
    }
}
