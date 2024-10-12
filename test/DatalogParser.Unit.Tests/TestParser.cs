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
        var fInfo = File.ReadAllText("TestData/prefixes.datalog");
        var ont = TestProgram(fInfo).ToList();
        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
        ont.First().Body.Count().Should().Be(2);
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