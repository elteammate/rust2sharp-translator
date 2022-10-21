using System.Data;
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
    private readonly Stack<int> _waypoints = new();

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
            if (!_generator._builderEndedLine)
                _generator.AddLine();
            _generator.AddLine("{");
            _generator.Push();
        }

        public void Dispose()
        {
            _generator.Pop();
            _generator.AddLine("}");
        }
    }

    private class WithLambdaBlock : IDisposable
    {
        private readonly Generator _generator;
        private readonly WithContext _withContext;

        public WithLambdaBlock(Generator generator)
        {
            _generator = generator;
            _generator.Add("(((%) => ", _generator.GetTempVar());
            _generator.Push();
            _withContext = new WithContext(generator, TranslationContext.Function);
        }

        public void Dispose()
        {
            _withContext.Dispose();
            _generator.Pop();
            _generator.Add(")(0))");
        }
    }
    
    private WithIndent Indent() => new(this);
    private WithBlock Block() => new(this);
    private WithLambdaBlock LambdaBlock() => new(this);

    private void AddLine(string line = "", params RsNode[] items)
    {
        Add(line + "\n", items);
    }
    
    private void Add(string str, params RsNode[] items)
    {
        switch (str)
        {
            case "":
                return;
            case "\n":
                _builder.AppendLine();
                _builderEndedLine = true;
                return;
        }

        if (_builderEndedLine)
            _builder.Append(new string(' ', _indentLevel * 4));
        _builderEndedLine = false;

        var itemStream = new Stream<RsNode>(items);
        
        foreach (var c in str)
        {
            switch (c)
            {
                case '%':
                    Generate(itemStream.Next().Unwrap());
                    break;
                case '|':
                    _waypoints.Push(_builder.Length);
                    break;
                default:
                    _builder.Append(c);
                    break;
            }
        }
        
        _builderEndedLine = str.Length > 0 && str[^1] == '\n';
    }

    private void AddAtWaypoint(string str, params RsNode[] items)
    {
        var waypoint = _waypoints.Pop();
        var oldBuilderEndedLine = _builderEndedLine;
        _builderEndedLine = false;
        
        var old = _builder.ToString(waypoint, _builder.Length - waypoint);
        _builder.Remove(waypoint, _builder.Length - waypoint);
        Add(str, items);

        _builder.Append(old);
        _builderEndedLine = oldBuilderEndedLine;
    }

    private void AddJoined(
        string sep, 
        RsNode[] items, 
        string prefix = "", 
        string suffix = "",
        bool addPrefixAndSuffixIfEmpty = true,
        bool pushScope = false
    )
    {
        if (items.Length == 0)
        {
            if (addPrefixAndSuffixIfEmpty)
            {
                Add(prefix);
                Add(suffix);
            }
            return;
        }

        if (pushScope) Push();
        Add(prefix);

        for (var i = 0; i < items.Length - 1; i++)
        {
            Generate(items[i]);
            Add(sep);
        }

        Generate(items[^1]);
        Add(suffix);
        if (pushScope) Pop();
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

    private int _nameCounter;
    
    private void RegisterName(RsName name)
    {
        if (!ContainsName(name.Name))
        {
            _scopes.Peek()[$"${_nameCounter++}${name.Name}"] = name.Name;
            _scopes.Peek()[name.Name] = name.Name;
            return;
        }

        var suffix = 1;
        while (ContainsName(name.Name + suffix))
            suffix++;

        var result = name.Name + suffix;
        
        _scopes.Peek()[$"${_nameCounter++}${name.Name}"] = result;
        _scopes.Peek()[name.Name] = result;
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

    private string? TryGetBuiltin(string name)
    {
        return name switch
        {
            "i8" => "sbyte",
            "i16" => "byte",
            "i32" => "int",
            "i64" => "long",
            "isize" => "nint",
            "u8" => "byte",
            "u16" => "ushort",
            "u32" => "uint",
            "u64" => "ulong",
            "usize" => "nunt",
            "f32" => "float",
            "f64" => "double",
            _ => null
        };
    }

    private void GenerateName(RsName name)
    {
        var escapedName = FindName(name.Name);
        Add(TryGetBuiltin(escapedName) ?? ToCamelCase(escapedName));
    }

    private int _tempVarCounter;

    private RsName GetTempVar()
    {
        var variable = new RsName($"Temp{_tempVarCounter++}");
        RegisterName(variable);
        return variable;
    }
}
