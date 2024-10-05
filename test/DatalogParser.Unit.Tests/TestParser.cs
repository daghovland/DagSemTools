using DagSemTools;
using DagSemTools.Datalog;
using DagSemTools.Rdf;
using FluentAssertions;
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

    public IEnumerable<Datalog.Rule> TestProgram(string datalog)
    {
        var datastore = new Datastore(1000);
        return DagSemTools.Datalog.Parser.Parser.ParseString(datalog, _outputWriter, datastore);

    }


    public IEnumerable<Datalog.Rule> TestProgramFile(FileInfo datalog)
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
    public void TestRealData()
    {
        var fInfo = File.ReadAllText("TestData/noaka_boundary.datalog");
        var ont = TestProgram(fInfo);
        Assert.NotNull(ont);
        Assert.Single(ont);
    }

}