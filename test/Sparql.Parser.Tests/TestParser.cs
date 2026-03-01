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
        var q = result.Item1;
        var e = result.Item2;
        q.Should().NotBeNull();
        q.Projection.Length.Should().Be(1, "There is one projected variable");
        q.Projection[0].Should().Be("name", "The projected variable is 'name'");
        q.Query.Length.Should().Be(1, "There is one BGP");
        var bgp = q.Query[0];
        bgp.Should().Be(Query.QueryComponent.NewPattern(Query.GetDefaultGraphPattern(
                Query.Term.NewVariable("person"),
                Query.Term.NewResource(e.GraphElementMap[GraphElement.NewNodeOrEdge(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name")))]),
                Query.Term.NewVariable("name"))),
            "?person foaf:name ?name ");
    }

    [Fact]
    public void TestSparql12Example1()
    {
        string sparql = """
                        SELECT ?title
                        WHERE
                        {
                            <http://example.org/book/book1> <http://purl.org/dc/elements/1.1/title> ?title .
                        }
                        
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        var e = result.Item2;
        q.Should().NotBeNull();
        q.Projection.Length.Should().Be(1, "There is one projected variable");
        q.Projection[0].Should().Be("title", "The projected variable is 'title'");
        q.Query.Length.Should().Be(1, "There is one BGP");
        var bgp = q.Query[0];
        bgp.Should().Be(Query.QueryComponent.NewPattern(Query.GetDefaultGraphPattern(
            Query.Term.NewResource(e.GraphElementMap[GraphElement.NewNodeOrEdge(RdfResource.NewIri(new IriReference("http://example.org/book/book1")))]),
                Query.Term.NewResource(e.GraphElementMap[GraphElement.NewNodeOrEdge(RdfResource.NewIri(new IriReference("http://purl.org/dc/elements/1.1/title")))]),
                Query.Term.NewVariable("title"))));
    }

    [Fact]
    public void TestSparql12Example3()
    {
        string sparql = """
                        SELECT ?v WHERE { ?v ?p "cat" }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        var e = result.Item2;
        q.Should().NotBeNull();
        q.Projection.Length.Should().Be(1, "There is one projected variable");
        q.Projection[0].Should().Be("v", "The projected variable is 'v'");
        q.Query.Length.Should().Be(1, "There is one BGP");
        var bgp = q.Query[0];
        bgp.Should().Be(Query.QueryComponent.NewPattern(Query.GetDefaultGraphPattern(
            Query.Term.NewVariable("v"),
            Query.Term.NewVariable("p"),
            Query.Term.NewResource(e.GraphElementMap[GraphElement.NewGraphLiteral(RdfLiteral.NewLiteralString("cat"))]))));
    }


    [Fact]
    public void TestSparql12Example4()
    {
        string sparql = """
                        SELECT ?v WHERE { ?v ?p "cat"@en }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        var e = result.Item2;
        q.Should().NotBeNull();
        q.Projection.Length.Should().Be(1, "There is one projected variable");
        q.Projection[0].Should().Be("v", "The projected variable is 'v'");
        q.Query.Length.Should().Be(1, "There is one BGP");
        var bgp = q.Query[0];
        bgp.Should().Be(Query.QueryComponent.NewPattern(Query.GetDefaultGraphPattern(
            Query.Term.NewVariable("v"),
            Query.Term.NewVariable("p"),
            Query.Term.NewResource(e.GraphElementMap[GraphElement.NewGraphLiteral(RdfLiteral.NewLangLiteral("cat", "en"))]))));
    }

    [Fact]
    public void TestSparql12Example5()
    {
        string sparql = """
                        SELECT ?v WHERE { ?v ?p 42 }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        var e = result.Item2;
        q.Should().NotBeNull();
        q.Projection.Length.Should().Be(1, "There is one projected variable");
        q.Projection[0].Should().Be("v", "The projected variable is 'v'");
        q.Query.Length.Should().Be(1, "There is one BGP");
        var bgp = q.Query[0];
        bgp.Should().Be(Query.QueryComponent.NewPattern(Query.GetDefaultGraphPattern(
            Query.Term.NewVariable("v"),
            Query.Term.NewVariable("p"),
            Query.Term.NewResource(e.GraphElementMap[GraphElement.NewGraphLiteral(RdfLiteral.NewIntegerLiteral(42))]))));
    }

    [Fact]
    public void TestSparql12Example6()
    {
        string sparql = """
                        SELECT ?v WHERE { ?v ?p "abc"^^<http://example.org/datatype#specialDatatype> }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        var e = result.Item2;
        q.Should().NotBeNull();
        q.Projection.Length.Should().Be(1, "There is one projected variable");
        q.Projection[0].Should().Be("v", "The projected variable is 'v'");
        q.Query.Length.Should().Be(1, "There is one BGP");
        var bgp = q.Query[0];
        bgp.Should().Be(Query.QueryComponent.NewPattern(Query.GetDefaultGraphPattern(
            Query.Term.NewVariable("v"),
            Query.Term.NewVariable("p"),
            Query.Term.NewResource(e.GraphElementMap[GraphElement.NewGraphLiteral(RdfLiteral.NewTypedLiteral(new IriReference("http://example.org/datatype#specialDatatype"), "abc"))]))));
    }

    [Fact]
    public void TestSparql12ExamplePredicateObjectList()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?name ?mbox
                        WHERE {
                          ?x foaf:name ?name ;
                             foaf:mbox ?mbox .
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        var e = result.Item2;
        q.Should().NotBeNull();
        q.Projection.Length.Should().Be(2, "There are two projected variables");
        q.Query.Length.Should().Be(2, "There are two triple patterns");
        var bgp1 = q.Query[0];
        bgp1.Should().Be(Query.QueryComponent.NewPattern(Query.GetDefaultGraphPattern(
            Query.Term.NewVariable("x"),
            Query.Term.NewResource(e.GraphElementMap[GraphElement.NewNodeOrEdge(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name")))]),
            Query.Term.NewVariable("name"))));
        var bgp2 = q.Query[1];
        bgp2.Should().Be(Query.QueryComponent.NewPattern(Query.GetDefaultGraphPattern(
            Query.Term.NewVariable("x"),
            Query.Term.NewResource(e.GraphElementMap[GraphElement.NewNodeOrEdge(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/mbox")))]),
            Query.Term.NewVariable("mbox"))));
    }

    [Fact]
    public void TestSparql12ExampleObjectList()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?x
                        WHERE {
                          ?x foaf:nick "Alice" , "Alice_" .
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        var e = result.Item2;
        q.Should().NotBeNull();
        q.Projection.Length.Should().Be(1, "There is one projected variable");
        q.Query.Length.Should().Be(2, "There are two triple patterns");
        var bgp1 = q.Query[0];
        bgp1.Should().Be(Query.QueryComponent.NewPattern(Query.GetDefaultGraphPattern(
            Query.Term.NewVariable("x"),
            Query.Term.NewResource(e.GraphElementMap[GraphElement.NewNodeOrEdge(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/nick")))]),
            Query.Term.NewResource(e.GraphElementMap[GraphElement.NewGraphLiteral(RdfLiteral.NewLiteralString("Alice"))]))));
        var bgp2 = q.Query[1];
        bgp2.Should().Be(Query.QueryComponent.NewPattern(Query.GetDefaultGraphPattern(
            Query.Term.NewVariable("x"),
            Query.Term.NewResource(e.GraphElementMap[GraphElement.NewNodeOrEdge(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/nick")))]),
            Query.Term.NewResource(e.GraphElementMap[GraphElement.NewGraphLiteral(RdfLiteral.NewLiteralString("Alice_"))]))));
    }

    /// <summary>
    /// Example from sparql-1.2 spec, section 6.1
    /// </summary>

    [Fact]
    public void TestSparql12ExampleOptional()
    {
        var sparql = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?name ?mbox
                          WHERE  {
                              ?x foaf:name  ?name .
                              OPTIONAL { ?x  foaf:mbox  ?mbox }
                          }
                          """;

        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        var e = result.Item2;
        q.Should().NotBeNull();
        q.Projection.Length.Should().Be(2, "There are two projected variables");
        q.Projection[0].Should().Be("name", "The projected variable is 'name'");
        q.Projection[1].Should().Be("mbox", "The projected variable is 'mbox'");
        q.Query.Length.Should().Be(2, "There is one pattern and one optional");
        var bgp = q.Query[0];
        bgp.Should().Be(Query.QueryComponent.NewPattern(Query.GetDefaultGraphPattern(
            Query.Term.NewVariable("x"),
            Query.Term.NewResource(e.GraphElementMap[
                GraphElement.NewNodeOrEdge(
                    RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name")))]),
            Query.Term.NewVariable("name")
        )));
        var optionalPattern = q.Query[1];
        optionalPattern.IsOptional.Should().BeTrue();
        var opt = ((Query.QueryComponent.Optional)optionalPattern).Item;
        var optGroup = opt.Item;
        optGroup.Length.Should().Be(1);
        optGroup[0].Should().Be(Query.QueryComponent.NewPattern(Query.GetDefaultGraphPattern(
            Query.Term.NewVariable("x"),
            Query.Term.NewResource(e.GraphElementMap[
                GraphElement.NewNodeOrEdge(
                    RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/mbox")))]),
            Query.Term.NewVariable("mbox")
        )));
    }


    [Fact(Skip = "Select Expressions are not yet supported in the AST")]
    public void TestSparql12ExampleSelectExpressions()
    {
        string sparql = """
                        PREFIX dc:   <http://purl.org/dc/elements/1.1/>
                        PREFIX :     <http://example.org/book/>
                        PREFIX ns:   <http://example.org/ns#>
                        SELECT ?title (?p*(1-?discount) AS ?price)
                        WHERE { ?book dc:title ?title ; ns:price ?p ; ns:discount ?discount }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        IEnumerable<string> projection = q.Projection;
        projection.Should().Contain("title");
        projection.Should().Contain("price");
    }

    [Fact(Skip = "BIND is not yet implemented")]
    public void TestSparql12ExampleBind()
    {
        string sparql = """
                        PREFIX dc:   <http://purl.org/dc/elements/1.1/>
                        PREFIX ns:   <http://example.org/ns#>
                        SELECT ?title ?price
                        WHERE {
                          ?x dc:title ?title ;
                             ns:price ?p ;
                             ns:discount ?discount .
                          BIND (?p*(1-?discount) AS ?price)
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        IEnumerable<string> projection = q.Projection;
        projection.Should().Contain("title");
        projection.Should().Contain("price");
    }

    [Fact(Skip = "UNION is not yet implemented")]
    public void TestSparql12ExampleUnion()
    {
        string sparql = """
                        PREFIX dc10:  <http://purl.org/dc/elements/1.0/>
                        PREFIX dc11:  <http://purl.org/dc/elements/1.1/>
                        SELECT ?title
                        WHERE  { { ?book dc10:title ?title } UNION { ?book dc11:title ?title } }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        IEnumerable<string> projection = q.Projection;
        projection.Should().Contain("title");
    }

    [Fact(Skip = "FILTER is not yet implemented")]
    public void TestSparql12ExampleFilter()
    {
        string sparql = """
                        PREFIX  dc:  <http://purl.org/dc/elements/1.1/>
                        PREFIX  ns:  <http://example.org/ns#>
                        SELECT  ?title ?price
                        WHERE   { ?x ns:price ?price .
                                  FILTER (?price < 30) .
                                  ?x dc:title ?title . }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        IEnumerable<string> projection = q.Projection;
        projection.Should().Contain("title");
        projection.Should().Contain("price");
    }

    [Fact(Skip = "Aggregates are not yet implemented")]
    public void TestSparql12ExampleAggregate()
    {
        string sparql = """
                        PREFIX : <http://books.example/>
                        SELECT (SUM(?lprice) AS ?totalPrice)
                        WHERE {
                          ?org :hasBook ?book .
                          ?book :price ?lprice .
                        }
                        GROUP BY ?org
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        IEnumerable<string> projection = q.Projection;
        projection.Should().Contain("totalPrice");
    }

    [Fact(Skip = "Subqueries are not yet implemented")]
    public void TestSparql12ExampleSubquery()
    {
        string sparql = """
                        PREFIX : <http://people.example/>
                        SELECT ?y ?minName
                        WHERE {
                          :alice :knows ?y .
                          {
                            SELECT ?y (MIN(?name) AS ?minName)
                            WHERE {
                              ?y :name ?name .
                            }
                            GROUP BY ?y
                          }
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        IEnumerable<string> projection = q.Projection;
        projection.Should().Contain("y");
        projection.Should().Contain("minName");
    }

    [Fact(Skip = "VALUES is not yet implemented")]
    public void TestSparql12ExampleValues()
    {
        string sparql = """
                        PREFIX dc:   <http://purl.org/dc/elements/1.1/> 
                        PREFIX :     <http://example.org/book/> 
                        SELECT ?book ?title
                        WHERE {
                           ?book dc:title ?title .
                           VALUES ?book { :book1 :book3 }
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        IEnumerable<string> projection = q.Projection;
        projection.Should().Contain("book");
        projection.Should().Contain("title");
    }

    [Fact(Skip = "MINUS is not yet implemented")]
    public void TestSparql12ExampleMinus()
    {
        string sparql = """
                        PREFIX : <http://example.org/>
                        SELECT ?s
                        WHERE {
                          ?s :p ?o .
                          MINUS { ?s :q ?o2 }
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        IEnumerable<string> projection = q.Projection;
        projection.Should().Contain("s");
    }

    [Fact(Skip = "Property Paths are not yet fully implemented")]
    public void TestSparql12ExamplePropertyPath()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?name
                        WHERE {
                          ?x foaf:mbox <mailto:alice@example.org> .
                          ?x foaf:knows/foaf:name ?name .
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        IEnumerable<string> projection = q.Projection;
        projection.Should().Contain("name");
    }

    [Fact(Skip = "EXISTS is not yet implemented")]
    public void TestSparql12ExampleExists()
    {
        string sparql = """
                        PREFIX  :       <http://example.org/>
                        SELECT ?person
                        WHERE {
                          ?person :name ?name .
                          FILTER EXISTS { ?person :mbox ?mbox }
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        IEnumerable<string> projection = q.Projection;
        projection.Should().Contain("person");
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