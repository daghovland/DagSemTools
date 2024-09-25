using IriTools;
using Xunit.Abstractions;
using FluentAssertions;

namespace NugetTest;

using AlcTableau.Api;

public class TestApi(ITestOutputHelper output)
{
    TestOutputTextWriter outputWriter = new TestOutputTextWriter(output);

    [Fact]
    public void Test1()
    {
        var ontology = new FileInfo("TestData/example1.ttl");
        var ont = AlcTableau.Api.TurtleParser.Parse(ontology, outputWriter);

        Assert.NotNull(ont);
        var enemies = ont.GetTriplesWithPredicate(new IriReference("http://www.perceive.net/schemas/relationship/enemyOf"));
        enemies.Count().Should().Be(2, "There are two trples with enemies");



    }


    [Fact]
    public void TestDbPedia()
    {
        var ontology = new FileInfo("DbpediaTests/test2.ttl");
        var ont = AlcTableau.Api.TurtleParser.Parse(ontology, outputWriter);

        Assert.NotNull(ont);
        var subjects = ont.GetTriplesWithPredicate(new IriReference("http://purl.org/dc/terms/subject"));
        subjects.Count().Should().BeGreaterThan(200, "There are many triples with subjects");
    }



    [Fact]
    public void TestDbPediaOntology()
    {
        var ontology = new FileInfo("DbpediaTests/test1.ttl");
        var ont = AlcTableau.Api.TurtleParser.Parse(ontology, outputWriter);

        Assert.NotNull(ont);
        var labels = ont.GetTriplesWithSubjectPredicate(
            new IriReference("http://dbpedia.org/ontology/NaturalEvent"),
            new IriReference("http://www.w3.org/2000/01/rdf-schema#label"));
        labels.Count().Should().Be(7, "There are two labels on natural event");
    }
}