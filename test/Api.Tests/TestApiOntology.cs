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
        rdf.EnableOwlReasoning();
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


    [Fact]
    public void LoadImfOntologyWorks()
    {
        var ontologyFileInfo = new FileInfo("TestData/imf.ttl");
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, outputWriter);
        var ont = OwlOntology.Create(rdf);
        ont.GetAxioms().Should().NotBeEmpty();
        ont.GetAxiomRules().ToList().Should().NotBeEmpty();
    }


    [Fact, Category("LongRunning")]
    public void LoadGeneOntologyWorks()
    {
        var ontologyFileInfo = new FileInfo("TestData/go.ttl");
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, outputWriter);
        var ont = OwlOntology.Create(rdf);
        ont.GetAxioms().Should().NotBeEmpty();

    }
}