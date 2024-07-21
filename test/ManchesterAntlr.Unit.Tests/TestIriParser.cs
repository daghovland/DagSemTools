namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using AlcTableau.ManchesterAntlr;
using FluentAssertions;
using IriTools;

public class TestIriParser
{


    public IriReference testFile(string filename){
        using TextReader text_reader = File.OpenText(filename);

           return testReader(text_reader);

    }

    public IriReference testReader(TextReader text_reader, Dictionary<string, IriReference> prefixes){
        
            var input = new AntlrInputStream(text_reader);
            var lexer = new ConceptLexer(input);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            var parser = new ConceptParser(tokens);
            IParseTree tree = parser.rdfiri();
            var visitor = new IriGrammarVisitor(prefixes);
            return visitor.Visit(tree);
            

    }
    
    public IriReference testReader(TextReader text_reader) =>
        testReader(text_reader, new Dictionary<string, IriReference>());

    public IriReference testString(string owl){
            using TextReader text_reader = new StringReader(owl);
                return testReader(text_reader);
    }


    [Theory]
    [InlineData("http://example.com/ex")]
    [InlineData("http://example.com/ex#abc")]
    [InlineData("http://ex.com/owl2/families.owl")]
    [InlineData("http://example.com/owl/families-v1")]
    [InlineData("http://ex.com/owl2/families?owl")]
    public void TestFullIri(string iri)
    {
        var testIri = new IriReference(iri);
        var parsedIri  = testString($"<{iri}>");
        parsedIri.Should().BeEquivalentTo(testIri);
        
    }
    
    

    [Theory]
    [InlineData("rdf:type")]
    [InlineData("xsd:int")]
    public void TestPrefixedIri(string iri)
    {
        var parsedIri  = testString(iri);
        parsedIri.Should().NotBeNull();

    }
    
    [Fact]
    public void TestCustomPrefixedIri()
    {
        var prefix = new IriReference("https://example.com/vocab/ont#");
        var prefixes = new Dictionary<string, IriReference>()
        {
            { "ex", prefix }
        };
        using TextReader textReader = new StringReader("ex:className");
            var parsedIri  = testReader(textReader, prefixes);
            parsedIri.Should().BeEquivalentTo(new IriReference($"{prefix}className"));
    }

    [Fact]
    public void TestDefaultPrefixedIri()
    {
        var prefix = new IriReference("https://example.com/vocab/ont#");
        var prefixes = new Dictionary<string, IriReference>()
        {
            { "", prefix }
        };
        using TextReader textReader = new StringReader(":className");
        var parsedIri  = testReader(textReader, prefixes);
        parsedIri.Should().BeEquivalentTo(new IriReference($"{prefix}className"));
        using TextReader textReader2 = new StringReader("className");
        var parsedIri2  = testReader(textReader2, prefixes);
        parsedIri2.Should().BeEquivalentTo(new IriReference($"{prefix}className"));
    }

}