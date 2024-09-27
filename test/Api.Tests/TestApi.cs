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

    [Fact]
    public void TestAbbreviatedBlankNode()
    {
        var ontology = new FileInfo("TestData/abbreviated_blank_nodes.ttl");
        var ont = AlcTableau.Api.TurtleParser.Parse(ontology, outputWriter);
        Assert.NotNull(ont);


        var knows = ont.GetTriplesWithPredicate(new IriReference("http://xmlns.com/foaf/0.1/knows")).ToList();
        knows.Should().HaveCount(2);


        var name = ont.GetTriplesWithPredicate(new IriReference("http://xmlns.com/foaf/0.1/name")).ToList();
        name.Should().HaveCount(3);
        var isKnown = knows.First().Object;
        var bobHasName = name.Skip(1).First().Subject;
        isKnown.Should().Be(bobHasName);

        var mbox = ont.GetTriplesWithPredicate(new IriReference("http://xmlns.com/foaf/0.1/mbox"));

        mbox.Should().HaveCount(1);

        var eve = ont.GetTriplesWithPredicate(new IriReference("http://xmlns.com/foaf/0.1/name"))
            .Where(tr => tr.Object.Equals(new LiteralResource("Eve")));
        eve.Should().HaveCount(1);
    }
}