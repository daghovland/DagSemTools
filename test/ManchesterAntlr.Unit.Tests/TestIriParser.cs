namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using AlcTableau.ManchesterAntlr;
using FluentAssertions;
using IriTools;

public class TestIriParser
{


    public IriReference testFile(string filename, IAntlrErrorListener<IToken>? errorListener)
    {
        using TextReader textReader = File.OpenText(filename);

        return TestReader(textReader, errorListener);

    }

    public IriReference TestReader(TextReader textReader, Dictionary<string, IriReference> prefixes, IAntlrErrorListener<IToken>? errorListener = null)
    {

        var input = new AntlrInputStream(textReader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        IParseTree tree = parser.rdfiri();
        IAntlrErrorListener<IToken> customErrorListener = new ConsoleErrorListener<IToken>();
        if (errorListener != null)
        {
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);
            customErrorListener = errorListener;
        }

        var visitor = new IriGrammarVisitor(prefixes, customErrorListener);
        return visitor.Visit(tree);


    }

    public IriReference TestReader(TextReader textReader, IAntlrErrorListener<IToken>? errorListener = null) =>
        TestReader(textReader, new Dictionary<string, IriReference>(), errorListener);

    public IriReference testString(string owl, IAntlrErrorListener<IToken>? errorListener = null)
    {
        using TextReader textReader = new StringReader(owl);
        return TestReader(textReader, errorListener);
    }
    public IriReference testStringWithEmptyProefix(string owl)
    {
        var prefixes = new Dictionary<string, IriReference>()
        {
            { "", new IriReference("https://example.com/vocab/ont#") }
        };
        using TextReader text_reader = new StringReader(owl);
        return TestReader(text_reader, prefixes);
    }


    [Theory]
    [InlineData("http://example.com/ex")]
    [InlineData("http://example.com/ex#abc")]
    [InlineData("http://ex.com/owl2/families.owl")]
    [InlineData("http://example.com/owl/families-v1")]
    [InlineData("http://ex.com/owl2/families?owl")]
    public void TestFullIri(string iri)
    {
        var testIri = new IriReference(iri);
        var parsedIri = testString($"<{iri}>");
        parsedIri.Should().BeEquivalentTo(testIri);

    }



    [Theory]
    [InlineData("rdf:type")]
    [InlineData("xsd:int")]
    public void TestPrefixedIri(string iri)
    {
        var parsedIri = testString(iri);
        parsedIri.Should().NotBeNull();

    }

    [Fact]
    public void TestCustomPrefixedIri()
    {
        var prefix = new IriReference("https://example.com/vocab/ont#");
        var prefixes = new Dictionary<string, IriReference>()
        {
            { "ex", prefix }
        };
        using TextReader textReader = new StringReader("ex:className");
        var parsedIri = TestReader(textReader, prefixes);
        parsedIri.Should().BeEquivalentTo(new IriReference($"{prefix}className"));
    }

    [Fact]
    public void TestDefaultPrefixedIri()
    {
        var prefix = new IriReference("https://example.com/vocab/ont#");
        var prefixes = new Dictionary<string, IriReference>()
        {
            { "", prefix }
        };
        using TextReader textReader = new StringReader(":className");
        var parsedIri = TestReader(textReader, prefixes);
        parsedIri.Should().BeEquivalentTo(new IriReference($"{prefix}className"));
        using TextReader textReader2 = new StringReader("className");
        var parsedIri2 = TestReader(textReader2, prefixes);
        parsedIri2.Should().BeEquivalentTo(new IriReference($"{prefix}className"));
    }

    [Fact]
    public void TestLocalNamesThatAreAlsoExponents()
    {
        var parsedIri = testStringWithEmptyProefix("E1");
        parsedIri.Should().BeEquivalentTo(new IriReference("https://example.com/vocab/ont#E1"));
    }


    [Fact]
    public void TestLocalNamesThatAreAlsoIntegers()
    {
        var parsedIri = testStringWithEmptyProefix("1");
        parsedIri.Should().BeEquivalentTo(new IriReference("https://example.com/vocab/ont#1"));
    }
}