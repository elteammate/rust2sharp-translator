namespace Rust2SharpTranslator.Parser;

public abstract record RsNode;

public abstract record RsStatement : RsNode;

public abstract record RsExpression : RsStatement;

public record RsName(string Name) : RsExpression;

public record RsUnderscore() : RsName("_");

public record RsSelfType() : RsName("Self");

public record RsSelf() : RsName("self");

public record RsSuper() : RsName("super");

public record RsCrate() : RsName("crate");

public record RsLabel(string Name) : RsNode;

public record RsPath(RsExpression? Prefix, RsName Name) : RsExpression;

public record RsLet
    (RsName Name, bool? Mutable, RsExpression? Type, RsExpression? Value) : RsStatement;

public record RsIf(RsExpression Condition, RsExpression Then, RsExpression? Else) : RsExpression;

public record RsLoop(RsExpression Body) : RsExpression;

public record RsWhile(RsExpression Condition, RsStatement Body) : RsExpression;

public record RsFor(RsName Binding, RsExpression Iterator, RsStatement Body) : RsExpression;

public record RsBlock(RsStatement[] Statements, RsExpression? Expression) : RsExpression;

public record RsBreak(RsExpression? Value) : RsExpression;

public record RsContinue : RsExpression;

public record RsReturn(RsExpression? Value) : RsExpression;

public record RsEmptyStatement : RsStatement;

public record RsMatchArm(RsExpression Pattern, RsExpression Body) : RsNode;

public record RsMatch(RsExpression Value, RsMatchArm[] Arms) : RsExpression;

public abstract record RsLiteral : RsExpression;

public record RsLiteralString(string Repr) : RsLiteral;

public record RsLiteralByteString(string Repr) : RsLiteral;

public record RsLiteralByte(string Repr) : RsLiteral;

public record RsLiteralChar(string Repr) : RsLiteral;

public record RsLiteralInt(string Repr) : RsLiteral;

public record RsLiteralFloat(string Repr) : RsLiteral;

public record RsLiteralBool(string Repr) : RsLiteral;

public record RsLiteralUnit : RsLiteral;

public record RsLiteralArray(RsExpression[] Elements) : RsLiteral;

public record RsCall(RsExpression Function, RsExpression[] Arguments) : RsExpression;

public record RsIndex(RsExpression Value, RsExpression Index) : RsExpression;

public record RsField(RsExpression Value, RsName Field) : RsExpression;

public record RsConstructor(RsExpression Type, (RsName, RsExpression)[] Parameters) : RsExpression;

public record RsWithGenerics
    (RsExpression Value, RsLifetime[] Lifetimes, RsGeneric[] Generics) : RsExpression;

public abstract record RsUnaryOp(RsExpression Arg) : RsExpression;

public abstract record RsBinaryOp(RsExpression Left, RsExpression Right) : RsExpression;

public record RsNot(RsExpression Arg) : RsUnaryOp(Arg);

public record RsUnaryMinus(RsExpression Arg) : RsUnaryOp(Arg);

public record RsDeref(RsExpression Arg) : RsUnaryOp(Arg);

public record RsRef(RsLabel? Lifetime, bool Mutable, RsExpression Arg) : RsUnaryOp(Arg);

public record RsAdd(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsSub(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsMul(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsDiv(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsRem(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsBitAnd(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsBitOr(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsBitXor(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsShl(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsShr(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsAnd(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsOr(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsEq(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsNe(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsLt(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsLe(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsGt(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsGe(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsAs(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsRange(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsRangeInclusive(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsAssign(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsAssignAdd(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsAssignSub(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsAssignMul(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsAssignDiv(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsAssignRem(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsAssignBitAnd(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsAssignBitOr(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsAssignBitXor(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsAssignShl(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsAssignShr(RsExpression Left, RsExpression Right) : RsBinaryOp(Left, Right);

public record RsGeneric(RsName Name, RsExpression[] Bounds) : RsNode;

public record RsLifetime(RsLabel Name) : RsNode;

public record RsStructField(RsName Name, RsExpression Type) : RsNode;

public record RsStruct(RsName Name, RsLifetime[] Lifetimes, RsGeneric[] Generics,
    RsStructField[] Fields
) : RsExpression;

public record RsEnum(RsName Name, RsLifetime[] Lifetimes, RsGeneric[] Generics, RsStruct[] Variants) : RsNode;

public record RsParameter(RsName Name, RsExpression Type) : RsNode;

public record RsSelfParameter(RsExpression Self) : RsParameter(new RsSelf(), new RsSelfType());

public record RsFunction(RsName Name, RsLifetime[] Lifetimes, RsGeneric[] Generics,
    RsParameter[] Parameters, RsExpression ReturnType, RsBlock? Body
) : RsNode;

public record RsTrait(RsName Name, RsLifetime[] Lifetimes, RsGeneric[] Generics, RsFunction[] Functions) : RsNode;

public record RsImpl(RsExpression Type, RsExpression? Trait, RsFunction[] Functions) : RsNode;

public record RsModule(RsName? Name, RsNode[] Nodes) : RsNode;

public record RsUse(RsPath Path) : RsNode;

public record RsTypeDecl(RsName Name, RsLifetime[] Lifetimes, RsGeneric[] Generics,
    RsExpression Definition
) : RsNode;

public record RsClosure
    (RsParameter[] Parameters, RsExpression? ReturnType, RsExpression Body) : RsExpression;
