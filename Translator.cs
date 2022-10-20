namespace Rust2SharpTranslator;

public static class Translator
{
    public static string Translate(string source)
    {
        var tokens = new Lexer.Lexer(source).Lex();
        var ast = new Parser.Parser(tokens).ParseTopLevel();

        return ast.ToString();
    }
}
