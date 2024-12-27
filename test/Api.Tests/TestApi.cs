using DagSemTools.Api;
using DagSemTools.Rdf;
using DagSemTools.Ingress;
using IriTools;
using FluentAssertions;
using Xunit.Abstractions;
using RdfLiteral = DagSemTools.Api.RdfLiteral;

namespace Api.Tests;

public class TestApi(ITestOutputHelper output)
{
    TestUtils.TestOutputTextWriter outputWriter = new TestUtils.TestOutputTextWriter(output);

    [Fact]
    public void Test1()
    {
        var ontology = new FileInfo("TestData/example1.ttl");
        var ont = DagSemTools.Api.TurtleParser.Parse(ontology, outputWriter);

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
        var ont = DagSemTools.Api.TurtleParser.Parse(ontology, outputWriter);
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
            .Where(tr => tr.Object.Equals(new RdfLiteral("Eve")));
        eve.Should().HaveCount(1);
    }

    [Fact]
    public void TestDatalogReasoning()
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

    [Fact]
    public void TestA()
    {
        var ontology = new FileInfo("TestData/test2.ttl");
        var ont = DagSemTools.Api.TurtleParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetTriplesWithObject(
            new IriReference("http://example.com/data#property")).ToList();
        resultsData.Should().HaveCount(1);
        resultsData.First().Predicate.Should().Be(new IriReference(Namespaces.RdfType));

    }

    [Fact]
    public void TestDatalog2()
    {
        var ontology = new FileInfo("TestData/test2.ttl");
        var ont = DagSemTools.Api.TurtleParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetTriplesWithObject(
            new IriReference("http://example.com/data#property")).ToList();
        resultsData.Should().HaveCount(1);
        resultsData.First().Predicate.Should().Be(new IriReference(Namespaces.RdfType));

        var datalogFile = new FileInfo("TestData/test2.datalog");
        ont.LoadDatalog(datalogFile);

        resultsData = ont.GetTriplesWithObject(
            new IriReference("http://example.com/data#property")).ToList();
        resultsData.Should().HaveCount(3);

    }



    [Fact]
    public void TestDatalogStratified()
    {
        var ontology = new FileInfo("TestData/test_stratified.ttl");
        var ont = DagSemTools.Api.TurtleParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetTriplesWithObject(
            new IriReference("http://example.com/data#Type")).ToList();
        resultsData.Should().HaveCount(1);
        resultsData.First().Predicate.Should().Be(new IriReference(Namespaces.RdfType));

        resultsData = ont.GetTriplesWithObject(
            new IriReference("http://example.com/data#Type3")).ToList();
        resultsData.Should().HaveCount(0);

        var datalogFile = new FileInfo("TestData/test_stratified.datalog");
        ont.LoadDatalog(datalogFile);

        resultsData = ont.GetTriplesWithObject(
            new IriReference("http://example.com/data#Type3")).ToList();
        resultsData.Should().HaveCount(1);

    }
}