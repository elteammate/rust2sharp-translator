using Rust2SharpTranslator.Parser;
using Rust2SharpTranslator.Utils;

namespace Rust2SharpTranslator.Generator;

public partial class Generator
{
    private enum BinaryPrecedence
    {
        Assign = 0,
        Or,
        And,
        BitOr,
        BitXor,
        BitAnd,
        Equality,
        Comparison,
        Shift,
        Additive,
        Multiplicative,
        Range,
        Higher,
    }

    private static BinaryPrecedence GetPrecedence(RsExpression op)
    {
        return op switch
        {
            RsAssign => BinaryPrecedence.Assign,
            RsAssignAdd => BinaryPrecedence.Assign,
            RsAssignSub => BinaryPrecedence.Assign,
            RsAssignMul => BinaryPrecedence.Assign,
            RsAssignDiv => BinaryPrecedence.Assign,
            RsAssignRem => BinaryPrecedence.Assign,
            RsAssignBitAnd => BinaryPrecedence.Assign,
            RsAssignBitOr => BinaryPrecedence.Assign,
            RsAssignBitXor => BinaryPrecedence.Assign,
            RsAssignShl => BinaryPrecedence.Assign,
            RsAssignShr => BinaryPrecedence.Assign,

            RsOr => BinaryPrecedence.Or,
            RsAnd => BinaryPrecedence.And,

            RsBitOr => BinaryPrecedence.BitOr,
            RsBitXor => BinaryPrecedence.BitXor,
            RsBitAnd => BinaryPrecedence.BitAnd,

            RsEq => BinaryPrecedence.Equality,
            RsNe => BinaryPrecedence.Equality,

            RsLt => BinaryPrecedence.Comparison,
            RsLe => BinaryPrecedence.Comparison,
            RsGe => BinaryPrecedence.Comparison,
            RsGt => BinaryPrecedence.Comparison,

            RsShl => BinaryPrecedence.Shift,
            RsShr => BinaryPrecedence.Shift,

            RsAdd => BinaryPrecedence.Additive,
            RsSub => BinaryPrecedence.Additive,

            RsMul => BinaryPrecedence.Multiplicative,
            RsDiv => BinaryPrecedence.Multiplicative,
            RsRem => BinaryPrecedence.Multiplicative,

            RsRange => BinaryPrecedence.Range,
            RsRangeInclusive => BinaryPrecedence.Range,
            
            _ => BinaryPrecedence.Higher
        };
    }

    private string GetOperatorRepresentation(RsExpression op)
    {
        return op switch
        {
            RsAssign => "=",
            RsAssignAdd => "+=",
            RsAssignSub => "-=",
            RsAssignMul => "*=",
            RsAssignDiv => "/=",
            RsAssignRem => "%=",
            RsAssignBitAnd => "&=",
            RsAssignBitOr => "|=",
            RsAssignBitXor => "^=",
            RsAssignShl => "<<=",
            RsAssignShr => ">>=",

            RsOr => "||",
            RsAnd => "&&",

            RsBitOr => "|",
            RsBitXor => "^",
            RsBitAnd => "&",

            RsEq => "==",
            RsNe => "!=",

            RsLt => "<",
            RsLe => "<=",
            RsGe => ">=",
            RsGt => ">",

            RsShl => "<<",
            RsShr => ">>",

            RsAdd => "+",
            RsSub => "-",

            RsMul => "*",
            RsDiv => "/",
            RsRem => "%",

            RsRange => "..",
            RsRangeInclusive => "..=",
            
            RsUnaryMinus => "-",
            RsNot => "+",
            
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void GenerateExpression(RsExpression expression)
    {
        using var _ = Context(TranslationContext.Expression);
        
        if (expression is RsBinaryOp op)
        {
            var precedence = GetPrecedence(op);
            var leftPrecedence = GetPrecedence(op.Left);
            var rightPrecedence = GetPrecedence(op.Right);

            Add(leftPrecedence >= precedence ? "% " : "(%) ", op.Left);
            Add(GetOperatorRepresentation(op));
            Add(rightPrecedence >= precedence ? " %" : " (%) ", op.Right);
        }
        else if (expression is RsUnaryOp)
        {
            switch (expression)
            {
                case RsUnaryMinus minus:
                    Add("-%", minus.Arg);
                    break;
                case RsNot not:
                    Add("!%", not.Arg);
                    break;
                case RsDeref deref:
                    Add("%", deref.Arg);
                    break;
                case RsRef { Mutable: false } immutableRef:
                    Add("%", immutableRef.Arg);
                    break;
                case RsRef { Mutable: true } mutableRef:
                    Add("ref %", mutableRef.Arg);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else if (expression is RsField field)
        {
            Add(
                field.Value is not RsBinaryOp or RsUnaryOp ? "%.%" : "(%).%", 
                field.Value,
                field.Field
                );
        }
        else if (expression is RsPath path)
        {
            Add("%.%", path.Prefix.Unwrap(), path.Name);
        }
    }
}
