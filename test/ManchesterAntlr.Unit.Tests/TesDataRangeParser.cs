using AlcTableau;
using Microsoft.FSharp.Collections;


namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using AlcTableau.ManchesterAntlr;
using FluentAssertions;
using IriTools;

public class TestDataRangeParser
{


    public DataRange.Datarange testFile(string filename)
    {
        using TextReader text_reader = File.OpenText(filename);

        return testReader(text_reader);

    }

    public DataRange.Datarange testReader(TextReader text_reader, Dictionary<string, IriReference> prefixes)
    {

        var input = new AntlrInputStream(text_reader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        IParseTree tree = parser.dataRange();
        var visitor = new DataRangeVisitor(prefixes);
        return visitor.Visit(tree);


    }

    public DataRange.Datarange testReader(TextReader text_reader) =>
        testReader(text_reader, new Dictionary<string, IriReference>());

    public DataRange.Datarange testString(string owl)
    {
        using TextReader text_reader = new StringReader(owl);
        return testReader(text_reader);
    }

    [Fact]
    public void TestDatatypeInt()
    {
        var parsedDataRange = testString("integer");
        var expectedDataRange = DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#integer");
        parsedDataRange.Should().BeEquivalentTo(expectedDataRange);
    }

    [Fact]
    public void TestRestrictedInt()
    {
        var parsedDataRange = testString("integer[< 0]");
        var xsd_int = DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#integer");
        var expected_facet = new Tuple<DataRange.facet, string>(DataRange.facet.LessThan, "0");
        var expexted =
            DataRange.Datarange.NewRestriction(xsd_int, new FSharpList<Tuple<DataRange.facet, string>>(expected_facet, FSharpList<Tuple<DataRange.facet, string>>.Empty));
        parsedDataRange.Should().BeEquivalentTo(expexted);
    }


    [Fact]
    public void TestRestrictedString()
    {
        var parsedDataRange = testString("string[length 5]");
        var xsd_int = DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#string");
        var expected_facet = new Tuple<DataRange.facet, string>(DataRange.facet.Length, "5");
        var expexted =
            DataRange.Datarange.NewRestriction(xsd_int, new FSharpList<Tuple<DataRange.facet, string>>(expected_facet, FSharpList<Tuple<DataRange.facet, string>>.Empty));
        parsedDataRange.Should().BeEquivalentTo(expexted);
    }
}