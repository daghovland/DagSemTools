using AlcTableau.Api;
using IriTools;
using FluentAssertions;
using Xunit.Abstractions;

namespace Api.Tests;

public class TestApi(ITestOutputHelper output)
{
    TestUtils.TestOutputTextWriter outputWriter = new TestUtils.TestOutputTextWriter(output);

    [Fact]
    public void Test1()
    {
        var ontology = new FileInfo("TestData/example1.ttl");
        var ont = AlcTableau.Api.TurtleParser.Parse(ontology, outputWriter);

        Assert.NotNull(ont);
        var labels = ont.GetTriplesWithSubjectPredicate(
            new IriReference("http://dbpedia.org/datatype/FuelEfficiency"),
            new IriReference("http://www.w3.org/2000/01/rdf-schema#label"));
        labels.Count().Should().Be(1, "There is one label on fuel efficiency");
    }
}