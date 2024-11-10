using DagSemTools;
using DagSemTools.Datalog;
using DagSemTools.Rdf;
using FluentAssertions;
using IriTools;
using TestUtils;
using Xunit.Abstractions;

namespace DatalogParser.Unit.Tests;

public class TestParser
{

    private ITestOutputHelper _output;
    private TextWriter _outputWriter;

    public TestParser(ITestOutputHelper output)
    {
        _output = output;
        _outputWriter = new TestOutputTextWriter(_output);
    }

    public IEnumerable<Rule> TestProgram(string datalog)
    {
        var datastore = new Datastore(1000);
        return DagSemTools.Datalog.Parser.Parser.ParseString(datalog, _outputWriter, datastore);

    }



    public IEnumerable<Rule> TestProgramFile(FileInfo datalog)
    {
        var datastore = new Datastore(1000);
        return DagSemTools.Datalog.Parser.Parser.ParseFile(datalog, _outputWriter, datastore);

    }


    [Fact]
    public void TestSingleRule()
    {
        var fInfo = File.ReadAllText("TestData/rule1.datalog");
        var ont = TestProgram(fInfo).ToList();
        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
    }


    [Fact]
    public void TestRuleWithAnd()
    {
        var fInfo = File.ReadAllText("TestData/ruleand.datalog");
        var ont = TestProgram(fInfo).ToList();
        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
        ont.First().Body.Count().Should().Be(2);
    }

    [Fact]
    public void TestTwoRules()
    {
        var fInfo = File.ReadAllText("TestData/tworules.datalog");
        var ont = TestProgram(fInfo).ToList();
        ont.Should().NotBeNull();
        ont.Should().HaveCount(2);
        ont.First().Body.Count().Should().Be(2);
    }


    [Fact]
    public void TestNot()
    {
        var fInfo = File.ReadAllText("TestData/rulenot.datalog");
        var ont = TestProgram(fInfo).ToList();
        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
        ont.First().Body.Count().Should().Be(2);
    }


    [Fact]
    public void TestTypeAtom()
    {
        var fInfo = File.ReadAllText("TestData/ruletypeatom.datalog");
        var ont = TestProgram(fInfo).ToList();
        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
        ont.First().Body.Count().Should().Be(2);
    }

    [Fact]
    public void TestPrefixes()
    {
        //Arrange
        var datastore = new Datastore(1000);
        var fInfo = File.ReadAllText("TestData/prefixes.datalog");

        //Act
        var ont = DagSemTools.Datalog.Parser.Parser.ParseString(fInfo, _outputWriter, datastore).ToList();

        //Assert
        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
        ont.First().Body.Count().Should().Be(1);
        var ruleAtom = ont.First().Body.First();
        ruleAtom.Should().NotBeNull();
        ruleAtom.IsPositiveTriple.Should().BeTrue();
        var ruleTriplePattern = ((RuleAtom.PositiveTriple)ruleAtom).Item;
        ruleTriplePattern.Subject.Should().Be(ResourceOrVariable.NewVariable("?s"));

        var predicateResource = ResourceOrVariable
            .NewResource(datastore.AddResource(Ingress.Resource
                .NewIri(new IriReference("https://example.com/data#predicate2"))));
        ruleTriplePattern.Predicate.Should().Be(predicateResource);

        var objectResource = ResourceOrVariable
            .NewResource(datastore.AddResource(Ingress.Resource
                .NewIri(new IriReference("https://example.com/data3#obj"))));
        ruleTriplePattern.Object.Should().Be(objectResource);

        ruleAtom.Should().Be(RuleAtom.NewPositiveTriple(new TriplePattern(
            ResourceOrVariable.NewVariable("?s"),
            predicateResource,
            objectResource)));
    }

    [Fact]
    public void TestRuleWithAllVariables()
    {
        var datastore = new Datastore(1000);
        var fInfo = File.ReadAllText("TestData/properties.datalog");
        var ont = DagSemTools.Datalog.Parser.Parser.ParseString(fInfo, _outputWriter, datastore).ToList();

        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
        var parsedDatalogRule = ont.First();
        parsedDatalogRule.Body.Count().Should().Be(2);
        parsedDatalogRule.Body.First().Should().Be(RuleAtom.NewPositiveTriple(new TriplePattern(
            ResourceOrVariable.NewVariable("?x"),
            ResourceOrVariable.NewVariable("?p"),
            ResourceOrVariable.NewVariable("?y"))));
        var rdfTypeResource = ResourceOrVariable
            .NewResource(datastore.AddResource(Ingress.Resource
                .NewIri(new IriReference(Namespaces.RdfType))));
        TriplePattern expectedHead = new TriplePattern(
            ResourceOrVariable.NewVariable("?x"),
            rdfTypeResource,
            ResourceOrVariable.NewVariable("?c"));
        expectedHead.Should().NotBeNull();
        parsedDatalogRule.Head.Should().NotBeNull();
        parsedDatalogRule.Head.Subject.Should().Be(expectedHead.Subject);
        parsedDatalogRule.Head.Predicate.Should().Be(expectedHead.Predicate);
        parsedDatalogRule.Head.Object.Should().Be(expectedHead.Object);
        parsedDatalogRule.Head.Should().Be(expectedHead);

    }

    [Fact]
    public void TestTypeAtom2()
    {
        var fInfo = File.ReadAllText("TestData/typeatom2.datalog");
        var datastore = new Datastore(1000);
        var ont = DagSemTools.Datalog.Parser.Parser.ParseString(fInfo, _outputWriter, datastore).ToList();

        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
        ont.First().Body.Count().Should().Be(1);
        ont.First().Head.Should().Be(new TriplePattern(
            ResourceOrVariable.NewVariable("?new_node"),
            ResourceOrVariable
                .NewResource(datastore.GetResourceId(Ingress.Resource
                    .NewIri(new IriReference("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")))),
            ResourceOrVariable
                .NewResource(datastore.GetResourceId(Ingress.Resource
                    .NewIri(new IriReference("https://example.com/data#type"))))));

        ont.First().Body.First().Should().Be(RuleAtom.NewPositiveTriple(new TriplePattern(
            ResourceOrVariable.NewVariable("?node"),
            ResourceOrVariable
                .NewResource(datastore.GetResourceId(Ingress.Resource
                    .NewIri(new IriReference("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")))),
            ResourceOrVariable
                .NewResource(datastore.GetResourceId(Ingress.Resource
                    .NewIri(new IriReference("https://example.com/data#type")))))));
    }



}