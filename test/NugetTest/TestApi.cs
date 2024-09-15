using Xunit.Abstractions;

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
    }
}