/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.Parser;
using TestUtils;
using Xunit.Abstractions;

namespace DagSemTools.Manchester.Parser.Unit.Tests;

using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using DagSemTools.ManchesterAntlr;
using FluentAssertions;
using IriTools;

public class TestIriParser
{


    public IriReference testFile(string filename, TextWriter errorListener)
    {
        using TextReader textReader = File.OpenText(filename);

        return TestReader(textReader, errorListener);

    }

    public IriReference TestReader(TextReader textReader, Dictionary<string, IriReference> prefixes, TextWriter errorOutput)
    {

        var input = new AntlrInputStream(textReader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        var customErrorListener = new ParserErrorListener(errorOutput);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(customErrorListener);
        IParseTree tree = parser.rdfiri();
        var visitor = new IriGrammarVisitor(prefixes, customErrorListener);
        return visitor.Visit(tree);


    }

    public IriReference TestReader(TextReader textReader, TextWriter errorOutput) =>
        TestReader(textReader, new Dictionary<string, IriReference>(), errorOutput);

    public IriReference testString(string owl, TextWriter errorListener)
    {
        using TextReader textReader = new StringReader(owl);
        return TestReader(textReader, errorListener);
    }
    public IriReference testStringWithEmptyProefix(string owl, TextWriter errorListener)
    {
        var prefixes = new Dictionary<string, IriReference>()
        {
            { "", new IriReference("https://example.com/vocab/ont#") }
        };
        using TextReader text_reader = new StringReader(owl);
        return TestReader(text_reader, prefixes, errorListener);
    }

    private ITestOutputHelper output;
    private TestOutputTextWriter testOutputTextWriter;
    public TestIriParser(ITestOutputHelper output)
    {
        this.output = output;
        this.testOutputTextWriter = new TestOutputTextWriter(output);
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
        var parsedIri = testString($"<{iri}>", testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(testIri);

    }



    [Theory]
    [InlineData("rdf:type")]
    [InlineData("xsd:int")]
    public void TestPrefixedIri(string iri)
    {
        var parsedIri = testString(iri, testOutputTextWriter);
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
        var parsedIri = TestReader(textReader, prefixes, testOutputTextWriter);
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
        var parsedIri = TestReader(textReader, prefixes, testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(new IriReference($"{prefix}className"));
        using TextReader textReader2 = new StringReader("className");
        var parsedIri2 = TestReader(textReader2, prefixes, testOutputTextWriter);
        parsedIri2.Should().BeEquivalentTo(new IriReference($"{prefix}className"));
    }

    [Fact]
    public void TestLocalNamesThatAreAlsoExponents()
    {
        var parsedIri = testStringWithEmptyProefix("E1", testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(new IriReference("https://example.com/vocab/ont#E1"));
    }


    [Fact]
    public void TestLocalNamesThatAreAlsoIntegers()
    {
        var parsedIri = testStringWithEmptyProefix("1", testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(new IriReference("https://example.com/vocab/ont#1"));
    }
}