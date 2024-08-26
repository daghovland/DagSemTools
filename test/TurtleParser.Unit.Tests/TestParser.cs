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
    public void Test1()
    {
        var ontology = File.ReadAllText("example1.ttl");
        var ont = TestOntology(ontology);
        Assert.NotNull(ont);
    }
}