/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/
using DagSemTools.Rdf;
using DagSemTools.Ingress;
using DagSemTools.Turtle.Parser;
using FluentAssertions;
using IriTools;
using TestUtils;
using Xunit.Abstractions;

namespace TurtleParser.Unit.Tests;

public class TestTriGParser : IDisposable, IAsyncDisposable
{

    private ITestOutputHelper _output;
    private TextWriter _outputWriter;
    public TestTriGParser(ITestOutputHelper output)
    {
        _output = output;
        _outputWriter = new TestOutputTextWriter(_output);
    }
    public Datastore TestOntology(string ontology)
    {
        return Parser.ParseString(ontology, _outputWriter);

    }

    [Fact]
    public void TestTrigExample1()
    {
        var ontology = File.ReadAllText("TestData/trig_example1.trig");
        var ont = TestOntology(ontology);
        Assert.NotNull(ont);
        var knows = ont.GetGraphElementId(GraphElement.NewNodeOrEdge(RdfResource.NewIri(new IriReference("http://www.example.org/vocabulary#name"))));
        ont.GetTriplesWithPredicate(knows).Should().HaveCount(1);
        
    }
    
    
    [Fact]
    public void TestTrigExample2()
    {
        var ontology = File.ReadAllText("TestData/trig_example2.trig");
        var ont = TestOntology(ontology);
        Assert.NotNull(ont);
        var bob = ont.GetGraphElementId(GraphElement.NewNodeOrEdge(RdfResource.NewIri(new IriReference("http://example.org/bob"))));
        var defaultBobTriples = ont.GetTriplesWithSubject(bob);
        defaultBobTriples.Should().HaveCount(1);

        var bobGraphBobTriples = ont.GetTriplesWithSubject(bob, bob);
        bobGraphBobTriples.Should().HaveCount(1);
    }
    
    public void Dispose()
    {
        _outputWriter.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _outputWriter.DisposeAsync();
    }
}