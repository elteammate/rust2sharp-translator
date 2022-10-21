using Rust2SharpTranslator.Parser;

namespace Rust2SharpTranslator.Generator;

/// <summary>
///     Maps rust nodes to generator methods
/// </summary>
public partial class Generator
{
    private TranslationContext _context = TranslationContext.Module;

    private WithContext Context(TranslationContext context) => new(this, context);

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

            case RsDocumented documented:
                GenerateDocumented(documented);
                break;

            case RsPub pub:
                Generate(pub.Node);
                break;

            case RsFunction function:
                GenerateFunction(function);
                break;
            
            case RsTypeDecl typeDecl:
                GenerateTypeDecl(typeDecl);
                break;

            case RsGeneric generic:
                GenerateGeneric(generic);
                break;

            case RsParameter parameter:
                GenerateParameter(parameter);
                break;

            case RsBlock block when _context != TranslationContext.Expression:
                GenerateBlock(block);
                break;

            case RsLet let:
                GenerateLet(let);
                break;

            case RsIf @if:
                GenerateIf(@if);
                break;

            case RsReturn @return:
                GenerateReturn(@return);
                break;

            case RsBreak @break:
                GenerateBreak(@break);
                break;

            case RsLoop loop:
                GenerateLoop(loop);
                break;

            case RsWhile @while:
                GenerateWhile(@while);
                break;

            case RsFor @for:
                GenerateFor(@for);
                break;

            case RsMatch match:
                GenerateMatch(match);
                break;

            case RsContinue:
                GenerateContinue();
                break;

            case RsStruct @struct:
                GenerateStruct(@struct);
                break;

            case RsEnum @enum:
                GenerateEnum(@enum);
                break;

            case RsTrait trait:
                GenerateTrait(trait);
                break;

            case RsImpl impl:
                GenerateImpl(impl);
                break;

            case RsStructField field:
                GenerateField(field);
                break;

            case RsExpression expression when _context != TranslationContext.Expression:
                GenerateExpressionStatement(expression);
                break;

            case RsExpression expression:
                GenerateExpression(expression);
                break;

            default:
                AddLine($"// Not implmented yet: {node}");
                break;
        }
    }

    private enum TranslationContext
    {
        Module,
        Block,
        Expression
    }

    private class WithContext : IDisposable
    {
        private readonly Generator _generator;
        private readonly TranslationContext _oldContext;

        public WithContext(Generator generator, TranslationContext context)
        {
            _generator = generator;
            _oldContext = generator._context;
            generator._context = context;
        }

        public void Dispose()
        {
            _generator._context = _oldContext;
        }
    }
}
