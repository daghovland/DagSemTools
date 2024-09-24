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
        var enemies = ont.GetTriplesWithPredicate(new IriReference("<http://www.perceive.net/schemas/relationship/enemyOf>"));
        
    }
    
    
    [Fact]
    public void TestDbPedia()
    {
        var ontology = new FileInfo("TestData/test2.ttl");
        var ont = AlcTableau.Api.TurtleParser.Parse(ontology, outputWriter);

        Assert.NotNull(ont);
    }
    
    
    
    [Fact]
    public void TestDbPediaOntology()
    {
        var ontology = new FileInfo("TestData/ontology--DEV_type=parsed_sorted.nt");
        var ont = AlcTableau.Api.TurtleParser.Parse(ontology, outputWriter);

        Assert.NotNull(ont);
    }
}