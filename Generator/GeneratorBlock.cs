using Rust2SharpTranslator.Parser;
using Rust2SharpTranslator.Utils;

namespace Rust2SharpTranslator.Generator;

public partial class Generator
{
    public void GenerateExpressionStatement(RsExpression expression)
    {
        using var _ = Context(TranslationContext.Expression);
        Generate(expression);
    }

    public void GenerateLet(RsLet let)
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
}
