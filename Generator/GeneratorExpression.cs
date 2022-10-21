using Rust2SharpTranslator.Parser;
using Rust2SharpTranslator.Utils;

namespace Rust2SharpTranslator.Generator;

public partial class Generator
{
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

        switch (expression)
        {
            case RsAs cast:
                Add("((%)%)", cast.Right, cast.Left);
                break;

            case RsBinaryOp op:
                var precedence = GetPrecedence(op);
                var leftPrecedence = GetPrecedence(op.Left);
                var rightPrecedence = GetPrecedence(op.Right);

                Add(leftPrecedence >= precedence ? "% " : "(%) ", op.Left);
                Add(GetOperatorRepresentation(op));
                Add(rightPrecedence >= precedence ? " %" : " (%) ", op.Right);
                break;

            case RsUnaryOp:
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

                break;

            case RsField field:
                Add(
                    field.Value is not RsBinaryOp or RsUnaryOp ? "%.%" : "(%).%",
                    field.Value,
                    field.Field
                );
                break;

            case RsPath path:
                Add("%.%", path.Prefix.Unwrap(), path.Name);
                break;

            case RsCall call:
                Generate(call.Function);
                AddJoined(", ", call.Arguments.ToArray<RsNode>(), "(", ")");
                break;

            case RsIndex index:
                Add("%[%]", index.Value, index.Index);
                break;

            case RsConstructor ctor:
            {
                Add("(new % { ", ctor.Type);
                foreach (var (name, value) in ctor.Parameters)
                {
                    Add("% = %", name, value);
                    if (name != ctor.Parameters.Last().Item1)
                        Add(", ");
                }

                Add(" })");
                break;
            }

            case RsLiteralUnit:
                Add("void");
                break;

            case RsLiteralInt @int:
                Add(@int.Repr);
                break;

            case RsLiteralFloat @float:
                Add(@float.Repr);
                break;

            case RsLiteralChar @char:
                Add("'");
                Add(@char.Repr);
                Add("'");
                break;

            case RsLiteralString @string:
                Add("\"");
                Add(@string.Repr);
                Add("\"");
                break;

            case RsLiteralByte @byte:
                Add("b'");
                Add(@byte.Repr);
                Add("'");
                break;

            case RsLiteralByteString byteString:
                Add("b\"");
                Add(byteString.Repr);
                Add("\"");
                break;

            case RsLiteralArray array:
                Add("new []{ ");
                AddJoined(", ", array.Elements.ToArray<RsNode>());
                Add(" }");
                break;

            case RsBlock block:
                using (LambdaBlock())
                {
                    Generate(block);
                }

                break;

            default:
                Add($"/* Not implemented {expression} */");
                break;
        }
    }

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
        Higher
    }
}
