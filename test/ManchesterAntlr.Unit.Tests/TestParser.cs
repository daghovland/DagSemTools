using Microsoft.FSharp.Collections;

namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using AlcTableau.ManchesterAntlr;
using FluentAssertions;
using AlcTableau;
using FluentAssertions;
using IriTools;

public class TestParser
{


    private ALC.OntologyDocument TestFile(string filename){
        using TextReader textReader = File.OpenText(filename);
        return TestReader(textReader);
    }

    private ALC.OntologyDocument TestReader(TextReader textReader, Dictionary<string, IriReference> prefixes){
        
        var input = new AntlrInputStream(textReader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        IParseTree tree = parser.ontologyDocument();
        var visitor = new ManchesterVisitor();
        return visitor.Visit(tree);
    }
    
    public ALC.OntologyDocument TestReader(TextReader textReader) =>
        TestReader(textReader, new Dictionary<string, IriReference>());
    public ALC.OntologyDocument TestString(string owl){
        using TextReader textReader = new StringReader(owl);
        return TestReader(textReader);
    }

    [Fact]
    public void TestSmallestOntology()
    {
        var parsed = TestString("Ontology:");
        parsed.Should().NotBeNull();
    }

    
    [Fact]
    public void TestOntologyWithIri()
    {
        var parsedOntology = TestString("Prefix: ex: <https://example.com/> Ontology: <https://example.com/ontology>");
        parsedOntology.Should().NotBeNull();

        var (prefixes, versionedOntology, KB) = parsedOntology.TryGetOntology();
        var ontologyIri = versionedOntology.TryGetOntologyIri();
        ontologyIri.Should().NotBeNull();
        ontologyIri.Should().Be(new IriReference("https://example.com/ontology"));
        
        var namedOntology = (ALC.ontologyVersion.NamedOntology)versionedOntology;
        namedOntology.OntologyIri.Should().Be(new IriReference("https://example.com/ontology"));
    }
    [Fact]
    public void TestOntologyWithVersonIri()
    {
        var parsedOntology = TestString("Prefix: ex: <https://example.com/> Ontology: <https://example.com/ontology> <https://example.com/ontology#1>");
        parsedOntology.Should().NotBeNull();

        var (prefixes, versionedOntology, KB) = parsedOntology.TryGetOntology();
        var ontologyIri = versionedOntology.TryGetOntologyIri();
        ontologyIri.Should().NotBeNull();
        ontologyIri.Should().Be(new IriReference("https://example.com/ontology"));
        
        
        var ontologyVersionIri = versionedOntology.TryGetOntologyVersionIri();
        ontologyVersionIri.Should().NotBeNull();
        ontologyVersionIri.Should().Be(new IriReference("https://example.com/ontology#1"));
    }
    
    [Fact]
    public void TestOntologyWithClass()
    {
        var parsedOntology = TestString("Prefix: ex: <https://example.com/> Ontology: <https://example.com/ontology> <https://example.com/ontology#1> Class: ex:Class");
        parsedOntology.Should().NotBeNull();

        var (prefixes, versionedOntology, KB) = parsedOntology.TryGetOntology();
        var ontologyIri = versionedOntology.TryGetOntologyIri();
        ontologyIri.Should().NotBeNull();
        ontologyIri.Should().Be(new IriReference("https://example.com/ontology"));
        
        
        var ontologyVersionIri = versionedOntology.TryGetOntologyVersionIri();
        ontologyVersionIri.Should().NotBeNull();
        ontologyVersionIri.Should().Be(new IriReference("https://example.com/ontology#1"));
    }
    
    
    [Fact]
    public void TestOntologyWithSubClass()
    {
        var parsedOntology = TestString("""
                                        Prefix: ex: <https://example.com/> 
                                        Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                        Class: ex:Class
                                        SubClassOf: ex:Class ex:SuperClass");
                                        """
                                        );
        parsedOntology.Should().NotBeNull();

        var (prefixes, versionedOntology, KB) = parsedOntology.TryGetOntology();
        var ontologyIri = versionedOntology.TryGetOntologyIri();
        ontologyIri.Should().NotBeNull();
        ontologyIri.Should().Be(new IriReference("https://example.com/ontology"));
        
        var ontologyVersionIri = versionedOntology.TryGetOntologyVersionIri();
        ontologyVersionIri.Should().NotBeNull();
        ontologyVersionIri.Should().Be(new IriReference("https://example.com/ontology#1"));
        
        var tboxAxioms = KB.Item1;
        var tboxAxiomsList = tboxAxioms.ToList();
        tboxAxiomsList.Should().HaveCount(1);
        tboxAxiomsList[0].Should().BeOfType<ALC.TBoxAxiom.Inclusion>();
        var inclusion = (ALC.TBoxAxiom.Inclusion)tboxAxiomsList[0];
        inclusion.Sub.Should().Be(ALC.Concept.NewConceptName(new IriReference("https://example.com/Class")));
        inclusion.Sup.Should().Be(ALC.Concept.NewConceptName(new IriReference("https://example.com/SuperClass")));
    }
}