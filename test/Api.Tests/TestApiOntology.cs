using System.ComponentModel;
using DagSemTools.Datalog;
using DagSemTools.Ingress;
using DagSemTools.Rdf;
using FluentAssertions;
using IriTools;
using Microsoft.FSharp.Collections;
using Serilog;
using Serilog.Sinks.InMemory;
using Xunit.Abstractions;

namespace Api.Tests;
using DagSemTools.Api;

public class TestApiOntology
{
    private ITestOutputHelper _output;
    private TestUtils.TestOutputTextWriter _outputWriter;
    private InMemorySink _inMemorySink;
    private ILogger _logger;

    public TestApiOntology(ITestOutputHelper output)
    {
        _output = output;
        _outputWriter = new TestUtils.TestOutputTextWriter(output);
        _inMemorySink = new InMemorySink();
        _logger =
        new LoggerConfiguration()
            .WriteTo.Sink(_inMemorySink)
            .WriteTo.Console()
            .CreateLogger();
    }

    [Fact]
    public void LoadEmptyOntologyWorks()
    {
        var ontologyFileInfo = new FileInfo("TestData/empty.owl");
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, _outputWriter);
        var ont = OwlOntology.Create(rdf);
        ont.GetAxioms().Should().NotBeEmpty();

    }

    [Fact]
    public void LoadSubClassFromRestriction()
    {
        var ontologyFileInfo = new FileInfo("TestData/subclass_of_restriction.owl");
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, _outputWriter);
        var ont = OwlOntology.Create(rdf);
        ont.GetAxioms().Should().NotBeEmpty();

    }


    [Fact]
    public void EqualityReasoningWorks()
    {
        //Arrange 
        var ontologyFileInfo = new FileInfo("TestData/equality.owl");
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, _outputWriter);
        var ont = OwlOntology.Create(rdf, _logger);
        ont.GetAxioms().Should().NotBeEmpty();
        var datalogProgram = ont.GetAxiomRules().ToList();
        IriReference rdfTypeIri = new(Namespaces.RdfType);
        IriReference ind1Iri = new("https://example.com/vocab#ind1");
        IriReference ind2Iri = new("https://example.com/vocab#ind2");
        rdf.GetTriplesWithSubjectPredicate(ind1Iri, rdfTypeIri).Should().NotBeEmpty();
        rdf.GetTriplesWithSubjectPredicate(ind2Iri, rdfTypeIri).Should().BeEmpty();

        // Act
        rdf.EnableEqualityReasoning();
        rdf.LoadDatalog(datalogProgram);


        // Assert
        rdf.GetTriplesWithSubjectPredicate(ind2Iri, rdfTypeIri).Should().NotBeEmpty();
    }



    [Fact]
    public void LoadIntersection()
    {
        var ontologyFileInfo = new FileInfo("TestData/intersection.owl.ttl");
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, _outputWriter);
        var ont = OwlOntology.Create(rdf);
        ont.GetAxioms().ToList().Should().NotBeEmpty();

    }

    [Theory]
    [InlineData("TestData/minQualifiedUnion.ttl")]
    [InlineData("TestData/someValuesFromInverse.ttl")]
    [InlineData("TestData/intersectionOfClassesWorks.ttl")]
    [InlineData("TestData/intersectionOfRestrictionsWorks.ttl")]
    [InlineData("TestData/someValuesExample.ttl")]
    [InlineData("TestData/minQualified.ttl")]
    [InlineData("TestData/minQualifiedSimpleUnion.ttl")]
    [InlineData("TestData/simpleUnion.ttl")]
    [InlineData("TestData/darlingExample.ttl")]
    [InlineData("TestData/qualifiedCardinalityIntersection.ttl")]
    public void ReasoningExampleWorks(string filename)
    {
        // Arrange
        var ontologyFileInfo = new FileInfo(filename);
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, _outputWriter);
        var ont = OwlOntology.Create(rdf);
        var axioms = ont.GetAxioms().ToList();
        axioms.Should().NotBeEmpty();
        _inMemorySink.LogEvents.Should().HaveCount(0);
        var calculatedTriple = new Triple(new("http://example.org/x"), new IriReference(Namespaces.RdfType), new IriReference("http://example.org/A"));
        var notCalculatedTriple = new Triple(new("http://example.org/notx"), new IriReference(Namespaces.RdfType), new IriReference("http://example.org/A"));

        // Act
        var axiomRules = ont.GetAxiomRules().ToList();
        axiomRules.Should().NotBeEmpty();
        _inMemorySink.LogEvents.Should().HaveCount(0);
        rdf.LoadDatalog(axiomRules);

        //Assert
        rdf.ContainsTriple(calculatedTriple).Should().BeTrue();
        rdf.ContainsTriple(notCalculatedTriple).Should().BeFalse();
        _inMemorySink.LogEvents.Should().HaveCount(0);
    }


    [Fact]
    public void DescriptorFromImfOntologyNonCyclic()
    {
        // Arrange
        var ontologyFileInfo = new FileInfo("TestData/cycle-imf-test.ttl");
        var rdfImf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, _outputWriter);
        var ont = OwlOntology.Create(rdfImf);
        ont.GetAxioms().Should().NotBeEmpty();

        // Act
        var axiomRules = ont.GetAxiomRules().ToList();
        axiomRules.Should().NotBeEmpty();
        rdfImf.LoadDatalog(axiomRules);

        _inMemorySink.LogEvents.Should().HaveCount(0);
    }


    [Fact]
    public void MaxQualifiedCardinalityIsIgnored()
    {
        // Arrange
        var ontologyFileInfo = new FileInfo("TestData/minimal-loop-test.ttl");
        var rdfImf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, _outputWriter);
        var ont = OwlOntology.Create(rdfImf);
        ont.GetAxioms().Should().NotBeEmpty();

        // Act
        var axiomRules = ont.GetAxiomRules().ToList();
        rdfImf.LoadDatalog(axiomRules);

        _inMemorySink.LogEvents.Should().HaveCount(0);
    }


    [Fact(Skip = "Too long")]
    public void LoadImfOntologyWorks()
    {
        // Arrange
        var ontologyFileInfo = new FileInfo("TestData/imf.ttl");
        var rdfImf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, _outputWriter);
        var aboxFileInfo = new FileInfo("TestData/imf-data.ttl");
        var imfData = DagSemTools.Api.TurtleParser.Parse(aboxFileInfo, _outputWriter);
        var ont = OwlOntology.Create(rdfImf);

        // Act
        var axiomRules = ont.GetAxiomRules().ToList();

        // Assert
        axiomRules.Should().NotBeEmpty();
        var ruleStringList = axiomRules.Select(rule => rule.ToString(rdfImf.Datastore.Resources));
        var ruleString = String.Join("\n", ruleStringList.Concat());
        _outputWriter.WriteLine(ruleString);

        imfData.LoadDatalog(axiomRules);

        _inMemorySink.LogEvents.Should().HaveCount(0);
    }


    [Fact]
    public void DuplicateRulesWorks()
    {
        // Arrange
        var datalogString = File.ReadAllText("TestData/duplicate_rules.datalog");
        var datastore = new Datastore(1000);
        var rules = DagSemTools.Datalog.Parser.Parser.ParseString(datalogString, _outputWriter, datastore);
        var ruleList = ListModule.OfSeq(rules);
        var stratifier = new Stratifier.RulePartitioner(_logger, ruleList, datastore.Resources);

        // Act
        var ordered = stratifier.orderRules().ToList(); ;

        // Assert
        ordered.Should().HaveCount(1);
        ordered.First().Should().HaveCount(1);
        _inMemorySink.LogEvents.Should().HaveCount(0);
    }


    [Fact]
    public void ParseImfOntologyWorks()
    {
        // Arrange
        var ontologyFileInfo = new FileInfo("TestData/imf.ttl");
        var rdfImf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, _outputWriter);

        // Act
        var ont = OwlOntology.Create(rdfImf);
        ont.GetAxioms().Should().NotBeEmpty();
        var axiomRules = ont.GetAxiomRules().ToList();

        axiomRules.Should().NotBeEmpty();

        _inMemorySink.LogEvents.Should().HaveCount(0);
    }


    [Fact(Skip = "Must wait until number constraints are implemented in tableau. See Issue https://github.com/daghovland/DagSemTools/issues/2")]
    public void Imf2AlcWorks()
    {
        // Arrange
        var ontologyFileInfo = new FileInfo("TestData/imf.ttl");
        var rdfImf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, _outputWriter);
        var ont = OwlOntology.Create(rdfImf);

        // Act 
        var alc = ont.GetTableauReasoner();
        alc.Should().NotBeNull();

        _inMemorySink.LogEvents.Should().HaveCount(0);
    }



    [Fact]
    public void LoadIDOOntologyWorks()
    {
        var ontologyFileInfo = new FileInfo("TestData/LIS-14.ttl");
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, _outputWriter);
        var ont = OwlOntology.Create(rdf);
        ont.GetAxioms().Should().NotBeEmpty();
        var datalogProgram = ont.GetAxiomRules().ToList();
        datalogProgram.Should().NotBeEmpty();
        rdf.LoadDatalog(datalogProgram);
    }


    [Fact(Skip = "Too long runtime")]
    public void ParseGeneOntologyWorks()
    {
        var ontologyFileInfo = new FileInfo("TestData/go.ttl");
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, _outputWriter);
        rdf.IsEmpty().Should().BeFalse();
        var ont = OwlOntology.Create(rdf);
        //ont.GetAxioms().Should().NotBeEmpty();
        //var datalogProgram = ont.GetAxiomRules().ToList();
        //datalogProgram.Should().NotBeEmpty();
        //rdf.LoadDatalog(datalogProgram);
    }
}