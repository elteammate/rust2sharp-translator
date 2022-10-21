using System.Text;
using Rust2SharpTranslator.Parser;
using Rust2SharpTranslator.Utils;

namespace Rust2SharpTranslator.Generator;

/// <summary>
///     A base class for code generation
///     The main method is Generate, which looks up
///     Generate...() method, used to generate the object
/// </summary>
public partial class Generator
{
    /// <summary>
    ///     The generated code is placed here
    /// </summary>
    private readonly StringBuilder _builder;

    /// <summary>
    ///     The root of AST to generate
    /// </summary>
    private readonly RsNode _node;

    /// <summary>
    ///     The scopes of variables
    /// </summary>
    private readonly Stack<Dictionary<string, string>> _scopes = new();

    /// <summary>
    ///     A stack of waypoints for builder
    /// </summary>
    private readonly Stack<int> _waypoints = new();

    /// <summary>
    ///     Whether the builder finished with \n
    /// </summary>
    private bool _builderEndedLine = true;

    /// <summary>
    ///     The current indentation level
    /// </summary>
    private int _indentLevel;

    /// <summary>
    ///     Used to assign unique names to variables
    /// </summary>
    private int _nameCounter;

    /// <summary>
    ///     Used to create temporary variables with unique names
    /// </summary>
    private int _tempVarCounter;

    public Generator(RsNode node)
    {
        _node = node;
        _builder = new StringBuilder();
        _scopes.Push(new Dictionary<string, string>());
    }

    /// <summary>
    ///     Creates a new scope with bigger indent
    /// </summary>
    private void Push()
    {
        _scopes.Push(new Dictionary<string, string>());
        _indentLevel++;
    }

    /// <summary>
    ///     Removes one level of indent and one scope
    /// </summary>
    private void Pop()
    {
        _indentLevel--;
        _scopes.Pop();
    }

    /// <summary>
    ///     Context used to increase code indent
    /// </summary>
    private WithIndent Indent() => new(this);

    /// <summary>
    ///     Context used to add braces to the code
    /// </summary>
    private WithBlock Block() => new(this);

    /// <summary>
    ///     Context used to add braces to the code in expressions using lambdas
    /// </summary>
    private WithLambdaBlock LambdaBlock() => new(this);

    /// <summary>
    ///     Add new line to the code
    /// </summary>
    private void AddLine(string line = "", params RsNode[] items)
    {
        Add(line + "\n", items);
    }

    /// <summary>
    ///     Add to the code. The % character in string used to inline nodes
    ///     into string, | used to create waypoings
    /// </summary>
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

        _builderEndedLine = str.Length > 0 && str[^1] == '\n';
    }

    /// <summary>
    ///     Add content to last added waypoing
    /// </summary>
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

    /// <summary>
    ///     Returns escaped and unique name if the variable is already defined
    /// </summary>
    private string? TryFindName(string name)
    {
        return _scopes
            .Where(scope => scope.ContainsKey(name))
            .Select(scope => scope[name])
            .FirstOrDefault();
    }

    /// <summary>
    ///     Checks whether the variable is already defined
    /// </summary>
    private bool ContainsName(string name) =>
        _scopes.Any(scope => scope.ContainsValue(name));


    /// <summary>
    ///     Returns *probably* escaped and unique name for the variable
    /// </summary>
    private string FindName(string name) => TryFindName(name) ?? name;

    /// <summary>
    ///     Registes a name in the current scope
    /// </summary>
    private void RegisterName(RsName name)
    {
        void SaveName(string newName, string initial)
        {
            _scopes.Peek()[$"${_nameCounter++}${newName}"] = newName;
            _scopes.Peek()[newName] = newName;
            _scopes.Peek()[initial] = newName;
        }

        if (!ContainsName(name.Name))
        {
            SaveName(name.Name, name.Name);
            return;
        }

        var suffix = 1;
        while (ContainsName(name.Name + suffix))
            suffix++;

        SaveName(name.Name + suffix, name.Name);
    }

    private static string ToCamelCase(string name)
    {
        var builder = new StringBuilder();
        var capitalize = true;

        foreach (var c in name)
            if (c == '_')
                capitalize = true;
            else
            {
                if (capitalize)
                    builder.Append(c.ToString().ToUpper());
                else
                    builder.Append(c);

                capitalize = false;
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
        switch (name)
        {
            case RsSelf:
                Add("this");
                break;
            case RsSuper:
                Add("base");
                break;
            case RsUnderscore:
                Add("_");
                break;
            default:
                var escapedName = FindName(name.Name);
                Add(TryGetBuiltin(escapedName) ?? ToCamelCase(escapedName));
                break;
        }
    }

    private RsName GetTempVar()
    {
        var variable = new RsName($"Temp{_tempVarCounter++}");
        RegisterName(variable);
        return variable;
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
            _generator.Add("((%) => ", _generator.GetTempVar());
            _generator.Push();
            _withContext = new WithContext(generator, TranslationContext.Module);
        }

        public void Dispose()
        {
            _withContext.Dispose();
            _generator.Pop();
            _generator.Add(")(0)");
        }
    }
}
