using System.Numerics;

namespace Rust2SharpTranslator.Parser;

public abstract record RsNode;

public abstract record RsStatement : RsNode;

public abstract record RsExpression : RsStatement;

public record RsName(string Name) : RsExpression;

public record RsLabel(string Name) : RsNode;

public record RsPath(RsExpression[] Pieces) : RsExpression;

public abstract record RsType : RsNode;
public record RsSimpleType(RsPath Path) : RsType;
public record RsRefType(RsType Type, RsLifetime Lifetime, bool Mutable) : RsType;
public record RsTupleType(RsType[] Types) : RsType;
public record RsArrayType(RsType Type, int? Size) : RsType;

public record RsLetDecl(RsName Name, bool? Mutable, RsType Type, RsExpression? Value) : RsStatement;

public record RsIf(RsExpression Condition, RsExpression Then, RsExpression? Else) : RsExpression;
public record RsLoop(RsExpression Body) : RsExpression;
public record RsWhile(RsExpression Condition, RsStatement Body, RsLabel? Label) : RsExpression;
public record RsFor(RsName Binding, RsExpression Iterator, RsStatement Body, RsLabel? Label) : RsExpression;
public record RsBlock(RsStatement[] Statements, RsExpression Expression, RsLabel? Label) : RsExpression;
public record RsBreak(RsLabel? Label, RsExpression? Value) : RsExpression;
public record RsContinue(RsLabel? Label) : RsExpression;
public record RsReturn(RsExpression? Value) : RsExpression;

public record RsMatchArm(RsExpression Pattern, RsExpression Body) : RsNode;
public record RsMatch(RsExpression Value, RsMatchArm[] Arms) : RsExpression;

public abstract record RsLiteral : RsExpression;

public record RsLiteralString(string Value) : RsLiteral;
public record RsLiteralByteString(string Value) : RsLiteral;
public record RsLiteralByte(char Value) : RsLiteral;
public record RsLiteralChar(char Value) : RsLiteral;
public record RsLiteralInt(BigInteger Value) : RsLiteral;
public record RsLiteralFloat(double Value) : RsLiteral;
public record RsLiteralBool(bool Value) : RsLiteral;
public record RsLiteralUnit : RsLiteral;
public record RsLiteralArray(RsExpression[] Elements) : RsLiteral;

public record RsCall(RsExpression Function, RsExpression[] Arguments) : RsExpression;
public record RsIndex(RsExpression Value, RsExpression Index) : RsExpression;
public record RsField(RsExpression Value, RsName Field) : RsExpression;

public abstract record RsUnaryOp(RsExpression Arg) : RsExpression;
public abstract record RsBinaryOp(RsExpression Left, RsExpression Right) : RsExpression;

public record RsNot(RsExpression Arg) : RsUnaryOp(Arg);
public record RsTilde(RsExpression Arg) : RsUnaryOp(Arg);
public record RsUnaryPlus(RsExpression Arg) : RsUnaryOp(Arg);
public record RsUnaryMinus(RsExpression Arg) : RsUnaryOp(Arg);
public record RsDeref(RsExpression Arg) : RsUnaryOp(Arg);
public record RsRef(bool Mutable, RsExpression Arg) : RsUnaryOp(Arg);

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

public record RsGeneric(RsName Name, RsType[] Bounds) : RsNode;
public record RsLifetime(RsLabel Name) : RsNode;

public record RsStructField(RsName Name, RsType Type) : RsNode;
public record RsStruct(RsName Name, RsLifetime[] Lifetimes, RsGeneric[] Generics, RsStructField[] Fields) : RsExpression;

public record RsEnum(RsName Name, RsStruct[] Variants) : RsNode;

public record RsParameter(RsName Name, RsType Type) : RsNode;
public record RsFunction(RsName Name, RsLifetime[] Lifetimes, RsGeneric[] Generics, RsParameter[] Parameters, RsType ReturnType, RsExpression? Body) : RsNode;

public record RsTrait(RsName Name, RsFunction[] Functions) : RsNode;

public record RsImpl(RsType Type, RsTrait Trait, RsFunction[] Functions) : RsNode;

public record RsModule(RsPath Path, RsNode[] Nodes) : RsNode;

public record RsUse(RsPath Path) : RsNode;

public record RsTypeDecl(RsName Name, RsLifetime[] Lifetimes, RsGeneric[] Generics, RsType Definition) : RsNode;

public record RsClosure(RsParameter[] Parameters, RsType? ReturnType, RsExpression Body) : RsExpression;
