using AlcTableau.Parser;

namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using AlcTableau.ManchesterAntlr;
using IriTools;

public static class Program
{


    public static IriReference testFile(string filename, TextWriter errorListener)  
    {
        using TextReader text_reader = File.OpenText(filename);

        return testReader(text_reader, errorListener);

    }

    public static IriReference testReader(TextReader text_reader, TextWriter errorOutput)
    {

        var input = new AntlrInputStream(text_reader);
        var lexer = new IriGrammarLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new IriGrammarParser(tokens);
        var customErrorListener = new ParserErrorListener(errorOutput);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(customErrorListener);
        IParseTree tree = parser.rdfiri();
        var visitor = new IriGrammarVisitor(new Dictionary<string, IriReference>(), customErrorListener);
        return visitor.Visit(tree);


    }

    public static IriReference testString(string owl, TextWriter errorOutput)
    {
        using TextReader text_reader = new StringReader(owl);
        return testReader(text_reader, errorOutput);
    }


    public static void Main(string[] args)
    {
        var iriString = args[0];
        Console.WriteLine($"Input string: {iriString}");
        var testIri = new IriReference(iriString);
        var parsedIri = testString($"<{testIri.ToString()}>", Console.Error);
        Console.WriteLine($"parsed IRI: {parsedIri}");

    }


}