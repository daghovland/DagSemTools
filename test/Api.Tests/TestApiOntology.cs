using System.ComponentModel;
using DagSemTools.Ingress;
using FluentAssertions;
using IriTools;
using Microsoft.FSharp.Collections;
using Serilog;
using Serilog.Sinks.InMemory;
using Xunit.Abstractions;

namespace Api.Tests;
using DagSemTools.Api;

public class TestApiOntology(ITestOutputHelper output)
{
    TestUtils.TestOutputTextWriter outputWriter = new TestUtils.TestOutputTextWriter(output);

    private static InMemorySink _inMemorySink = new InMemorySink();

    private ILogger _logger =
        new LoggerConfiguration()
            .WriteTo.Sink(_inMemorySink)
            .WriteTo.Console()
            .CreateLogger();

    [Fact]
    public void LoadEmptyOntologyWorks()
    {
        var ontologyFileInfo = new FileInfo("TestData/empty.owl");
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, outputWriter);
        var ont = OwlOntology.Create(rdf);
        ont.GetAxioms().Should().NotBeEmpty();

    }

    [Fact]
    public void LoadSubClassFromRestriction()
    {
        var ontologyFileInfo = new FileInfo("TestData/subclass_of_restriction.owl");
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, outputWriter);
        var ont = OwlOntology.Create(rdf);
        ont.GetAxioms().Should().NotBeEmpty();

    }


    [Fact]
    public void EqualityReasoningWorks()
    {
        //Arrange 
        var ontologyFileInfo = new FileInfo("TestData/equality.owl");
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, outputWriter);
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
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, outputWriter);
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
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, outputWriter);
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

    [Fact(Skip = "Not working, github issue: https://github.com/daghovland/DagSemTools/issues/55" )]
    public void LoadImfOntologyWorks()
    {
        // Arrange
        var ontologyFileInfo = new FileInfo("TestData/imf.ttl");
        var rdfImf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, outputWriter);
        var aboxFileInfo = new FileInfo("TestData/imf-data.ttl");
        var imfData = DagSemTools.Api.TurtleParser.Parse(aboxFileInfo, outputWriter);
        var ont = OwlOntology.Create(rdfImf);
        ont.GetAxioms().Should().NotBeEmpty();
    
        // Act
        var axiomRules = ont.GetAxiomRules().ToList();
        axiomRules.Should().NotBeEmpty();
        rdfImf.LoadDatalog(axiomRules);
    
        _inMemorySink.LogEvents.Should().HaveCount(0);
    }


     [Fact(Skip = "Takes too long for normal testing. https://github.com/daghovland/DagSemTools/issues/58"), Category("LongRunning") ]
     public void LoadGeneOntologyWorks()
     {
         var ontologyFileInfo = new FileInfo("TestData/go.ttl");
         var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, outputWriter);
         var ont = OwlOntology.Create(rdf);
         ont.GetAxioms().Should().NotBeEmpty();
         var datalogProgram = ont.GetAxiomRules().ToList();
         datalogProgram.Should().NotBeEmpty();
         rdf.LoadDatalog(datalogProgram);
    }
}