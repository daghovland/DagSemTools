namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using AlcTableau.ManchesterAntlr;
using AlcTableau;
using FluentAssertions;
using IriTools;

public class TestConceptParser
{
    private ALC.Concept testFile(string filename){
        using TextReader text_reader = File.OpenText(filename);
           return testReader(text_reader);
    }

    private ALC.Concept testReader(TextReader text_reader, Dictionary<string, ALC.Concept> prefixes){
        
            var input = new AntlrInputStream(text_reader);
            var lexer = new ConceptLexer(input);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            var parser = new ConceptParser(tokens);
            IParseTree tree = parser.start();
            var visitor = new ConceptVisitor();
            return visitor.Visit(tree);
    }
    
    public ALC.Concept testReader(TextReader text_reader) =>
        testReader(text_reader, new Dictionary<string, ALC.Concept>());
    public ALC.Concept testString(string owl){
            using TextReader text_reader = new StringReader(owl);
                return testReader(text_reader);
    }
    
    [Fact]
    public void TestConjunction()
    {
        var conceptString = "<http://example.com/ex1> and <http://example.com/ex2>";
        var parsedIri  = testString(conceptString);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewConjunction(
                ALC.Concept.NewConceptName(new IriReference("http://example.com/ex1")),
            ALC.Concept.NewConceptName(new IriReference("http://example.com/ex2"))
                ));
    }
    
    [Fact]
    public void TestDisjunction()
    {
        var conceptString = "<http://example.com/ex1> or <http://example.com/ex2>";
        var parsedIri  = testString(conceptString);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewDisjunction(
                ALC.Concept.NewConceptName(new IriReference("http://example.com/ex1")),
                ALC.Concept.NewConceptName(new IriReference("http://example.com/ex2"))
            ));
    }
    [Fact]
    public void TestUniversal()
    {
        var conceptString = "<http://example.com/name> only <http://foaf.com/name>";
        var parsedIri  = testString(conceptString);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewUniversal(
                new IriReference("http://example.com/name"),
                ALC.Concept.NewConceptName(new IriReference("http://foaf.com/name"))
            ));
    }

    
    [Fact]
    public void TestExistential()
    {
        var conceptString = "<http://example.com/name> some <http://foaf.com/name>";
        var parsedIri  = testString(conceptString);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewExistential(
                new IriReference("http://example.com/name"),
                ALC.Concept.NewConceptName(new IriReference("http://foaf.com/name"))
            ));
    }

    [Fact]
    public void TestTripleDisjunction()
    {
        var conceptString = "<http://example.com/ex1> or <http://example.com/ex2> or <http://example.com/ex3>";
        var parsedIri  = testString(conceptString);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewDisjunction(
                ALC.Concept.NewDisjunction(
                    ALC.Concept.NewConceptName(new IriReference("http://example.com/ex1")),
                    ALC.Concept.NewConceptName(new IriReference("http://example.com/ex2")))
                ,ALC.Concept.NewConceptName(new IriReference("http://example.com/ex3"))
                
            ));
    }
    
    [Fact]
    public void TestTripleConjunction()
    {
        var conceptString = "<http://example.com/ex1> and <http://example.com/ex2> and <http://example.com/ex3>";
        var parsedIri  = testString(conceptString);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewConjunction(
                ALC.Concept.NewConjunction(
                ALC.Concept.NewConceptName(new IriReference("http://example.com/ex1")),
                ALC.Concept.NewConceptName(new IriReference("http://example.com/ex2")))
                ,ALC.Concept.NewConceptName(new IriReference("http://example.com/ex3"))
                
            ));
    }
    [Fact]
    public void TestNamedConcept()
    {
        var conceptString = "<http://example.com/ex1>";
        var parsedIri  = testString(conceptString);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewConceptName(new IriReference("http://example.com/ex1"))
            );
    }
}