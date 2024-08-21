using AlcTableau;
using AlcTableau.ManchesterAntlr;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IriTools;

namespace ManchesterAntlr;

public static class Parser
{

    public static ALC.OntologyDocument ParseFile(string filename)
    {
        using TextReader textReader = File.OpenText(filename);
        return ParseReader(textReader);
    }

    public static ALC.OntologyDocument ParseReader(TextReader textReader, Dictionary<string, IriReference> prefixes)
    {

        var input = new AntlrInputStream(textReader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        parser.ErrorHandler = new BailErrorStrategy();
        IParseTree tree = parser.ontologyDocument();
        var visitor = new ManchesterVisitor();
        return visitor.Visit(tree);
    }

    public static ALC.OntologyDocument ParseReader(TextReader textReader) =>
        ParseReader(textReader, new Dictionary<string, IriReference>());

    public static ALC.OntologyDocument ParseString(string owl)
    {
        using TextReader textReader = new StringReader(owl);
        return ParseReader(textReader);
    }


}