namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using AlcTableau.ManchesterAntlr;
using FluentAssertions;
using AlcTableau.ManchesterAntlr;
using AlcTableau;
using FluentAssertions;
using IriTools;

public class TestParser
{


    private ALC.Concept TestFile(string filename){
        using TextReader textReader = File.OpenText(filename);
        return TestReader(textReader);
    }

    private ALC.Concept TestReader(TextReader textReader, Dictionary<string, IriReference> prefixes){
        
        var input = new AntlrInputStream(textReader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        IParseTree tree = parser.start();
        var visitor = new ConceptVisitor(prefixes);
        return visitor.Visit(tree);
    }
    
    public ALC.Concept TestReader(TextReader textReader) =>
        TestReader(textReader, new Dictionary<string, IriReference>());
    public ALC.Concept TestString(string owl){
        using TextReader textReader = new StringReader(owl);
        return TestReader(textReader);
    }

    [Fact]
    public void TestSmallestOntology()
    {
        var parsed = TestString("Prefix: Ontology:");
        parsed.Should().NotBeNull();
    }

    
    [Fact]
    public void TestOntologyWithIri()
    {
        var parsedOntology = TestString("Prefix: Ontology: <https://example.com/ontology>");
        parsedOntology.Should().NotBeNull();
    }
    


}