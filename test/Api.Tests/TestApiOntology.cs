using Xunit.Abstractions;

namespace Api.Tests;
using DagSemTools.Api;

public class TestApiOntology(ITestOutputHelper output)
{
    TestUtils.TestOutputTextWriter outputWriter = new TestUtils.TestOutputTextWriter(output);

    [Fact]
    public void LoadImfOntologyWorks()
    {
        var ontologyFileInfo = new FileInfo("TestData/imf.owl");
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, outputWriter);
        var ont = OwlOntology.Create(rdf);

    }
}