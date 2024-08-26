using AlcTableau;

namespace TurtleParser.Unit.Tests;

public class TestParser
{
    public ALC.OntologyDocument TestOntology(string ontology)
        => TurtleAntlr.Parser.ParseString(ontology);

    [Fact]
    public void Test1()
    {
        var ontology = File.ReadAllText("example1.ttl");
        var ont = TestOntology(ontology);
        Assert.NotNull(ont);
    }
}