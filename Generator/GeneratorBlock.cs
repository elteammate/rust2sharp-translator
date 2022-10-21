using Rust2SharpTranslator.Parser;
using Rust2SharpTranslator.Utils;

namespace Rust2SharpTranslator.Generator;

public partial class Generator
{
    private void GenerateExpressionStatement(RsExpression expression)
    {
        using (Context(TranslationContext.Expression))
            AddLine("%;", expression);
    }

    private void GenerateLet(RsLet let)
    {
        using (Context(TranslationContext.Expression))
        {
            if (let.Type == null)
                AddLine("var | = %;", let.Value.Unwrap());
            else if (let.Value == null)
                AddLine("% |;", let.Type);
            else
                AddLine("% | = %;", let.Type, let.Value);

            RegisterName(let.Name);
            AddAtWaypoint("%", let.Name);
        }
    }

    private static RsBlock ElevateReturn(RsBlock block)
    {
        if (block.Expression == null) return block;

        var returnStatement = new RsReturn(block.Expression);
        return new RsBlock(block.Statements.Append(returnStatement).ToArray(), null);
    }

    private void GenerateIfImpl(RsIf @if)
    {
        using (Context(TranslationContext.Expression))
        {
            AddLine("if (%)", @if.Condition);
        }

        Generate(@if.Then);

        if (@if.Else == null) return;

        AddLine("else");
        if (@if.Else is RsIf @else)
            GenerateIfImpl(@else);
        else
            Generate(@if.Else);
    }

    private void GenerateIf(RsIf @if)
    {
        if (_context == TranslationContext.Expression && (@if.Then as RsBlock)?.Expression != null)
            using (LambdaBlock())
            using (Block())
            {
                GenerateIfImpl(@if);
            }
        else
            GenerateIfImpl(@if);
    }

    private void GenerateReturn(RsReturn @return)
    {
        using (Context(TranslationContext.Expression)) 
            AddLine("return %;", @return.Value.Unwrap());
    }

    private void GenerateContinue()
    {
        using (Context(TranslationContext.Expression)) 
            AddLine("continue;");
    }

    private void GenerateBreak(RsBreak @break)
    {
        using (Context(TranslationContext.Expression)) 
            if (@break.Value == null)
                AddLine("break;");
            else
                AddLine("break %;", @break.Value);
    }

    private void GenerateLoop(RsLoop loop)
    {
        using (Context(TranslationContext.Expression)) 
            AddLine("while (true)");

        using (Context(TranslationContext.Block))
            Generate(loop.Body);
    }

    private void GenerateWhile(RsWhile @while)
    {        
        using (Context(TranslationContext.Expression)) 
            AddLine("while (%)", @while.Condition);

        using (Context(TranslationContext.Block))
            Generate(@while.Body);
    }

    private void GenerateFor(RsFor @for)
    {
        using (Context(TranslationContext.Expression)) 
            AddLine("foreach (var | in %)", @for.Iterator);

        RegisterName(@for.Binding);
        AddAtWaypoint("%", @for.Binding);
        
        using (Context(TranslationContext.Block))
            Generate(@for.Body);
    }

    private void GenerateMatchImpl(RsMatch match)
    {
        using (Context(TranslationContext.Expression))
        {
            AddLine("switch (%)", match.Value.Unwrap());
            using (Block())
            {
                foreach (var arm in match.Arms)
                {
                    if (arm.Pattern is RsUnderscore)
                    {
                        AddLine("default:");
                    }
                    else
                    {
                        AddLine("case %:", arm.Pattern);
                    }
                    
                    using (Indent())
                    {
                        GenerateExpressionStatement(arm.Body);
                        AddLine("break;");
                    }
                }
            }
        }
    }

    private void GenerateMatch(RsMatch match)
    {
        if (_context == TranslationContext.Expression && match.Arms.All(a => a.Body is RsBlock))
            using (LambdaBlock())
            using (Block())
            {
                GenerateMatchImpl(match);
            }
        else
            GenerateMatchImpl(match);
    }
}
