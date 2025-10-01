/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/
using DagSemTools.Rdf;
using DagSemTools.Ingress;
using DagSemTools.Sparql.Parser;
using FluentAssertions;
using IriTools;
using TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace Sparql.Parser.Tests;

public class TestParser : IDisposable, IAsyncDisposable
{

    private ITestOutputHelper _output;
    private TextWriter _outputWriter;
    public TestParser(ITestOutputHelper output)
    {
        _output = output;
        _outputWriter = new TestOutputTextWriter(_output);
    }
    
    [Fact]
    public void TestParseSimpleSelect()
    {
        string sparql = """
            PREFIX foaf: <http://xmlns.com/foaf/0.1/>
            SELECT ?name
            WHERE {
              ?person foaf:name ?name .
            }
            """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        result.Should().NotBeNull();
        result!.Projection.Length.Should().Be(1, "There is one projected variable");
        result.Projection[0].Should().Be("?name", "The projected variable is ?name");
        result.BGPs.Length.Should().Be(1, "There is one BGP");
        var bgp = result.BGPs[0];
        bgp.Should().Be(new Query.TriplePattern(
                Query.Term.NewVariable("person"),
                Query.Term.NewResource(new IriReference("http://xmlns.com/foaf/0.1/name")), 
                Query.Term.NewVariable("name")),
            "?person foaf:name ?name ");
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