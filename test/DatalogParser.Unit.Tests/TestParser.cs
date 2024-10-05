using DagSemTools;
using DagSemTools.Datalog;
using DagSemTools.Rdf;
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
    public void TestSingleTriple()
    {
        var fInfo = File.ReadAllText("TestData/noaka_boundary.datalog");
        var ont = TestProgram(fInfo);
        Assert.NotNull(ont);
        Assert.Single(ont);
    }

}