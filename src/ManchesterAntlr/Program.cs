namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using AlcTableau.ManchesterAntlr;
using IriTools;

public static class Program
{


    public static IriReference testFile(string filename, IAntlrErrorListener<IToken>? errorListener = null)
    {
        using TextReader text_reader = File.OpenText(filename);

        return testReader(text_reader, errorListener);

    }

    public static IriReference testReader(TextReader text_reader, IAntlrErrorListener<IToken>? errorListener = null)
    {

        // Create an input character stream from standard in
        var input = new AntlrInputStream(text_reader);
        // Create an ExprLexer that feeds from that stream
        var lexer = new IriGrammarLexer(input);
        // Create a stream of tokens fed by the lexer
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        // Create a parser that feeds off the token stream
        var parser = new IriGrammarParser(tokens);
        // Begin parsing at rule r
        IAntlrErrorListener<IToken> customErrorListener = new ConsoleErrorListener<IToken>();
        if (errorListener != null)
        {
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);
            customErrorListener = errorListener;
        }
        IParseTree tree = parser.rdfiri();
        var visitor = new IriGrammarVisitor(new Dictionary<string, IriReference>(), customErrorListener);
        return visitor.Visit(tree);


    }

    public static IriReference testString(string owl, IAntlrErrorListener<IToken>? errorListener = null)
    {
        using TextReader text_reader = new StringReader(owl);
        return testReader(text_reader, errorListener);
    }


    public static void Main(string[] args)
    {
        var iriString = args[0];
        Console.WriteLine($"Input string: {iriString}");
        var testIri = new IriReference(iriString);
        var parsedIri = testString($"<{testIri.ToString()}>");
        Console.WriteLine($"parsed IRI: {parsedIri}");

    }


}