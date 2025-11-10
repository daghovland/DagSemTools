using DagSemTools.Api;
using IriTools;
using Xunit.Abstractions;
using FluentAssertions;

namespace NugetTest;

using DagSemTools.Api;

public class TestApi(ITestOutputHelper output)
{
    TestOutputTextWriter outputWriter = new TestOutputTextWriter(output);

    [Fact]
    public void Test1()
    {
        var ontology = new FileInfo("TestData/example1.ttl");
        var ont = TurtleParser.Parse(ontology, outputWriter);

        Assert.NotNull(ont);
        var enemies = ont.GetTriplesWithPredicate(new IriReference("http://www.perceive.net/schemas/relationship/enemyOf"));
        enemies.Count().Should().Be(2, "There are two trples with enemies");
    }

    [Fact]
    public void TestSparql()
    {
        var ontology = new FileInfo("TestData/example1.ttl");
        var ont = TurtleParser.Parse(ontology, outputWriter);

        Assert.NotNull(ont);
        var enemies = ont.AnswerSelectQuery("SELECT * WHERE where{?hero <http://www.perceive.net/schemas/relationship/enemyOf> ?enemy.}").ToList();
        Assert.NotNull(enemies);
        foreach (var answerMap in enemies)
        {
            outputWriter.WriteLine();
            foreach (var answer in answerMap.Values)
                outputWriter.Write(answer);
        }
        enemies.Count().Should().Be(2, "There are two triples with enemies");
        
    }

    

    [Fact]
    public void TestDbPedia()
    {
        var ontology = new FileInfo("DbpediaTests/test2.ttl");
        var ont = TurtleParser.Parse(ontology, outputWriter);

        Assert.NotNull(ont);
        var subjects = ont.GetTriplesWithPredicate(new IriReference("http://purl.org/dc/terms/subject"));
        subjects.Count().Should().BeGreaterThan(200, "There are many triples with subjects");
    }



    [Fact]
    public void TestDbPediaOntology()
    {
        var ontology = new FileInfo("DbpediaTests/test1.ttl");
        var ont = TurtleParser.Parse(ontology, outputWriter);

        Assert.NotNull(ont);
        var labels = ont.GetTriplesWithSubjectPredicate(
            new IriReference("http://dbpedia.org/ontology/NaturalEvent"),
            new IriReference("http://www.w3.org/2000/01/rdf-schema#label"));
        labels.Count().Should().Be(7, "There are two labels on natural event");
    }
    
    
    [Fact]
    public void TestDatalog()
    {
        var ontology = new FileInfo("TestData/data.ttl");
        var ont = DagSemTools.Api.TurtleParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetTriplesWithPredicateObject(
            new IriReference("https://example.com/data#predicate"),
            new IriReference("https://example.com/data#object"));
        resultsData.Should().HaveCount(1);
        var resultsBefore = ont.GetTriplesWithPredicateObject(
            new IriReference("https://example.com/data#predicate"),
            new IriReference("https://example.com/data#object2"));
        resultsBefore.Should().BeEmpty();
        Assert.NotNull(ont);
        var datalogFile = new FileInfo("TestData/rules.datalog");
        ont.LoadDatalog(datalogFile);
        var resultsAfter = ont.GetTriplesWithPredicateObject(
            new IriReference("https://example.com/data#predicate"),
            new IriReference("https://example.com/data#object2"));
        resultsAfter.Should().HaveCount(1);
    }
}