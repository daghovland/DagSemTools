using AlcTableau;
using AlcTableau.ManchesterAntlr;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IriTools;

namespace ManchesterAntlr;

public static class Parser
{
    
    public static ALC.OntologyDocument TestFile(string filename){
        using TextReader textReader = File.OpenText(filename);
        return TestReader(textReader);
    }

    public static ALC.OntologyDocument TestReader(TextReader textReader, Dictionary<string, IriReference> prefixes){
        
        var input = new AntlrInputStream(textReader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        parser.ErrorHandler = new BailErrorStrategy();
        IParseTree tree = parser.ontologyDocument();
        var visitor = new ManchesterVisitor();
        return visitor.Visit(tree);
    }
    
    public static ALC.OntologyDocument TestReader(TextReader textReader) =>
        TestReader(textReader, new Dictionary<string, IriReference>());
    
    public static ALC.OntologyDocument TestString(string owl){
        using TextReader textReader = new StringReader(owl);
        return TestReader(textReader);
    }
    

}