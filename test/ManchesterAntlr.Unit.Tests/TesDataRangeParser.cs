using AlcTableau;
using AlcTableau.Parser;
using Microsoft.FSharp.Collections;
using TurtleParser.Unit.Tests;
using Xunit.Abstractions;


namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using AlcTableau.ManchesterAntlr;
using FluentAssertions;
using IriTools;

public class TestDataRangeParser
{

    public DataRange.Datarange testReader(TextReader text_reader, Dictionary<string, IriReference> prefixes, TextWriter errorOutput)
    {

        var input = new AntlrInputStream(text_reader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        var customErrorListener = new ParserErrorListener(errorOutput);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(customErrorListener);
        IParseTree tree = parser.dataRange();
        var visitor = new DataRangeVisitor(prefixes, customErrorListener);
        return visitor.Visit(tree);
    }

    public DataRange.Datarange testReader(TextReader text_reader, TextWriter errorOutput) =>
        testReader(text_reader, new Dictionary<string, IriReference>(), errorOutput);

    public DataRange.Datarange testString(string owl, TextWriter errorOutput)
    {
        using TextReader text_reader = new StringReader(owl);
        return testReader(text_reader, errorOutput);
    }

    private ITestOutputHelper output;
    private TestOutputTextWriter testOutputTextWriter;
    public TestDataRangeParser(ITestOutputHelper output)
    {
        this.output = output;
        this.testOutputTextWriter = new TestOutputTextWriter(output);
    }

    [Fact]
    public void TestDatatypeInt()
    {
        var parsedDataRange = testString("integer", testOutputTextWriter);
        var expectedDataRange = DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#integer");
        parsedDataRange.Should().BeEquivalentTo(expectedDataRange);
    }

    [Fact]
    public void TestRestrictedInt()
    {
        var parsedDataRange = testString("integer[< 0]", testOutputTextWriter);
        var xsd_int = DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#integer");
        var expected_facet = new Tuple<DataRange.facet, string>(DataRange.facet.LessThan, "0");
        var expexted =
            DataRange.Datarange.NewRestriction(xsd_int, new FSharpList<Tuple<DataRange.facet, string>>(expected_facet, FSharpList<Tuple<DataRange.facet, string>>.Empty));
        parsedDataRange.Should().BeEquivalentTo(expexted);
    }


    [Fact]
    public void TestRestrictedString()
    {
        var parsedDataRange = testString("string[length 5]", testOutputTextWriter);
        var xsd_int = DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#string");
        var expected_facet = new Tuple<DataRange.facet, string>(DataRange.facet.Length, "5");
        var expexted =
            DataRange.Datarange.NewRestriction(xsd_int, new FSharpList<Tuple<DataRange.facet, string>>(expected_facet, FSharpList<Tuple<DataRange.facet, string>>.Empty));
        parsedDataRange.Should().BeEquivalentTo(expexted);
    }
}