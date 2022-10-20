using System.Text;
using Rust2SharpTranslator.Parser;
using Rust2SharpTranslator.Utils;

namespace Rust2SharpTranslator.Generator;

public partial class Generator
{
    private readonly RsNode _node;
    private readonly StringBuilder _builder;

    private int _indentLevel;

    private Stack<Dictionary<string, string>> _scopes = new();
    private bool _builderEndedLine = true;

    public Generator(RsNode node)
    {
        _node = node;
        _builder = new StringBuilder();
        _scopes.Push(new Dictionary<string, string>());
    }

    private void Push()
    {
        _scopes.Push(new Dictionary<string, string>());
        _indentLevel++;
    }

    private void Pop()
    {
        _indentLevel--;
        _scopes.Pop();
    }

    private class WithIndent : IDisposable
    {
        private readonly Generator _generator;

        public WithIndent(Generator generator)
        {
            _generator = generator;
            _generator.Push();
        }

        public void Dispose()
        {
            _generator.Pop();
        }
    }
    
    private class WithBlock : IDisposable
    {
        private readonly Generator _generator;

        public WithBlock(Generator generator)
        {
            _generator = generator;
            _generator.AddLine("{");
            _generator.Push();
        }

        public void Dispose()
        {
            _generator.Pop();
            _generator.AddLine("}");
        }
    }
    
    private WithIndent Indent() => new(this);
    private WithBlock Block() => new(this);

    private void AddLine(string line = "", params RsNode[] items)
    {
        Add(line + "\n", items);
    }
    
    private void Add(string str, params RsNode[] items)
    {
        if (_builderEndedLine)
            _builder.Append(new string(' ', _indentLevel * 4));

        var itemStream = new Stream<RsNode>(items);
        
        foreach (var c in str)
        {
            if (c != '%')
                _builder.Append(c);
            else
                Generate(itemStream.Next().Unwrap());

            _builderEndedLine = c == '\n';
        }
    }
    
    public string Generate()
    {
        GenerateTopLevel((_node as RsModule)!);
        return _builder.ToString();
    }

    private string? TryFindName(string name)
    {
        string? foundName = null;
        foreach (var scope in _scopes.Where(scope => scope.ContainsKey(name)))
            foundName = scope[name];
        return foundName;
    }

    private bool ContainsName(string name) => 
        _scopes.Any(scope => scope.ContainsValue(name));

    private string FindName(string name) => TryFindName(name) ?? name;

    private string RegisterName(RsName name)
    {
        if (!ContainsName(name.Name))
        {
            _scopes.Peek().Add(name.Name, name.Name);
            return name.Name;
        }
        var suffix = 1;
        while (ContainsName(name.Name + suffix))
            suffix++;

        var result = name.Name + suffix;
        _scopes.Peek()[name.Name] = result;
        return result;
    }

    private static string ToCamelCase(string name)
    {
        var builder = new StringBuilder();
        var capitalize = true;
        
        foreach (var c in name)
        {
            if (c == '_') 
                capitalize = true;
            else { 
                if (capitalize)
                    builder.Append(c.ToString().ToUpper());
                else
                    builder.Append(c);
                
                capitalize = false;
            }
        }

        return builder.ToString();
    }

    private void GenerateName(RsName name)
    {
        Add(ToCamelCase(FindName(name.Name)));
    }
}
