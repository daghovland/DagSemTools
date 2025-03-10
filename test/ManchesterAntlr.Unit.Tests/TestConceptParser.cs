/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.OwlOntology;
using DagSemTools.Parser;
using TestUtils;
using Xunit.Abstractions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using FluentAssertions;
using IriTools;
using Microsoft.FSharp.Collections;
using Serilog;
using Serilog.Sinks.InMemory;

namespace DagSemTools.Manchester.Parser.Unit.Tests;

public class TestConceptParser
{
    private static InMemorySink _inMemorySink = new InMemorySink();

    private ILogger _logger =
        new LoggerConfiguration()
            .WriteTo.Sink(_inMemorySink)
            .WriteTo.Console()
            .CreateLogger();

    private ClassExpression TestFile(string filename, TextWriter errorOutput)
    {
        using TextReader textReader = File.OpenText(filename);
        return TestReader(textReader, errorOutput);
    }

    private ClassExpression TestReader(TextReader textReader, Dictionary<string, IriReference> prefixes, TextWriter errorOutput)
    {

        var input = new AntlrInputStream(textReader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        var customErrorListener = new ParserErrorListener(errorOutput);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(customErrorListener);

        IParseTree tree = parser.description();
        var visitor = new ConceptVisitor(prefixes, customErrorListener);
        return visitor.Visit(tree);
    }

    public ClassExpression TestReader(TextReader textReader, TextWriter errorOutput) =>
        TestReader(textReader, new Dictionary<string, IriReference>(), errorOutput);
    public ClassExpression TestString(string owl, TextWriter errorOutput)
    {
        using TextReader textReader = new StringReader(owl);
        return TestReader(textReader, errorOutput);
    }

    public ClassExpression TestString(string owl, Dictionary<string, IriReference> prefixes, TextWriter errorOutput)
    {
        using TextReader textReader = new StringReader(owl);
        return TestReader(textReader, prefixes, errorOutput);
    }

    private ITestOutputHelper _outputHelper;
    private TestOutputTextWriter _testOutputTextWriter;
    public TestConceptParser(ITestOutputHelper output)
    {
        _outputHelper = output;
        _testOutputTextWriter = new TestOutputTextWriter(output);
    }



    [Fact]
    public void TestConjunction()
    {
        var conceptString = "<http://example.com/ex1> and <http://example.com/ex2>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ClassExpression.NewObjectIntersectionOf(
                ListModule.OfSeq([ClassExpression.NewClassName(Iri.NewFullIri( new IriReference("http://example.com/ex1"))),
            ClassExpression.NewClassName(Iri.NewFullIri( new IriReference("http://example.com/ex2")))])
                ));
    }


    [Fact]
    public void TestParenConjunction()
    {
        var conceptString = "(<http://example.com/ex1>) and (<http://example.com/ex2>)";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ClassExpression.NewObjectIntersectionOf(
                ListModule.OfSeq([ ClassExpression.NewClassName(Iri.NewFullIri( new IriReference("http://example.com/ex1"))),
                ClassExpression.NewClassName(Iri.NewFullIri( new IriReference("http://example.com/ex2")))])
            ));
    }
    [Fact]
    public void TestDisjunction()
    {
        var conceptString = "<http://example.com/ex1> or <http://example.com/ex2>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ClassExpression.NewObjectUnionOf(
                ListModule.OfSeq([ ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://example.com/ex1"))),
                ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://example.com/ex2")))])
            ));
    }

    [Fact]
    public void TestNegation()
    {
        var conceptString = "not <http://example.com/ex1>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ClassExpression.NewObjectComplementOf(
                ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://example.com/ex1")))
            ));
    }

    [Fact]
    public void TestUniversal()
    {
        var conceptString = "<http://example.com/name> only <http://foaf.com/name>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ClassExpression.NewObjectAllValuesFrom(
                ObjectPropertyExpression.NewNamedObjectProperty(Iri.NewFullIri(new IriReference("http://example.com/name"))),
                ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://foaf.com/name")))
            ));
    }


    [Fact]
    public void TestExistential()
    {
        var conceptString = "<http://example.com/name> some <http://foaf.com/name>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ClassExpression.NewObjectSomeValuesFrom(
                ObjectPropertyExpression.NewNamedObjectProperty(Iri.NewFullIri(new IriReference("http://example.com/name"))),
                ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://foaf.com/name"))
            )));
    }


    [Fact]
    public void TestParenExistential()
    {
        var conceptString = "(<http://example.com/name> some <http://foaf.com/name>)";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ClassExpression.NewObjectSomeValuesFrom(
                ObjectPropertyExpression.NewNamedObjectProperty(Iri.NewFullIri(new IriReference("http://example.com/name"))),
                ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://foaf.com/name"))
            )));
    }


    [Fact]
    public void TestPrecedenceOfSomeAnd()
    {
        // Arrange
        var prefixes = new Dictionary<string, IriReference>()
        {
            { "", "https://example.com/" }
        };
        var conceptString = "p some a and p only b";
        var parenthesizedString = "(p some a) and (p only b)";
        var expected = ClassExpression.NewObjectIntersectionOf(
                ListModule.OfSeq([ 
                    ClassExpression.NewObjectSomeValuesFrom(
                        ObjectPropertyExpression.NewNamedObjectProperty(Iri.NewFullIri(new IriReference("https://example.com/p"))),
                        ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/a")))
                        ),
                    ClassExpression.NewObjectAllValuesFrom(
                        ObjectPropertyExpression.NewNamedObjectProperty(Iri.NewFullIri(new IriReference("https://example.com/p"))),
                        ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/b")))
                    )]));

        //Act
        var parsedIri = TestString(conceptString, prefixes, _testOutputTextWriter);
        var parsedParenthesized = TestString(parenthesizedString, prefixes, _testOutputTextWriter);

        // Assert
        parsedIri.Should().BeEquivalentTo(expected);
        parsedParenthesized.Should().BeEquivalentTo(expected);
    }


    [Fact]
    public void TestTripleDisjunction()
    {
        var conceptString = "<http://example.com/ex1> or <http://example.com/ex2> or <http://example.com/ex3>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ClassExpression.NewObjectUnionOf(
                        ListModule.OfSeq([
                            (
                                ClassExpression.NewObjectUnionOf(
                                    ListModule.OfSeq([
                                        ClassExpression.NewClassName(
                                            Iri.NewFullIri(new IriReference("http://example.com/ex1"))),
                                        ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://example.com/ex2")))
                                    ]))),
                                ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://example.com/ex3")))
                        ])));
    }

    [Fact]
    public void TestTripleConjunction()
    {
        var conceptString = "<http://example.com/ex1> and <http://example.com/ex2> and <http://example.com/ex3>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
    ClassExpression.NewObjectIntersectionOf(
              ListModule.OfSeq([ 
                ClassExpression.NewObjectIntersectionOf(
                  ListModule.OfSeq([ 
                    ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://example.com/ex1"))),
                    ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://example.com/ex2")))])),
                 ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://example.com/ex3")))])));
    }
    [Fact]
    public void TestNamedConcept()
    {
        var conceptString = "<http://example.com/ex1>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://example.com/ex1")))
            );
    }



    [Fact]
    public void TestParenNamedConcept()
    {
        var conceptString = "(<http://example.com/ex1>)";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://example.com/ex1")))
        );
    }


    [Fact(Skip = "Not implemented yet, See Issue https://github.com/daghovland/AlcTableau/issues/2")]
    public void TestConceptRestriction()
    {
        var conceptString = "hasFirstName exactly 1";
        var parsedConcept = TestString(conceptString, _testOutputTextWriter);
        parsedConcept.Should().NotBeNull();
    }

    [Fact(Skip = "Not implemented yet, See Issue https://github.com/daghovland/AlcTableau/issues/2")]
    public void TestDatapropertyRestriction()
    {
        var conceptString = "hasFirstName only string[minLength 1]";
        var parsedConcept = TestString(conceptString, _testOutputTextWriter);
        parsedConcept.Should().NotBeNull();
    }

    [Fact(Skip = "Not implemented yet, See Issue https://github.com/daghovland/AlcTableau/issues/2")]
    public void TestComplexConcept()
    {
        var conceptString = "owl:Thing that hasFirstName exactly 1 and hasFirstName only string[minLength 1]";
        var parsedConcept = TestString(conceptString, _testOutputTextWriter);
        parsedConcept.Should().NotBeNull();
        // var expected = ClassExpression.NewObjectIntersectionOf(
        //        ListModule.OfSeq([ (
        //     ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://www.w3.org/2002/07/owl#Thing")),
        //     ClassExpression.NewObjectIntersectionOf(
        //        ListModule.OfSeq([ (
        //         ClassExpression.NewCardinality(
        //             new IriReference("http://example.com/hasFirstName"),
        //             1,
        //             ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("http://example.com/hasFirstName"))
        //         ),
        //         ClassExpression.NewObjectAllValuesFrom(ObjectPropertyExpression.NewNamedObjectProperty(
        //             new IriReference("http://example.com/hasFirstName"),
        //              DataRange.Datarange.NewRestriction(
        //                 DataRange.Datarange.NewDatatype(new IriReference("http://www.w3.org/2001/XMLSchema#string")),
        //                 new FSharpList<Tuple<DataRange.facet, string>>(
        //                     new Tuple<DataRange.facet, string>(DataRange.facet.MinLength, "1"),
        //                     FSharpList<Tuple<DataRange.facet, string>>.Empty
        //                 )
        //             )
        //         )
        //     )
        // );
    }
}