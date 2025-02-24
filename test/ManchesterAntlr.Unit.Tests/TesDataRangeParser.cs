/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools;
using DagSemTools.Ingress;
using DagSemTools.OwlOntology;
using DagSemTools.Manchester.Parser;
using DagSemTools.Parser;
using Microsoft.FSharp.Collections;
using TestUtils;
using Xunit.Abstractions;


namespace DagSemTools.ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using DagSemTools.ManchesterAntlr;
using FluentAssertions;
using IriTools;

public class TestDataRangeParser
{

    public DataRange TestReader(TextReader text_reader, Dictionary<string, IriReference> prefixes, TextWriter errorOutput)
    {

        var input = new AntlrInputStream(text_reader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        var customErrorListener = new ParserErrorListener(errorOutput);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(customErrorListener);
        IParseTree tree = parser.dataRange();
        var visitor = new DataPrimaryVisitor(prefixes, customErrorListener);
        return visitor.Visit(tree);
    }

    private DataRange TestReader(TextReader text_reader, TextWriter errorOutput) =>
        TestReader(text_reader, new Dictionary<string, IriReference>(), errorOutput);

    private DataRange TestString(string owl, TextWriter errorOutput)
    {
        using TextReader text_reader = new StringReader(owl);
        return TestReader(text_reader, errorOutput);
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
        var parsedDataRange = TestString("integer", testOutputTextWriter);
        var expectedDataRange = DataRange.NewNamedDataRange(Iri.NewFullIri("https://www.w3.org/2001/XMLSchema#integer"));
        parsedDataRange.Should().BeEquivalentTo(expectedDataRange);
    }

    [Fact]
    public void TestRestrictedInt()
    {
        var parsedDataRange = TestString("integer[< 0]", testOutputTextWriter);
        var xsdInt = Iri.NewFullIri(Namespaces.XsdInt);
        var lt = Iri.NewFullIri(Namespaces.XsdMaxExclusive);
        var zero = GraphElement.NewGraphLiteral(RdfLiteral.NewIntegerLiteral(0));
        var expected =
            DataRange.NewDatatypeRestriction(xsdInt, ListModule.OfSeq([Tuple.Create(lt, zero)]));
        parsedDataRange.Should().BeEquivalentTo(expected);
    }


    [Fact]
    public void TestRestrictedString()
    {
        var parsedDataRange = TestString("string[length 5]", testOutputTextWriter);
        var xsdInt = Iri.NewFullIri(Namespaces.XsdInt);
        var length = Iri.NewFullIri(Namespaces.XsdLength);
        var five = GraphElement.NewGraphLiteral(RdfLiteral.NewIntegerLiteral(5));
        var expected =
            DataRange.NewDatatypeRestriction(xsdInt, ListModule.OfSeq([Tuple.Create(length, five)]));
        parsedDataRange.Should().BeEquivalentTo(expected);
    }
}