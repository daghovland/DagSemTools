using System.ComponentModel;
using FluentAssertions;
using Microsoft.FSharp.Collections;
using Xunit.Abstractions;

namespace Api.Tests;
using DagSemTools.Api;

public class TestApiOntology(ITestOutputHelper output)
{
    TestUtils.TestOutputTextWriter outputWriter = new TestUtils.TestOutputTextWriter(output);


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
    public void LoadgeneOntologyWorks()
    {
        var ontologyFileInfo = new FileInfo("TestData/go.ttl");
        var rdf = DagSemTools.Api.TurtleParser.Parse(ontologyFileInfo, outputWriter);
        var ont = OwlOntology.Create(rdf);
        ont.GetAxioms().Should().NotBeEmpty();

    }
}