using DagSemTools.Api;
using DagSemTools.Rdf;
using DagSemTools.Ingress;
using IriTools;
using FluentAssertions;
using Xunit.Abstractions;
using RdfLiteral = DagSemTools.Api.RdfLiteral;

namespace Api.Tests;

public class TestApi(ITestOutputHelper output)
{
    TestUtils.TestOutputTextWriter outputWriter = new TestUtils.TestOutputTextWriter(output);

    [Fact]
    public void Test1()
    {
        var ontology = new FileInfo("TestData/example1.ttl");
        var ont = DagSemTools.Api.TriGParser.Parse(ontology, outputWriter);

        Assert.NotNull(ont);
        var labels = ont.GetDefaultGraph().GetTriplesWithSubjectPredicate(
            new IriReference("http://dbpedia.org/datatype/FuelEfficiency"),
            new IriReference("http://www.w3.org/2000/01/rdf-schema#label"));
        labels.Count().Should().Be(1, "There is one label on fuel efficiency");
    }

    [Fact]
    public void TestAbbreviatedBlankNode()
    {
        var ontology = new FileInfo("TestData/abbreviated_blank_nodes.ttl");
        var ont = DagSemTools.Api.TriGParser.Parse(ontology, outputWriter);
        Assert.NotNull(ont);


        var knows = ont.GetDefaultGraph().GetTriplesWithPredicate(new IriReference("http://xmlns.com/foaf/0.1/knows")).ToList();
        knows.Should().HaveCount(2);


        var name = ont.GetDefaultGraph().GetTriplesWithPredicate(new IriReference("http://xmlns.com/foaf/0.1/name")).ToList();
        name.Should().HaveCount(3);
        var isKnown = knows.First().Object;
        var bobHasName = name.Skip(1).First().Subject;
        isKnown.Should().Be(bobHasName);

        var mbox = ont.GetDefaultGraph().GetTriplesWithPredicate(new IriReference("http://xmlns.com/foaf/0.1/mbox"));

        mbox.Should().HaveCount(1);

        var eve = ont.GetDefaultGraph().GetTriplesWithPredicate(new IriReference("http://xmlns.com/foaf/0.1/name"))
            .Where(tr => tr.Object.Equals(ont.GetResourceManager().CreateRdfStringLiteral("Eve")));
        eve.Should().HaveCount(1);
    }

    [Fact]
    public void TestDatalogReasoning()
    {
        var ontology = new FileInfo("TestData/data.ttl");
        var ont = DagSemTools.Api.TriGParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetDefaultGraph().GetTriplesWithPredicateObject(
            new IriReference("https://example.com/data#predicate"),
            new IriReference("https://example.com/data#object"));
        resultsData.Should().HaveCount(1);
        var resultsBefore = ont.GetDefaultGraph().GetTriplesWithPredicateObject(
            new IriReference("https://example.com/data#predicate"),
            new IriReference("https://example.com/data#object2"));
        resultsBefore.Should().BeEmpty();
        Assert.NotNull(ont);
        var datalogFile = new FileInfo("TestData/rules.datalog");
        ont.LoadDatalog(datalogFile);
        var resultsAfter = ont.GetDefaultGraph().GetTriplesWithPredicateObject(
            new IriReference("https://example.com/data#predicate"),
            new IriReference("https://example.com/data#object2"));
        resultsAfter.Should().HaveCount(1);
    }


    [Fact]
    public void TestNamedGraphDatalogReasoning()
    {
        var ontology = new FileInfo("TestData/namedgraph.trig");
        var ont = DagSemTools.Api.TriGParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetNamedGraph("https://example.com/data#graph").GetTriplesWithPredicateObject(
            new IriReference("https://example.com/data#predicate"),
            new IriReference("https://example.com/data#object"));
        resultsData.Should().HaveCount(1);
        var resultsBefore = ont.GetNamedGraph("https://example.com/data#graph").GetTriplesWithPredicateObject(
            new IriReference("https://example.com/data#predicate"),
            new IriReference("https://example.com/data#object2"));
        resultsBefore.Should().BeEmpty();
        Assert.NotNull(ont);
        var datalogFile = new FileInfo("TestData/namedgraph.datalog");
        ont.LoadDatalog(datalogFile);
        var resultsAfter = ont.GetNamedGraph("https://example.com/data#graph").GetTriplesWithPredicateObject(
            new IriReference("https://example.com/data#predicate"),
            new IriReference("https://example.com/data#object2"));
        resultsAfter.Should().HaveCount(1, "Datalog reasoning should have added this triple");
    }

    [Fact]
    public void TestA()
    {
        var ontology = new FileInfo("TestData/test2.ttl");
        var ont = DagSemTools.Api.TriGParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetDefaultGraph().GetTriplesWithObject(
            new IriReference("http://example.com/data#property")).ToList();
        resultsData.Should().HaveCount(1);
        resultsData.First().Predicate.Should().Be(new IriReference(Namespaces.RdfType));

    }

    [Fact]
    public void TestStreamParsing()
    {
        var ontology = new FileInfo("TestData/test2.ttl");
        var ont = DagSemTools.Api.TriGParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetDefaultGraph().GetTriplesWithObject(
            new IriReference("http://example.com/data#property")).ToList();
        resultsData.Should().HaveCount(1);
        resultsData.First().Predicate.Should().Be(new IriReference(Namespaces.RdfType));

    }
    [Fact]
    public void TestDatalog2()
    {
        var ontology = new FileInfo("TestData/test2.ttl");
        var ont = DagSemTools.Api.TriGParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetDefaultGraph().GetTriplesWithObject(
            new IriReference("http://example.com/data#property")).ToList();
        resultsData.Should().HaveCount(1);
        resultsData.First().Predicate.Should().Be(new IriReference(Namespaces.RdfType));

        var datalogFile = new FileInfo("TestData/test2.datalog");
        ont.LoadDatalog(datalogFile);

        resultsData = ont.GetDefaultGraph().GetTriplesWithObject(
            new IriReference("http://example.com/data#property")).ToList();
        resultsData.Should().HaveCount(3);

    }



    [Fact]
    public void TestDatalogStratified()
    {
        var ontology = new FileInfo("TestData/test_stratified.ttl");
        var ont = DagSemTools.Api.TriGParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetDefaultGraph().GetTriplesWithObject(
            new IriReference("http://example.com/data#Type")).ToList();
        resultsData.Should().HaveCount(1);
        resultsData.First().Predicate.Should().Be(new IriReference(Namespaces.RdfType));

        resultsData = ont.GetDefaultGraph().GetTriplesWithObject(
            new IriReference("http://example.com/data#Type3")).ToList();
        resultsData.Should().HaveCount(0);

        var datalogFile = new FileInfo("TestData/test_stratified.datalog");
        ont.LoadDatalog(datalogFile);

        resultsData = ont.GetDefaultGraph().GetTriplesWithObject(
            new IriReference("http://example.com/data#Type3")).ToList();
        resultsData.Should().HaveCount(1);

    }

    /// <summary>
    /// First simple example in sparql 1.2 docs
    /// </summary>
    [Fact]
    public void TestSparql1()
    {
        var data = "<http://example.org/book/book1> <http://purl.org/dc/elements/1.1/title> \"SPARQL Tutorial\" .";
        var graph = ParseTurtleData(data);
        var queryString = """
                          SELECT ?title
                          WHERE
                          {
                              <http://example.org/book/book1> <http://purl.org/dc/elements/1.1/title> ?title .
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
        var answer = answers.First();
        answer.Count.Should().Be(1);
        var actual = answer["title"];
        var expected = graph.GetResourceManager().CreateRdfStringLiteral("SPARQL Tutorial");
        Assert.Equal(actual, expected);

    }



    /// <summary>
    /// Second example in sparql 1.2 docs
    /// </summary>
    [Fact]
    public void TestSparql2()
    {
        var data = """
                    PREFIX foaf:  <http://xmlns.com/foaf/0.1/> .

                    _:a  foaf:name   "Johnny Lee Outlaw" .
                    _:a  foaf:mbox   <mailto:jlow@example.com> .
                    _:b  foaf:name   "Peter Goodguy" .
                    _:b  foaf:mbox   <mailto:peter@example.org> .
                    _:c  foaf:mbox   <mailto:carol@example.org> .
                """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf:   <http://xmlns.com/foaf/0.1/>
                          SELECT ?name ?mbox
                          WHERE
                          { ?x foaf:name ?name .
                            ?x foaf:mbox ?mbox }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
        var answer = answers.First();
        answer.Count.Should().Be(2);
        var actual = answer["name"];
        var expected1 = graph.GetResourceManager().CreateRdfStringLiteral("Johnny Lee Outlaw");
        var expected2 = graph.GetResourceManager().CreateRdfStringLiteral("Peter Goodguy");
        (actual.Equals(expected1) || actual.Equals(expected2)).Should().BeTrue();
        var actualMbox = answer["mbox"];
        var expectedMbox1 = graph.GetResourceManager().CreateIriResource("mailto:peter@example.org");
        var expectedMbox2 = graph.GetResourceManager().CreateIriResource("mailto:carol@example.org");
        (actualMbox.Equals(expectedMbox1) || actualMbox.Equals(expectedMbox2)).Should().BeTrue();

    }


    /// <summary>
    /// Third example, literals, in sparql 1.2 docs section 2.3.1
    /// </summary>
    [Fact]
    public void TestSparql3()
    {
        var data = """
                   PREFIX dt:   <http://example.org/datatype#>
                   PREFIX ns:   <http://example.org/ns#>
                   PREFIX :     <http://example.org/ns#>
                   PREFIX xsd:  <http://www.w3.org/2001/XMLSchema#>
                   
                   :x   ns:p     "cat"@en .
                   :y   ns:p     "42"^^xsd:integer .
                   :z   ns:p     "abc"^^dt:specialDatatype .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          SELECT ?v WHERE { ?v ?p "cat" }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(0);


        queryString = """
                          SELECT ?v WHERE { ?v ?p "cat"@en }
                          """;
        answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
        var answer = answers.First();
        answer.Count.Should().Be(1);
        var actual = answer["v"];
        var expected = graph.GetResourceManager().CreateIriResource("http://example.org/ns#x");
        actual.Should().Be(expected);
    }

    /// <summary>
    /// Third example, int literals, in sparql 1.2 docs section 2.3.2
    /// </summary>
    [Fact]
    public void TestSparql4()
    {
        var data = """
                   PREFIX dt:   <http://example.org/datatype#>
                   PREFIX ns:   <http://example.org/ns#>
                   PREFIX :     <http://example.org/ns#>
                   PREFIX xsd:  <http://www.w3.org/2001/XMLSchema#>

                   :x   ns:p     "cat"@en .
                   :y   ns:p     "42"^^xsd:integer .
                   :z   ns:p     "abc"^^dt:specialDatatype .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          SELECT ?v WHERE { ?v ?p 42 }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
        var answer = answers.First();
        answer.Count.Should().Be(1);
        var actual = answer["v"];
        var expected = graph.GetResourceManager().CreateIriResource("http://example.org/ns#y");
        actual.Should().Be(expected);
    }


    /// <summary>
    /// Fifth example, custom datatypes, in sparql 1.2 docs section 2.3.3
    /// </summary>
    [Fact]
    public void TestSparql5()
    {
        var data = """
                   PREFIX dt:   <http://example.org/datatype#>
                   PREFIX ns:   <http://example.org/ns#>
                   PREFIX :     <http://example.org/ns#>
                   PREFIX xsd:  <http://www.w3.org/2001/XMLSchema#>

                   :x   ns:p     "cat"@en .
                   :y   ns:p     "42"^^xsd:integer .
                   :z   ns:p     "abc"^^dt:specialDatatype .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          SELECT ?v WHERE { ?v ?p "abc"^^<http://example.org/datatype#specialDatatype> }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
        var answer = answers.First();
        answer.Count.Should().Be(1);
        var actual = answer["v"];
        var expected = graph.GetResourceManager().CreateIriResource("http://example.org/ns#z");
        actual.Should().Be(expected);
    }

    /// <summary>
    /// Example, blank results nodes, in sparql 1.2 docs section 2.4
    /// </summary>
    [Fact]
    public void TestSparql6()
    {
        var data = """
                   PREFIX foaf:  <http://xmlns.com/foaf/0.1/>
                   
                   _:a  foaf:name   "Alice" .
                   _:b  foaf:name   "Bob" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf:   <http://xmlns.com/foaf/0.1/>
                          SELECT ?x ?name
                          WHERE  { ?x foaf:name ?name }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
        foreach (var answer in answers)
        {
            answer.Count.Should().Be(2);
            var actual = answer["name"];
            var alice = graph.GetResourceManager().CreateRdfStringLiteral("Alice");
            var bob = graph.GetResourceManager().CreateRdfStringLiteral("Bob");
            (actual.Equals(alice) || actual.Equals(bob)).Should().BeTrue();
            var actualX = answer["x"];
            actualX.Should().BeOfType<BlankNodeResource>();
        }
    }


    /// <summary>
    /// Example, creating values, in sparql 1.2 docs section 2.5
    /// </summary>
    [Fact(Skip = "See https://github.com/daghovland/DagSemTools/issues/86")]
    public void TestSparqlCreatingValues()
    {
        var data = """
                   PREFIX foaf:  <http://xmlns.com/foaf/0.1/>
                               
                   _:a  foaf:givenName   "John" .
                   _:a  foaf:surname  "Doe" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf:   <http://xmlns.com/foaf/0.1/>
                          SELECT ( CONCAT(?G, " ", ?S) AS ?name )
                          WHERE  { ?P foaf:givenName ?G ; foaf:surname ?S }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
        var answer = answers.First();
        answer.Count.Should().Be(1);
        var actual = answer["name"];
        actual.Should().BeOfType<RdfLiteral>();
        actual.ToString().Should().Be("John Doe");
    }


    /// <summary>
    /// Example, creating values with bind, in sparql 1.2 docs section 2.5
    /// </summary>
    [Fact(Skip = "See https://github.com/daghovland/DagSemTools/issues/86")]
    public void TestSparqlCreatingValuesBind()
    {
        var data = """
                   PREFIX foaf:  <http://xmlns.com/foaf/0.1/>
                               
                   _:a  foaf:givenName   "John" .
                   _:a  foaf:surname  "Doe" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf:   <http://xmlns.com/foaf/0.1/>
                          SELECT ?name
                          WHERE  { 
                              ?P foaf:givenName ?G ; 
                                 foaf:surname ?S 
                              BIND(CONCAT(?G, " ", ?S) AS ?name)
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
        var answer = answers.First();
        answer.Count.Should().Be(1);
        var actual = answer["name"];
        actual.Should().BeOfType<RdfLiteral>();
        actual.ToString().Should().Be("John Doe");
    }




    /// <summary>
    /// Example from sparql-1.2 spec, section 6.1
    /// </summary>
    [Fact]
    public void TestSparqlOptionalPatterns()
    {
        var data = """
                   PREFIX foaf:       <http://xmlns.com/foaf/0.1/>
                   PREFIX rdf:        <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                   _:a  rdf:type        foaf:Person .
                       _:a  foaf:name       "Alice" .
                   _:a  foaf:mbox       <mailto:alice@example.com> .
                   _:a  foaf:mbox       <mailto:alice@work.example> .
                   
                   _:b  rdf:type        foaf:Person .
                       _:b  foaf:name       "Bob" .
                   
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?name ?mbox
                          WHERE  {
                              ?x foaf:name  ?name .
                              OPTIONAL { ?x  foaf:mbox  ?mbox }
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(3);
        foreach (var answer in answers)
        {
            var actual = answer["name"];
            actual.ToString().Should().BeOneOf("Alice", "Bob");
        }
    }

    [Fact(Skip = "Select Expressions are not yet supported")]
    public void TestSparqlSelectExpressions()
    {
        var data = """
                   PREFIX dc:   <http://purl.org/dc/elements/1.1/>
                   PREFIX ns:   <http://example.org/ns#>
                   <http://example.org/book/book1> dc:title "SPARQL Tutorial" ; ns:price 42 ; ns:discount 0.2 .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX dc:   <http://purl.org/dc/elements/1.1/>
                          PREFIX ns:   <http://example.org/ns#>
                          SELECT ?title (?p*(1-?discount) AS ?price)
                          WHERE { ?book dc:title ?title ; ns:price ?p ; ns:discount ?discount }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
    }

    [Fact(Skip = "UNION is not yet supported")]
    public void TestSparqlUnion()
    {
        var data = """
                   PREFIX dc10:  <http://purl.org/dc/elements/1.0/>
                   PREFIX dc11:  <http://purl.org/dc/elements/1.1/>
                   _:a dc10:title "SPARQL 1.0" .
                   _:b dc11:title "SPARQL 1.1" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX dc10:  <http://purl.org/dc/elements/1.0/>
                          PREFIX dc11:  <http://purl.org/dc/elements/1.1/>
                          SELECT ?title
                          WHERE  { { ?book dc10:title ?title } UNION { ?book dc11:title ?title } }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
    }

    [Fact(Skip = "FILTER is not yet supported")]
    public void TestSparqlFilter()
    {
        var data = """
                   PREFIX dc:  <http://purl.org/dc/elements/1.1/>
                   PREFIX ns:  <http://example.org/ns#>
                   _:a ns:price 20 ; dc:title "Cheap Book" .
                   _:b ns:price 40 ; dc:title "Expensive Book" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX  dc:  <http://purl.org/dc/elements/1.1/>
                          PREFIX  ns:  <http://example.org/ns#>
                          SELECT  ?title ?price
                          WHERE   { ?x ns:price ?price .
                                    FILTER (?price < 30) .
                                    ?x dc:title ?title . }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
    }

    [Fact(Skip = "Subqueries are not yet supported")]
    public void TestSparqlSubquery()
    {
        var data = """
                   PREFIX : <http://people.example/>
                   :alice :knows :bob .
                   :bob :name "Bob" .
                   :alice :knows :carol .
                   :carol :name "Carol" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
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
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
    }

    [Fact(Skip = "VALUES is not yet supported")]
    public void TestSparqlValues()
    {
        var data = """
                   PREFIX dc:   <http://purl.org/dc/elements/1.1/>
                   PREFIX :     <http://example.org/book/>
                   :book1 dc:title "Book 1" .
                   :book2 dc:title "Book 2" .
                   :book3 dc:title "Book 3" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX dc:   <http://purl.org/dc/elements/1.1/> 
                          PREFIX :     <http://example.org/book/> 
                          SELECT ?book ?title
                          WHERE {
                             ?book dc:title ?title .
                             VALUES ?book { :book1 :book3 }
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
    }

    [Fact(Skip = "MINUS is not yet supported")]
    public void TestSparqlMinus()
    {
        var data = """
                   PREFIX : <http://example.org/>
                   :s1 :p :o1 .
                   :s1 :q :o2 .
                   :s2 :p :o3 .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX : <http://example.org/>
                          SELECT ?s
                          WHERE {
                            ?s :p ?o .
                            MINUS { ?s :q ?o2 }
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
    }

    [Fact(Skip = "Property Paths are not yet supported")]
    public void TestSparqlPropertyPath()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:mbox <mailto:alice@example.org> .
                   _:a foaf:knows _:b .
                   _:b foaf:name "Bob" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?name
                          WHERE {
                            ?x foaf:mbox <mailto:alice@example.org> .
                            ?x foaf:knows/foaf:name ?name .
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
    }

    [Fact(Skip = "EXISTS is not yet supported")]
    public void TestSparqlExists()
    {
        var data = """
                   PREFIX : <http://example.org/>
                   :alice :name "Alice" ; :mbox <mailto:alice@example.org> .
                   :bob :name "Bob" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX  :       <http://example.org/>
                          SELECT ?person
                          WHERE {
                            ?person :name ?name .
                            FILTER EXISTS { ?person :mbox ?mbox }
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
    }

    private IDataset ParseTurtleData(string data)
    {
        var writer = new StringWriter();
        var graph = TriGParser.Parse(data, writer);
        if (!string.IsNullOrEmpty(writer.ToString()))
        {
            output.WriteLine("Parser warnings/errors:");
            output.WriteLine(writer.ToString());
            Assert.Fail("Parser warnings/errors:");
        }

        return graph;
    }

    [Fact]
    public void TestSparqlAggregate()
    {
        var data = """
                   PREFIX : <http://books.example/>
                   :org1 :hasBook :book1 .
                   :book1 :price 10 .
                   :org1 :hasBook :book2 .
                   :book2 :price 20 .
                   :org2 :hasBook :book3 .
                   :book3 :price 30 .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX : <http://books.example/>
                          SELECT ?org (SUM(?lprice) AS ?totalPrice)
                          WHERE {
                            ?org :hasBook ?book .
                            ?book :price ?lprice .
                          }
                          GROUP BY ?org
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();

        Assert.NotNull(answers);
        answers.Count.Should().Be(2);

        var org1Result = answers.FirstOrDefault(a => a["org"].ToString().Contains("org1"));
        var org2Result = answers.FirstOrDefault(a => a["org"].ToString().Contains("org2"));

        org1Result.Should().NotBeNull();
        org2Result.Should().NotBeNull();

        org1Result["totalPrice"].ToString().Should().Be("IntegerLiteral(30)");
        org2Result["totalPrice"].ToString().Should().Be("IntegerLiteral(30)");
    }
}