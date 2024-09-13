using AlcTableau.Parser;
using Microsoft.FSharp.Collections;
using TurtleParser.Unit.Tests;
using Xunit.Abstractions;

namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using AlcTableau.ManchesterAntlr;
using AlcTableau;
using FluentAssertions;
using IriTools;

public class TestConceptParser
{
    private ALC.Concept TestFile(string filename, TextWriter errorOutput)
    {
        using TextReader textReader = File.OpenText(filename);
        return TestReader(textReader, errorOutput);
    }

    private ALC.Concept TestReader(TextReader textReader, Dictionary<string, IriReference> prefixes, TextWriter errorOutput)
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

    public ALC.Concept TestReader(TextReader textReader, TextWriter errorOutput) =>
        TestReader(textReader, new Dictionary<string, IriReference>(), errorOutput);
    public ALC.Concept TestString(string owl, TextWriter errorOutput)
    {
        using TextReader textReader = new StringReader(owl);
        return TestReader(textReader, errorOutput);
    }

    public ALC.Concept TestString(string owl, Dictionary<string, IriReference> prefixes, TextWriter errorOutput)
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
            ALC.Concept.NewConjunction(
                ALC.Concept.NewConceptName(new IriReference("http://example.com/ex1")),
            ALC.Concept.NewConceptName(new IriReference("http://example.com/ex2"))
                ));
    }


    [Fact]
    public void TestParenConjunction()
    {
        var conceptString = "(<http://example.com/ex1>) and (<http://example.com/ex2>)";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewConjunction(
                ALC.Concept.NewConceptName(new IriReference("http://example.com/ex1")),
                ALC.Concept.NewConceptName(new IriReference("http://example.com/ex2"))
            ));
    }
    [Fact]
    public void TestDisjunction()
    {
        var conceptString = "<http://example.com/ex1> or <http://example.com/ex2>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewDisjunction(
                ALC.Concept.NewConceptName(new IriReference("http://example.com/ex1")),
                ALC.Concept.NewConceptName(new IriReference("http://example.com/ex2"))
            ));
    }

    [Fact]
    public void TestNegation()
    {
        var conceptString = "not <http://example.com/ex1>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewNegation(
                ALC.Concept.NewConceptName(new IriReference("http://example.com/ex1"))
            ));
    }

    [Fact]
    public void TestUniversal()
    {
        var conceptString = "<http://example.com/name> only <http://foaf.com/name>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewUniversal(
                ALC.Role.NewIri(new IriReference("http://example.com/name")),
                ALC.Concept.NewConceptName(new IriReference("http://foaf.com/name"))
            ));
    }


    [Fact]
    public void TestExistential()
    {
        var conceptString = "<http://example.com/name> some <http://foaf.com/name>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewExistential(
                ALC.Role.NewIri(new IriReference("http://example.com/name")),
                ALC.Concept.NewConceptName(new IriReference("http://foaf.com/name"))
            ));
    }


    [Fact]
    public void TestParenExistential()
    {
        var conceptString = "(<http://example.com/name> some <http://foaf.com/name>)";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewExistential(
                ALC.Role.NewIri(new IriReference("http://example.com/name")),
                ALC.Concept.NewConceptName(new IriReference("http://foaf.com/name"))
            ));
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
        var expected = ALC.Concept.NewConjunction(
            ALC.Concept.NewExistential(
                ALC.Role.NewIri(new IriReference("https://example.com/p")),
                ALC.Concept.NewConceptName(new IriReference("https://example.com/a"))),
            ALC.Concept.NewUniversal(
                ALC.Role.NewIri(new IriReference("https://example.com/p")),
                ALC.Concept.NewConceptName(new IriReference("https://example.com/b")))
        );

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
            ALC.Concept.NewDisjunction(
                ALC.Concept.NewDisjunction(
                    ALC.Concept.NewConceptName(new IriReference("http://example.com/ex1")),
                    ALC.Concept.NewConceptName(new IriReference("http://example.com/ex2")))
                , ALC.Concept.NewConceptName(new IriReference("http://example.com/ex3"))

            ));
    }

    [Fact]
    public void TestTripleConjunction()
    {
        var conceptString = "<http://example.com/ex1> and <http://example.com/ex2> and <http://example.com/ex3>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewConjunction(
                ALC.Concept.NewConjunction(
                ALC.Concept.NewConceptName(new IriReference("http://example.com/ex1")),
                ALC.Concept.NewConceptName(new IriReference("http://example.com/ex2")))
                , ALC.Concept.NewConceptName(new IriReference("http://example.com/ex3"))

            ));
    }
    [Fact]
    public void TestNamedConcept()
    {
        var conceptString = "<http://example.com/ex1>";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewConceptName(new IriReference("http://example.com/ex1"))
            );
    }



    [Fact]
    public void TestParenNamedConcept()
    {
        var conceptString = "(<http://example.com/ex1>)";
        var parsedIri = TestString(conceptString, _testOutputTextWriter);
        parsedIri.Should().BeEquivalentTo(
            ALC.Concept.NewConceptName(new IriReference("http://example.com/ex1"))
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
        // var expected = ALC.Concept.NewConjunction(
        //     ALC.Concept.NewConceptName(new IriReference("http://www.w3.org/2002/07/owl#Thing")),
        //     ALC.Concept.NewConjunction(
        //         ALC.Concept.NewCardinality(
        //             new IriReference("http://example.com/hasFirstName"),
        //             1,
        //             ALC.Concept.NewConceptName(new IriReference("http://example.com/hasFirstName"))
        //         ),
        //         ALC.Concept.NewUniversal(
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