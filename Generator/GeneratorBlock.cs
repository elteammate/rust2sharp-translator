using Rust2SharpTranslator.Parser;
using Rust2SharpTranslator.Utils;

namespace Rust2SharpTranslator.Generator;

public partial class Generator
{
    private void GenerateExpressionStatement(RsExpression expression)
    {
        using var _ = Context(TranslationContext.Expression);
        AddLine("%;", expression);
    }

    private void GenerateLet(RsLet let)
    {
        using var _ = Context(TranslationContext.Expression);

        if (let.Type == null)
            AddLine("var | = %;", let.Value.Unwrap());
        else if (let.Value == null)
            AddLine("% |;", let.Type);
        else
            AddLine("% | = %;", let.Type, let.Value);

        RegisterName(let.Name);
        AddAtWaypoint("%", let.Name);
    }

    private static RsBlock ElevateReturn(RsBlock block)
    {
        if (block.Expression == null) return block;

        var returnStatement = new RsReturn(block.Expression);
        return new RsBlock(block.Statements.Append(returnStatement).ToArray(), null);
    }

    private void GenerateIf(RsIf @if, bool elevateReturn)
    {
        using (Context(TranslationContext.Expression))
        {
            AddLine("if (%)", @if.Condition);
        }

        using (Context(TranslationContext.Function))
        {
            Generate(elevateReturn ? ElevateReturn((@if.Then as RsBlock).Unwrap()) : @if.Then);
        }

        if (@if.Else == null) return;

        AddLine("else");
        if (@if.Else is RsIf @else)
            GenerateIf(@else, elevateReturn);
        else
            Generate(elevateReturn ? ElevateReturn((@if.Else as RsBlock).Unwrap()) : @if.Else);
    }

    private void GenerateIf(RsIf @if)
    {
        if (_context == TranslationContext.Expression && (@if.Then as RsBlock)?.Expression != null)
            using (LambdaBlock())
            using (Block())
            {
                GenerateIf(@if, true);
            }
        else
            GenerateIf(@if, false);
    }

    private void GenerateReturn(RsReturn @return)
    {
        using (Context(TranslationContext.Expression))
        {
            AddLine("return %;", @return.Value.Unwrap());
        }
    }
}
