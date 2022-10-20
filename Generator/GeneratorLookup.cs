using Rust2SharpTranslator.Parser;

namespace Rust2SharpTranslator.Generator;

public partial class Generator
{
    private void Generate(RsNode node)
    {
        switch (node)
        {
            case RsName name:
                GenerateName(name);
                break;
            
            case RsModule module:
                GenerateModule(module);
                break;
            case RsFunction function:
                GenerateFunction(function);
                break;
            
            default:
                AddLine($"// Not implmented yet: {node}");
                break;
        }
    }
}
