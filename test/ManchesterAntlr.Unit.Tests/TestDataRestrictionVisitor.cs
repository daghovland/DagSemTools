using AlcTableau;
using AlcTableau.ManchesterAntlr;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using FluentAssertions;
using IriTools;

namespace ManchesterAntlr.Unit.Tests;

public class TestDataRestrictionVisitor
{

    public Tuple<DataRange.facet, string> testReader(TextReader text_reader, Dictionary<string, IriReference> prefixes)
    {

        var input = new AntlrInputStream(text_reader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        IParseTree tree = parser.datatype_restriction();
        var visitor = new DatatypeRestrictionVisitor();
        return visitor.Visit(tree);


    }

    public Tuple<DataRange.facet, string> testReader(TextReader text_reader) =>
        testReader(text_reader, new Dictionary<string, IriReference>());

    public Tuple<DataRange.facet, string> testString(string owl)
    {
        using TextReader text_reader = new StringReader(owl);
        return testReader(text_reader);
    }


    [Fact]
    public void TestLessThan0()
    {
        var parsedDataRange = testString("< 0");
        var expexted = new Tuple<DataRange.facet, string>(DataRange.facet.LessThan, "0");
        parsedDataRange.Should().BeEquivalentTo(expexted);
    }


    [Fact]
    public void TestGreaterThan9()
    {
        var parsedDataRange = testString("> 9");
        var expexted = new Tuple<DataRange.facet, string>(DataRange.facet.GreaterThan, "9");
        parsedDataRange.Should().BeEquivalentTo(expexted);
    }


    [Fact]
    public void TestLength()
    {
        var parsedDataRange = testString("length 2");
        var expexted = new Tuple<DataRange.facet, string>(DataRange.facet.Length, "2");
        parsedDataRange.Should().BeEquivalentTo(expexted);
    }
}