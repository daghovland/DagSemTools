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
        var element = q.Projection[0];
        element.IsProjectVariable.Should().BeTrue();
        ((Query.ProjectionElement.ProjectVariable)element).Item.Should().Be("name", "The projected variable is 'name'");
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
        var element = q.Projection[0];
        element.IsProjectVariable.Should().BeTrue();
        ((Query.ProjectionElement.ProjectVariable)element).Item.Should().Be("title", "The projected variable is 'title'");
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
        var element = q.Projection[0];
        element.IsProjectVariable.Should().BeTrue();
        ((Query.ProjectionElement.ProjectVariable)element).Item.Should().Be("v", "The projected variable is 'v'");
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
        var element = q.Projection[0];
        element.IsProjectVariable.Should().BeTrue();
        ((Query.ProjectionElement.ProjectVariable)element).Item.Should().Be("v", "The projected variable is 'v'");
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
        var element = q.Projection[0];
        element.IsProjectVariable.Should().BeTrue();
        ((Query.ProjectionElement.ProjectVariable)element).Item.Should().Be("v", "The projected variable is 'v'");
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
        var element = q.Projection[0];
        element.IsProjectVariable.Should().BeTrue();
        ((Query.ProjectionElement.ProjectVariable)element).Item.Should().Be("v", "The projected variable is 'v'");
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
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Count.Should().Be(2, "There are two projected variables");
        projection.Should().Contain("name");
        projection.Should().Contain("mbox");
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
        var element = q.Projection[0];
        element.IsProjectVariable.Should().BeTrue();
        ((Query.ProjectionElement.ProjectVariable)element).Item.Should().Be("x", "The projected variable is 'x'");
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
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Count.Should().Be(2, "There are two projected variables");
        projection.Should().Contain("name");
        projection.Should().Contain("mbox");
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


    [Fact]
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
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("title");
        projection.Should().Contain("price");
    }

    [Fact]
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
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("title");
        projection.Should().Contain("price");
    }

    [Fact]
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
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("title");
    }

    [Fact]
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
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias);
        projection.Should().Contain("title");
        projection.Should().Contain("price");
    }

    [Fact]
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
        var projection = q.Projection;
        projection.Length.Should().Be(1);
        var element = projection[0];
        element.IsProjectExpression.Should().BeTrue();
        var projExpr = (Query.ProjectionElement.ProjectExpression)element;
        var expr = projExpr.Item1;
        var alias = projExpr.alias;
        alias.Should().Be("totalPrice");
        expr.IsExprAggregate.Should().BeTrue();
        var agg = ((Query.Expression.ExprAggregate)expr).Item;
        agg.IsSum.Should().BeTrue();

        q.GroupBy.Length.Should().Be(1);
        var groupByElement = q.GroupBy[0];
        groupByElement.IsExprVariable.Should().BeTrue();
        ((Query.Expression.ExprVariable)groupByElement).Item.Should().Be("org");
    }

    [Fact]
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
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("y");
        projection.Should().Contain("minName");
    }

    [Fact]
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
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("book");
        projection.Should().Contain("title");
    }

    [Fact]
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
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("s");
    }

    [Fact]
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
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("name");
    }

    [Fact]
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
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("person");
    }

    /// <summary>
    /// SPARQL 1.2 spec §6.2: OPTIONAL with an inner FILTER
    /// </summary>
    [Fact(Skip = "FILTER inside OPTIONAL not yet evaluated")]
    public void TestSparql12ExampleOptionalWithFilter()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?name ?mbox
                        WHERE {
                          ?x foaf:name  ?name .
                          OPTIONAL { ?x foaf:mbox ?mbox
                                     FILTER regex(?mbox, "@work") }
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("name");
        projection.Should().Contain("mbox");
    }

    /// <summary>
    /// SPARQL 1.2 spec §6.3: Multiple OPTIONAL blocks
    /// </summary>
    [Fact(Skip = "Multiple OPTIONAL not yet fully evaluated")]
    public void TestSparql12ExampleMultipleOptionals()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?name ?mbox ?hpage
                        WHERE {
                          ?x foaf:name  ?name .
                          OPTIONAL { ?x foaf:mbox  ?mbox } .
                          OPTIONAL { ?x foaf:homepage ?hpage }
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("name");
        projection.Should().Contain("mbox");
        projection.Should().Contain("hpage");
    }

    /// <summary>
    /// SPARQL 1.2 spec §8.1: FILTER NOT EXISTS
    /// </summary>
    [Fact(Skip = "NOT EXISTS is not yet implemented")]
    public void TestSparql12ExampleNotExists()
    {
        string sparql = """
                        PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?person
                        WHERE {
                          ?person rdf:type foaf:Person .
                          FILTER NOT EXISTS { ?person foaf:name ?name }
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("person");
    }

    /// <summary>
    /// SPARQL 1.2 spec §9.3.2: Path alternative using |
    /// </summary>
    [Fact(Skip = "Path alternatives are not yet fully implemented")]
    public void TestSparql12ExamplePathAlternative()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                        SELECT ?x ?name
                        WHERE {
                          ?x foaf:name|rdfs:label ?name .
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("name");
    }

    /// <summary>
    /// SPARQL 1.2 spec §9.4: Inverse property path using ^
    /// </summary>
    [Fact(Skip = "Inverse property paths are not yet implemented")]
    public void TestSparql12ExamplePathInverse()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?x
                        WHERE {
                          ?x ^foaf:knows <http://example.org/alice> .
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("x");
    }

    /// <summary>
    /// SPARQL 1.2 spec §9.5: Arbitrary-length path using + (one or more)
    /// </summary>
    [Fact(Skip = "Arbitrary length property paths are not yet implemented")]
    public void TestSparql12ExamplePathOneOrMore()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?x ?name
                        WHERE {
                          <http://example.org/alice> foaf:knows+ ?x .
                          ?x foaf:name ?name .
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("name");
    }

    /// <summary>
    /// SPARQL 1.2 spec §9.5: Arbitrary-length path using * (zero or more)
    /// </summary>
    [Fact(Skip = "Arbitrary length property paths are not yet implemented")]
    public void TestSparql12ExamplePathZeroOrMore()
    {
        string sparql = """
                        PREFIX skos: <http://www.w3.org/2004/02/skos/core#>
                        SELECT ?concept ?broader
                        WHERE {
                          ?concept skos:broader* ?broader .
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("concept");
        projection.Should().Contain("broader");
    }

    /// <summary>
    /// SPARQL 1.2 spec §9.6: Negated property set
    /// </summary>
    [Fact(Skip = "Negated property sets are not yet implemented")]
    public void TestSparql12ExampleNegatedPropertySet()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?s ?p ?o
                        WHERE {
                          ?s !(foaf:name|foaf:mbox) ?o .
                          BIND(?p AS ?p)
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
    }

    /// <summary>
    /// SPARQL 1.2 spec §11.2: HAVING clause for post-aggregate filtering
    /// </summary>
    [Fact(Skip = "HAVING is not yet implemented")]
    public void TestSparql12ExampleHaving()
    {
        string sparql = """
                        PREFIX : <http://books.example/>
                        SELECT ?org (SUM(?lprice) AS ?totalPrice)
                        WHERE {
                          ?org :hasBook ?book .
                          ?book :price ?lprice .
                        }
                        GROUP BY ?org
                        HAVING (SUM(?lprice) > 10)
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("totalPrice");
    }

    /// <summary>
    /// SPARQL 1.2 spec §15.1: ORDER BY modifier
    /// </summary>
    [Fact(Skip = "ORDER BY is not yet implemented")]
    public void TestSparql12ExampleOrderBy()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?name
                        WHERE { ?x foaf:name ?name }
                        ORDER BY ?name
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("name");
    }

    /// <summary>
    /// SPARQL 1.2 spec §15.3–15.4: LIMIT and OFFSET modifiers
    /// </summary>
    [Fact(Skip = "LIMIT/OFFSET are not yet implemented")]
    public void TestSparql12ExampleLimitOffset()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?name
                        WHERE { ?x foaf:name ?name }
                        ORDER BY ?name
                        LIMIT 5
                        OFFSET 10
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("name");
    }

    /// <summary>
    /// SPARQL 1.2 spec §15.5: DISTINCT modifier
    /// </summary>
    [Fact(Skip = "DISTINCT is not yet implemented")]
    public void TestSparql12ExampleDistinct()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT DISTINCT ?name
                        WHERE { ?x foaf:name ?name }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("name");
    }

    /// <summary>
    /// SPARQL 1.2 spec §16.1.1: SELECT * wildcard projection
    /// </summary>
    [Fact(Skip = "SELECT * wildcard projection is not yet implemented")]
    public void TestSparql12ExampleSelectStar()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT *
                        WHERE { ?x foaf:name ?name ; foaf:mbox ?mbox }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        // SELECT * should project all variables in the WHERE clause
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("name");
        projection.Should().Contain("mbox");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.2: IN expression
    /// </summary>
    [Fact(Skip = "IN expression is not yet evaluated")]
    public void TestSparql12ExampleIn()
    {
        string sparql = """
                        PREFIX dc: <http://purl.org/dc/elements/1.1/>
                        SELECT ?title
                        WHERE {
                          ?book dc:title ?title .
                          FILTER (?title IN ("SPARQL Tutorial", "The Semantic Web"))
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("title");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.2: NOT IN expression
    /// </summary>
    [Fact(Skip = "NOT IN expression is not yet evaluated")]
    public void TestSparql12ExampleNotIn()
    {
        string sparql = """
                        PREFIX dc: <http://purl.org/dc/elements/1.1/>
                        SELECT ?title
                        WHERE {
                          ?book dc:title ?title .
                          FILTER (?title NOT IN ("SPARQL Tutorial"))
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("title");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.4.1: BOUND function
    /// </summary>
    [Fact(Skip = "BOUND function is not yet evaluated")]
    public void TestSparql12ExampleBound()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?name ?mbox
                        WHERE {
                          ?x foaf:name ?name .
                          OPTIONAL { ?x foaf:mbox ?mbox }
                          FILTER (BOUND(?mbox))
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("name");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.4.1: IF function
    /// </summary>
    [Fact(Skip = "IF function is not yet evaluated")]
    public void TestSparql12ExampleIf()
    {
        string sparql = """
                        PREFIX ns: <http://example.org/ns#>
                        SELECT ?x (IF(?x > 10, "big", "small") AS ?size)
                        WHERE { ?s ns:value ?x }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("size");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.4.1: COALESCE function
    /// </summary>
    [Fact(Skip = "COALESCE function is not yet evaluated")]
    public void TestSparql12ExampleCoalesce()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?name (COALESCE(?mbox, "no email") AS ?contact)
                        WHERE {
                          ?x foaf:name ?name .
                          OPTIONAL { ?x foaf:mbox ?mbox }
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("contact");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.4.2: isIRI / isLiteral / isBlank type-testing functions
    /// </summary>
    [Fact(Skip = "Type-testing functions (isIRI, isLiteral, isBlank) are not yet evaluated")]
    public void TestSparql12ExampleTypeTests()
    {
        string sparql = """
                        SELECT ?x
                        WHERE {
                          ?s ?p ?x .
                          FILTER (isLiteral(?x))
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("x");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.4.2: str, lang, datatype accessor functions
    /// </summary>
    [Fact(Skip = "str/lang/datatype functions are not yet evaluated")]
    public void TestSparql12ExampleAccessorFunctions()
    {
        string sparql = """
                        SELECT ?x (str(?x) AS ?s) (lang(?x) AS ?l) (datatype(?x) AS ?dt)
                        WHERE { ?s ?p ?x . FILTER(isLiteral(?x)) }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("s");
        projection.Should().Contain("l");
        projection.Should().Contain("dt");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.4.3: REGEX string function
    /// </summary>
    [Fact(Skip = "REGEX function is not yet evaluated")]
    public void TestSparql12ExampleRegex()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?name
                        WHERE {
                          ?x foaf:name ?name .
                          FILTER regex(?name, "^Ali", "i")
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("name");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.4.3: String functions STRLEN, SUBSTR, UCASE, LCASE, STRSTARTS
    /// </summary>
    [Fact(Skip = "String functions are not yet evaluated")]
    public void TestSparql12ExampleStringFunctions()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?name (STRLEN(?name) AS ?len) (UCASE(?name) AS ?upper)
                        WHERE {
                          ?x foaf:name ?name .
                          FILTER (STRSTARTS(?name, "A"))
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("len");
        projection.Should().Contain("upper");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.4.4: Numeric functions ABS, ROUND, CEIL, FLOOR
    /// </summary>
    [Fact(Skip = "Numeric functions are not yet evaluated")]
    public void TestSparql12ExampleNumericFunctions()
    {
        string sparql = """
                        PREFIX ns: <http://example.org/ns#>
                        SELECT ?x (ABS(?x) AS ?abs) (ROUND(?x) AS ?rounded) (CEIL(?x) AS ?ceiling) (FLOOR(?x) AS ?floored)
                        WHERE { ?s ns:value ?x }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("abs");
        projection.Should().Contain("rounded");
    }

    /// <summary>
    /// SPARQL 1.2 spec §13.3: GRAPH keyword for named graphs
    /// </summary>
    [Fact(Skip = "Named graph GRAPH patterns are not yet implemented")]
    public void TestSparql12ExampleGraphPattern()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?name ?graph
                        WHERE {
                          GRAPH ?graph {
                            ?x foaf:name ?name .
                          }
                        }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("name");
    }

    /// <summary>
    /// SPARQL 1.2 spec §11.3: COUNT aggregate
    /// </summary>
    [Fact(Skip = "COUNT aggregate evaluation is not yet verified")]
    public void TestSparql12ExampleCount()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT (COUNT(?x) AS ?count)
                        WHERE { ?x foaf:name ?name }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("count");
    }

    /// <summary>
    /// SPARQL 1.2 spec §11.3: COUNT DISTINCT
    /// </summary>
    [Fact(Skip = "COUNT DISTINCT evaluation is not yet verified")]
    public void TestSparql12ExampleCountDistinct()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT (COUNT(DISTINCT ?name) AS ?distinctNames)
                        WHERE { ?x foaf:name ?name }
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("distinctNames");
    }

    /// <summary>
    /// SPARQL 1.2 spec §11.3: GROUP_CONCAT aggregate
    /// </summary>
    [Fact(Skip = "GROUP_CONCAT aggregate evaluation is not yet implemented")]
    public void TestSparql12ExampleGroupConcat()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?org (GROUP_CONCAT(?name; SEPARATOR=", ") AS ?names)
                        WHERE { ?x foaf:member ?org ; foaf:name ?name }
                        GROUP BY ?org
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("names");
    }

    /// <summary>
    /// SPARQL 1.2 spec §11.3: SAMPLE aggregate
    /// </summary>
    [Fact(Skip = "SAMPLE aggregate evaluation is not yet implemented")]
    public void TestSparql12ExampleSample()
    {
        string sparql = """
                        PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                        SELECT ?org (SAMPLE(?name) AS ?someName)
                        WHERE { ?x foaf:member ?org ; foaf:name ?name }
                        GROUP BY ?org
                        """;
        var result = DagSemTools.Sparql.Parser.Parser.ParseString(sparql, _outputWriter);
        var q = result.Item1;
        q.Should().NotBeNull();
        var projection = q.Projection.Select(p => p.IsProjectVariable ? ((Query.ProjectionElement.ProjectVariable)p).Item : ((Query.ProjectionElement.ProjectExpression)p).alias).ToList();
        projection.Should().Contain("someName");
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