using Rust2SharpTranslator.Parser;

namespace Rust2SharpTranslator.Generator;

/// <summary>
///     Generates everything at module level
/// </summary>
public partial class Generator
{
    private void GenerateDocumented(RsDocumented documented)
    {
        if (documented.Comment is RsLineDocComment lineComment)
            AddLine("///" + lineComment.Content);
        else if (documented.Comment is RsBlockDocComment docComment)
        {
            AddLine("/**");
            using (Indent())
                foreach (var line in docComment.Content.Split("\n"))
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length != 0) AddLine(trimmed);
                }

            AddLine("*/");
        }

        Generate(documented.Node);
    }

    private void GenerateModule(RsModule module)
    {
        AddLine("public static class %", module.Name!);
        using var _ = Block();
        AddJoined("\n", module.Nodes);
    }

    private void GenerateFunction(RsFunction function)
    {
        if (_context == TranslationContext.Module)
            Add("public ");

        if (function.Body == null) Add("abstract ");

        using (Context(TranslationContext.Expression))
        {
            if (function.Parameters.FirstOrDefault() is not RsSelfParameter)
                Add("static ");
            Add("% %", function.ReturnType, function.Name);
        }

        using (Context(TranslationContext.Expression))
        {
            GenerateGenerics(function.Generics);
            GenerateParameters(function.Parameters);
        }

        GenerateGenericBounds(function.Generics);

        using (Context(TranslationContext.Module))
            if (function.Body == null)
                AddLine(";");
            else
                GenerateBlock(function.Body);
    }

    private void GenerateTopLevel(RsModule module)
    {
        var rootModule = new RsModule(new RsName("crate"), module.Nodes);
        _scopes.Peek().Add("crate", "crate");
        GenerateModule(rootModule);
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
        => AddJoined(", ", parameters
            .Where(p => p is not RsSelfParameter)
            .ToArray<RsNode>(), "(", ")");

    private void GenerateStruct(RsStruct @struct)
    {
        Add("public partial class %", @struct.Name);
        GenerateGenerics(@struct.Generics);
        GenerateGenericBounds(@struct.Generics);

        using (Block()) AddJoined("", @struct.Fields.ToArray<RsNode>());
    }

    private void GenerateField(RsStructField field)
    {
        AddLine("public % %;", field.Type, field.Name);
    }

    private void GenerateEnum(RsEnum @enum)
    {
        var name = @enum.Name;
        Add("public abstract partial class %", name);
        GenerateGenerics(@enum.Generics);
        GenerateGenericBounds(@enum.Generics);

        using (Block())
            foreach (var option in @enum.Variants)
            {
                Add("public class % : %", option.Name, name);
                GenerateGenerics(@enum.Generics);
                using (Block())
                using (Context(TranslationContext.Expression))
                    AddJoined("", option.Fields.ToArray<RsNode>());
                AddLine();
            }
    }

    private void GenerateTrait(RsTrait trait)
    {
        Add("public interface %", trait.Name);
        GenerateGenerics(trait.Generics);
        GenerateGenericBounds(trait.Generics);

        using (Block()) AddJoined("", trait.Functions.ToArray<RsNode>());
    }

    private void GenerateImpl(RsImpl impl)
    {
        using (Context(TranslationContext.Expression))
        {
            Add("public partial class %", impl.Type);
            if (impl.Trait != null) Add(" : %", impl.Trait);
        }

        using (Block()) AddJoined("\n", impl.Functions.ToArray<RsNode>());
    }

    private void GenerateTypeDecl(RsTypeDecl typeDecl)
    {
        using (Context(TranslationContext.Expression))
        {
            Add("public using %", typeDecl.Name);
            AddJoined(", ", typeDecl.Generics.ToArray<RsNode>(), "<", ">", false);
            AddLine(" = %;", typeDecl.Definition);
        }
    }

    private void GenerateStatic(RsStatic @static)
    {
        using (Context(TranslationContext.Expression))
            AddLine("public static % % = %;", @static.Type, @static.Name, @static.Value);
    }

    private void GenerateConst(RsConst @const)
    {
        using (Context(TranslationContext.Expression))
            AddLine("public const % % = %;", @const.Type, @const.Name, @const.Value);
    }

    private void GenerateAttributed(RsAttributed attributed)
    {
        foreach (var attribute in attributed.Attributes.Elements)
            using (Context(TranslationContext.Expression))
                AddLine("[%]", attribute);

        Generate(attributed.Node);
    }
}
