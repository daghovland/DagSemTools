using AlcTableau;

namespace TurtleParser.Unit.Tests;

public class TestParser
{
    public ALC.OntologyDocument TestOntology(string ontology)
        => TurtleAntlr.Parser.ParseString(ontology);

    [Fact]
    public void TestSingleTriple()
    {
        var ont = TestOntology("<http://example.org/subject> <http://example.org/predicate> <http://example.org/object> .");
        Assert.NotNull(ont);
    }

    [Fact]
    public void TestSpecExample21()
    {
        var ont = TestOntology(
            "<http://example.org/#spiderman>\n  <http://www.perceive.net/schemas/relationship/enemyOf>\n    <http://example.org/#green-goblin> .");
        Assert.NotNull(ont);
    }
    
    
    [Fact]
    public void TestSpecExamplePredicateList()
    {
        var ont = TestOntology(
            "<http://example.org/#spiderman> <http://www.perceive.net/schemas/relationship/enemyOf> <http://example.org/#green-goblin> ;\n    <http://xmlns.com/foaf/0.1/name> \"Spiderman\" .\n    ");
        Assert.NotNull(ont);
    }
    
    [Fact]
    public void TestAllIriWritings()
    {
        var ont = TestOntology(
            "\n# A triple with all resolved IRIs\n<http://one.example/subject1> <http://one.example/predicate1> <http://one.example/object1> .\n\n@base <http://one.example/> .\n<subject2> <predicate2> <object2> .     # relative IRI references, e.g., http://one.example/subject2\n\nBASE <http://one.example/>\n<subject2> <predicate2> <object2> .     # relative IRI references, e.g., http://one.example/subject2\n\n@prefix p: <http://two.example/> .\np:subject3 p:predicate3 p:object3 .     # prefixed name, e.g., http://two.example/subject3\n\nPREFIX p: <http://two.example/>\np:subject3 p:predicate3 p:object3 .     # prefixed name, e.g., http://two.example/subject3\n\n@prefix p: <path/> .                    # prefix p: now stands for http://one.example/path/\np:subject4 p:predicate4 p:object4 .     # prefixed name, e.g., http://one.example/path/subject4\n\nPrEfIx : <http://another.example/>       # empty prefix\n:subject5 :predicate5 :object5 .        # prefixed name, e.g., http://another.example/subject5\n\n:subject6 a :subject7 .                 # same as :subject6 <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> :subject7 .\n\n<http://伝言.example/?user=أ&channel=R%26D> a :subject8 . # a multi-script subject IRI .");
        Assert.NotNull(ont);
    }
    
        
    [Fact]
    public void TestNumberLiterals()
    {
        var ont = TestOntology(
            "\nPREFIX : <http://example.org/elements/>\n<http://en.wikipedia.org/wiki/Helium>\n    :atomicNumber 2 ;               # xsd:integer\n    :atomicMass 4.002602 ;          # xsd:decimal\n    :specificGravity 1.663E-4 .     # xsd:double");
        Assert.NotNull(ont);
    }
    
    [Fact]
    public void Test1()
    {
        var ontology = File.ReadAllText("example1.ttl");
        var ont = TestOntology(ontology);
        Assert.NotNull(ont);
    }
}