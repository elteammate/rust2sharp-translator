using NUnit.Framework;
using Rust2SharpTranslator.Lexer;
using Rust2SharpTranslator.Utils;

namespace Rust2SharpTranslator.Parser;

public partial class Parser
{
    // https://doc.rust-lang.org/reference/expressions.html
    public enum BinaryPrecedence
    {
        Lowest = 0,
        Assign, // =, +=, -=, ...
        Range, //.., ..=
        Or, // ||,
        And, // &&
        Compare, // ==, !=, <, >, <=, >=
        BitOr, // |
        BitXor, // ^
        BitAnd, // &
        Shift, // <<, >>
        Add, // +, -
        Mul, // *, /, %
        As, // as
    }

    public enum Operator
    {
        Assign,
        PlusAssign,
        MinusAssign,
        MulAssign,
        DivAssign,
        ModAssign,
        BitAndAssign,
        BitOrAssign,
        BitXorAssign,
        BitShlAssign,
        BitShrAssign,
        Range,
        RangeInclusive,
        Or,
        And,
        BitOr,
        BitXor,
        BitAnd,
        BitShl,
        BitShr,
        Equal,
        NotEqual,
        Less,
        Greater,
        LessEqual,
        GreaterEqual,
        Plus,
        Minus,
        Mul,
        Div,
        Mod,
        As
    }

    public enum Associativity
    {
        Left,
        Right
    }

    private static Operator GetOperator(Token token)
    {
        return token switch
        {
            Keyword { Value: KeywordType.As } => Operator.As,
            Punctuation punctuation => punctuation.Value switch
            {
                PunctuationType.Eq => Operator.Assign,
                PunctuationType.PlusEq => Operator.PlusAssign,
                PunctuationType.MinusEq => Operator.MinusAssign,
                PunctuationType.StarEq => Operator.MulAssign,
                PunctuationType.SlashEq => Operator.DivAssign,
                PunctuationType.PercentEq => Operator.ModAssign,
                PunctuationType.AndEq => Operator.BitAndAssign,
                PunctuationType.OrEq => Operator.BitOrAssign,
                PunctuationType.CaretEq => Operator.BitXorAssign,
                PunctuationType.ShlEq => Operator.BitShlAssign,
                PunctuationType.ShrEq => Operator.BitShrAssign,
                PunctuationType.DotDot => Operator.Range,
                PunctuationType.DotDotEq => Operator.RangeInclusive,
                PunctuationType.OrOr => Operator.Or,
                PunctuationType.AndAnd => Operator.And,
                PunctuationType.Or => Operator.BitOr,
                PunctuationType.Caret => Operator.BitXor,
                PunctuationType.And => Operator.BitAnd,
                PunctuationType.Shl => Operator.BitShl,
                PunctuationType.Shr => Operator.BitShr,
                PunctuationType.EqEq => Operator.Equal,
                PunctuationType.Ne => Operator.NotEqual,
                PunctuationType.Lt => Operator.Less,
                PunctuationType.Gt => Operator.Greater,
                PunctuationType.Le => Operator.LessEqual,
                PunctuationType.Ge => Operator.GreaterEqual,
                PunctuationType.Plus => Operator.Plus,
                PunctuationType.Minus => Operator.Minus,
                PunctuationType.Star => Operator.Mul,
                PunctuationType.Slash => Operator.Div,
                PunctuationType.Percent => Operator.Mod,
                _ => throw new UnexpectedTokenException(token)
            },
            _ => throw new UnexpectedTokenException(token)
        };
    }

    private static BinaryPrecedence GetPrecedence(Operator op)
    {
        return op switch
        {
            Operator.Assign => BinaryPrecedence.Assign,
            Operator.PlusAssign => BinaryPrecedence.Assign,
            Operator.MinusAssign => BinaryPrecedence.Assign,
            Operator.MulAssign => BinaryPrecedence.Assign,
            Operator.DivAssign => BinaryPrecedence.Assign,
            Operator.ModAssign => BinaryPrecedence.Assign,
            Operator.BitAndAssign => BinaryPrecedence.Assign,
            Operator.BitOrAssign => BinaryPrecedence.Assign,
            Operator.BitXorAssign => BinaryPrecedence.Assign,
            Operator.BitShlAssign => BinaryPrecedence.Assign,
            Operator.BitShrAssign => BinaryPrecedence.Assign,
            Operator.Range => BinaryPrecedence.Range,
            Operator.RangeInclusive => BinaryPrecedence.Range,
            Operator.Or => BinaryPrecedence.Or,
            Operator.And => BinaryPrecedence.And,
            Operator.BitOr => BinaryPrecedence.BitOr,
            Operator.BitXor => BinaryPrecedence.BitXor,
            Operator.BitAnd => BinaryPrecedence.BitAnd,
            Operator.BitShl => BinaryPrecedence.Shift,
            Operator.BitShr => BinaryPrecedence.Shift,
            Operator.Equal => BinaryPrecedence.Compare,
            Operator.NotEqual => BinaryPrecedence.Compare,
            Operator.Less => BinaryPrecedence.Compare,
            Operator.Greater => BinaryPrecedence.Compare,
            Operator.LessEqual => BinaryPrecedence.Compare,
            Operator.GreaterEqual => BinaryPrecedence.Compare,
            Operator.Plus => BinaryPrecedence.Add,
            Operator.Minus => BinaryPrecedence.Add,
            Operator.Mul => BinaryPrecedence.Mul,
            Operator.Div => BinaryPrecedence.Mul,
            Operator.Mod => BinaryPrecedence.Mul,
            Operator.As => BinaryPrecedence.As,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }

    private Associativity GetAssociativity(Operator op)
    {
        return op switch
        {
            Operator.Assign => Associativity.Right,
            Operator.PlusAssign => Associativity.Right,
            Operator.MinusAssign => Associativity.Right,
            Operator.MulAssign => Associativity.Right,
            Operator.DivAssign => Associativity.Right,
            Operator.ModAssign => Associativity.Right,
            Operator.BitAndAssign => Associativity.Right,
            Operator.BitOrAssign => Associativity.Right,
            Operator.BitXorAssign => Associativity.Right,
            Operator.BitShlAssign => Associativity.Right,
            Operator.BitShrAssign => Associativity.Right,
            _ => Associativity.Left
        };
    }

    public RsExpression ParseExpression()
    {
        return ParseExpressionAfter(ParsePrimaryExpression(), BinaryPrecedence.Lowest);
    }

    private RsExpression ParseExpressionAfter(RsExpression expr, BinaryPrecedence precedence)
    {
        while (true)
        {
            if (IsTerminator(_stream.Peek()))
                return expr;

            var op = GetOperator(_stream.Peek().Unwrap());

            if (GetPrecedence(op) < precedence)
                break;

            _stream.Skip();

            var right = ParsePrimaryExpression();

            while (true)
            {
                if (IsTerminator(_stream.Peek()))
                    break;

                var nextOp = GetOperator(_stream.Peek().Unwrap());

                if (!(GetPrecedence(nextOp) > GetPrecedence(op) ||
                      (GetAssociativity(nextOp) == Associativity.Right &&
                       GetPrecedence(nextOp) >= GetPrecedence(op))))
                    break;

                right = ParseExpressionAfter(right, GetPrecedence(nextOp));
            }

            expr = op switch
            {
                Operator.Assign => new RsAssign(expr, right),
                Operator.PlusAssign => new RsAssignAdd(expr, right),
                Operator.MinusAssign => new RsAssignSub(expr, right),
                Operator.MulAssign => new RsAssignMul(expr, right),
                Operator.DivAssign => new RsAssignDiv(expr, right),
                Operator.ModAssign => new RsAssignRem(expr, right),
                Operator.BitAndAssign => new RsAssignBitAnd(expr, right),
                Operator.BitOrAssign => new RsAssignBitOr(expr, right),
                Operator.BitXorAssign => new RsAssignBitXor(expr, right),
                Operator.BitShlAssign => new RsAssignShl(expr, right),
                Operator.BitShrAssign => new RsAssignShr(expr, right),
                Operator.Range => new RsRange(expr, right),
                Operator.RangeInclusive => new RsRangeInclusive(expr, right),
                Operator.Or => new RsOr(expr, right),
                Operator.And => new RsAnd(expr, right),
                Operator.BitOr => new RsBitOr(expr, right),
                Operator.BitXor => new RsBitXor(expr, right),
                Operator.BitAnd => new RsBitAnd(expr, right),
                Operator.BitShl => new RsShl(expr, right),
                Operator.BitShr => new RsShr(expr, right),
                Operator.Equal => new RsEq(expr, right),
                Operator.NotEqual => new RsNe(expr, right),
                Operator.Less => new RsLt(expr, right),
                Operator.Greater => new RsGt(expr, right),
                Operator.LessEqual => new RsLe(expr, right),
                Operator.GreaterEqual => new RsGe(expr, right),
                Operator.Plus => new RsAdd(expr, right),
                Operator.Minus => new RsSub(expr, right),
                Operator.Mul => new RsMul(expr, right),
                Operator.Div => new RsDiv(expr, right),
                Operator.Mod => new RsRem(expr, right),
                Operator.As => new RsAs(expr, right),
                _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
            };
        }

        return expr;
    }

    private static bool IsTerminator(Token? token)
    {
        return token is null or Punctuation
        {
            Value: PunctuationType.Semi or
            PunctuationType.Comma or
            PunctuationType.CloseParen or
            PunctuationType.CloseBracket or
            PunctuationType.CloseBrace
        };
    }
}

internal class __ParserExpressionTests__
{
    [Test]
    public void Parser_Expressions_TestSimplePrecedence()
    {
        var parser = new Parser(new Lexer.Lexer("1 + 2 * 3;").Lex());
        var expr = parser.ParseExpression();
        Assert.AreEqual(expr, new RsAdd(
            new RsLiteralInt("1"),
            new RsMul(
                new RsLiteralInt("2"),
                new RsLiteralInt("3")
            )
        ));
    }
    
    [Test]
    public void Parser_Expressions_TestRightAssociativity()
    {
        var parser = new Parser(new Lexer.Lexer("a = b = c").Lex());
        var expr = parser.ParseExpression();
        Assert.AreEqual(expr, new RsAssign(
            new RsName("a"),
            new RsAssign(
                new RsName("b"),
                new RsName("c")
            )
        ));
    }
    
    [Test]
    public void Parser_Expressions_TestLeftAssociativity()
    {
        var parser = new Parser(new Lexer.Lexer("a + b + c").Lex());
        var expr = parser.ParseExpression();
        Assert.AreEqual(expr, new RsAdd(
            new RsAdd(
                new RsName("a"),
                new RsName("b")
            ),
            new RsName("c")
        ));
    }
}
