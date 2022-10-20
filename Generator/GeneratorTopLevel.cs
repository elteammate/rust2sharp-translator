using Rust2SharpTranslator.Parser;

namespace Rust2SharpTranslator.Generator;

public partial class Generator
{
    private void GenerateModule(RsModule module)
    {
        AddLine("static class %", module.Name!);
        using var _ = Block();
        foreach (var item in module.Nodes) Generate(item);
    }

    private void GenerateFunction(RsFunction function)
    {
        AddLine("% %", function.ReturnType, function.Name);
        
        using var _ = Block();
    }
    
    private void GenerateTopLevel(RsModule module)
    {
        RegisterName(new RsName("root"));
        AddLine("static class Root");
        using var _ = Block();
        foreach (var item in module.Nodes) Generate(item);
    }
}
