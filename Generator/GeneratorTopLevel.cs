using Rust2SharpTranslator.Parser;

namespace Rust2SharpTranslator.Generator;

public partial class Generator
{
    private void GenerateModule(RsModule module)
    {
        AddLine("static class %", module.Name!);
        using var _ = Block();
        AddJoined("\n", module.Nodes);
    }

    private void GenerateFunction(RsFunction function)
    {
        if (function.Body == null) Add("abstract ");

        using (Context(TranslationContext.Expression))
        {
            Add("% %", function.ReturnType, function.Name);
        }

        GenerateGenerics(function.Generics);
        GenerateParameters(function.Parameters);

        if (function.Body == null)
            Add(";");
        else
            GenerateBlock(function.Body);
    }

    private void GenerateTopLevel(RsModule module)
    {
        var rootModule = new RsModule(new RsName("crate"), module.Nodes);
        _scopes.Peek().Add("crate", "crate");
        GenerateModule(rootModule);
    }

    private void GenerateBlock(RsBlock block)
    {
        var outerContext = _context;
        using var _1 = Block();
        using var _2 = Context(TranslationContext.Function);

        foreach (var item in block.Statements) Generate(item);
        
        if (block.Expression == null) return;
        
        if (outerContext == TranslationContext.Module)
            using (Context(TranslationContext.Expression))
                AddLine("return %;", block.Expression);
        else
            Generate(block.Expression);
    }

    private void GenerateGeneric(RsGeneric generic)
        => Generate(generic.Name);

    private void GenerateGenerics(IEnumerable<RsGeneric> generics)
        => AddJoined(", ", generics.ToArray<RsNode>(), "<", ">", false);

    private void GenerateParameter(RsParameter parameter)
    {
        RegisterName(parameter.Name);
        Add("% %", parameter.Type, parameter.Name);
    }

    private void GenerateParameters(IEnumerable<RsParameter> parameters)
        => AddJoined(", ", parameters.ToArray<RsNode>(), "(", ")");
}
