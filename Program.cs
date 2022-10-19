namespace Rust2SharpTranslator;

public static class Program
{
    public static int Main(string[] args)
    {
        string input;
        try
        {
            input = File.ReadAllText(args[0]);
        }
        catch (IndexOutOfRangeException)
        {
            Console.WriteLine("Usage: <program> <input file> [output file]");
            return -1;
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("Input file does not exist: " + e.Message);
            return -1;
        }
        catch (IOException e)
        {
            Console.WriteLine("Error reading file: " + e.Message);
            return -1;
        }

        var output = Translator.Translate(input);

        if (args.Length > 1)
            try
            {
                File.WriteAllText(args[1], output);
            }
            catch (IOException e)
            {
                Console.WriteLine("Error writing file: " + e.Message);
                return -1;
            }
        else
            Console.WriteLine(output);

        return 0;
    }
}
