namespace NugetTest;

using AlcTableau.Api;

public class TestApi
{

    [Fact]
    public void Test1()
    {
        var ontology = new FileInfo("TestData/example1.ttl");
        var ont = AlcTableau.Api.TurtleParser.Parse(ontology);

        Assert.NotNull(ont);
    }
}