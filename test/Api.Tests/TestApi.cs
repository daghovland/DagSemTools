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

    [Fact]
    public void TestSparqlBasicJoin()
    {
        var data = """
                   PREFIX ns:  <http://example.org/ns#>
                   _:a ns:price 20 .
                   _:a ns:title "Cheap Book" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX ns:  <http://example.org/ns#>
                          SELECT ?title ?price
                          WHERE { 
                            ?x ns:price ?price .
                            ?x ns:title ?title .
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        answers.Should().HaveCount(1);
    }

    [Fact]
    public void TestSparqlBind()
    {
        var data = """
                   PREFIX ns:  <http://example.org/ns#>
                   _:a ns:price 20 .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX ns:  <http://example.org/ns#>
                          SELECT ?price ?double
                          WHERE { 
                            ?x ns:price ?price .
                            BIND(?price AS ?double)
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        answers.Should().HaveCount(1);
        answers[0]["double"].ToString().Should().Be(answers[0]["price"].ToString());
    }

    [Fact]
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
                                    ?x dc:title ?title .
                                  }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        answers.Should().HaveCount(2);

        queryString = """
                          PREFIX  dc:  <http://purl.org/dc/elements/1.1/>
                          PREFIX  ns:  <http://example.org/ns#>
                          SELECT  ?title ?price
                          WHERE   { ?x ns:price ?price .
                                    ?x dc:title ?title .
                                    FILTER (?price < 30) .
                                  }
                          """;
        answers = graph.AnswerSelectQuery(queryString).ToList();
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

    /// <summary>
    /// SPARQL 1.2 spec §6.2: OPTIONAL with inner FILTER
    /// </summary>
    [Fact(Skip = "FILTER inside OPTIONAL not yet evaluated")]
    public void TestSparqlOptionalWithFilter()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:name "Alice" ; foaf:mbox <mailto:alice@work.example> .
                   _:b foaf:name "Bob" ; foaf:mbox <mailto:bob@home.example> .
                   _:c foaf:name "Carol" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?name ?mbox
                          WHERE {
                            ?x foaf:name ?name .
                            OPTIONAL { ?x foaf:mbox ?mbox
                                       FILTER regex(str(?mbox), "@work") }
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        // Carol has no mbox, Alice's mbox matches @work, Bob's mbox does not match
        answers.Count.Should().Be(3);
        var aliceAnswer = answers.Single(a => a["name"].ToString() == "Alice");
        aliceAnswer.ContainsKey("mbox").Should().BeTrue();
        var bobAnswer = answers.Single(a => a["name"].ToString() == "Bob");
        bobAnswer.ContainsKey("mbox").Should().BeFalse();
    }

    /// <summary>
    /// SPARQL 1.2 spec §6.3: Multiple OPTIONAL blocks
    /// </summary>
    [Fact(Skip = "Multiple OPTIONAL not yet fully evaluated")]
    public void TestSparqlMultipleOptionals()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:name "Alice" ; foaf:mbox <mailto:alice@example.org> ; foaf:homepage <http://alice.example.org/> .
                   _:b foaf:name "Bob" ; foaf:mbox <mailto:bob@example.org> .
                   _:c foaf:name "Carol" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?name ?mbox ?hpage
                          WHERE {
                            ?x foaf:name  ?name .
                            OPTIONAL { ?x foaf:mbox  ?mbox } .
                            OPTIONAL { ?x foaf:homepage ?hpage }
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(3);
        var aliceAnswer = answers.Single(a => a["name"].ToString() == "Alice");
        aliceAnswer.ContainsKey("mbox").Should().BeTrue();
        aliceAnswer.ContainsKey("hpage").Should().BeTrue();
        var bobAnswer = answers.Single(a => a["name"].ToString() == "Bob");
        bobAnswer.ContainsKey("mbox").Should().BeTrue();
        bobAnswer.ContainsKey("hpage").Should().BeFalse();
        var carolAnswer = answers.Single(a => a["name"].ToString() == "Carol");
        carolAnswer.ContainsKey("mbox").Should().BeFalse();
    }

    /// <summary>
    /// SPARQL 1.2 spec §8.1: FILTER NOT EXISTS
    /// </summary>
    [Fact(Skip = "NOT EXISTS is not yet implemented")]
    public void TestSparqlNotExists()
    {
        var data = """
                   PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   <http://example.org/alice> rdf:type foaf:Person ; foaf:name "Alice" .
                   <http://example.org/bob> rdf:type foaf:Person .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?person
                          WHERE {
                            ?person rdf:type foaf:Person .
                            FILTER NOT EXISTS { ?person foaf:name ?name }
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        // Only bob has no foaf:name
        answers.Count.Should().Be(1);
        answers[0]["person"].ToString().Should().Contain("bob");
    }

    /// <summary>
    /// SPARQL 1.2 spec §9.3.2: Property path alternative using |
    /// </summary>
    [Fact(Skip = "Property path alternatives are not yet implemented")]
    public void TestSparqlPathAlternative()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                   <http://example.org/alice> foaf:name "Alice" .
                   <http://example.org/bob> rdfs:label "Bob" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
                          SELECT ?x ?name
                          WHERE {
                            ?x foaf:name|rdfs:label ?name .
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
    }

    /// <summary>
    /// SPARQL 1.2 spec §9.4: Inverse property path using ^
    /// </summary>
    [Fact(Skip = "Inverse property paths are not yet implemented")]
    public void TestSparqlPathInverse()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   <http://example.org/alice> foaf:knows <http://example.org/bob> .
                   <http://example.org/alice> foaf:knows <http://example.org/carol> .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?x
                          WHERE {
                            ?x ^foaf:knows <http://example.org/alice> .
                          }
                          """;
        // Inverse: ?x is known-by alice, i.e. alice foaf:knows ?x
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
    }

    /// <summary>
    /// SPARQL 1.2 spec §9.5: One-or-more path (+)
    /// </summary>
    [Fact(Skip = "Arbitrary length property paths are not yet implemented")]
    public void TestSparqlPathOneOrMore()
    {
        var data = """
                   PREFIX : <http://example.org/>
                   :a :knows :b .
                   :b :knows :c .
                   :c :knows :d .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX : <http://example.org/>
                          SELECT ?x
                          WHERE { :a :knows+ ?x }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        // Transitively reachable: b, c, d
        answers.Count.Should().Be(3);
    }

    /// <summary>
    /// SPARQL 1.2 spec §9.5: Zero-or-more path (*)
    /// </summary>
    [Fact(Skip = "Arbitrary length property paths are not yet implemented")]
    public void TestSparqlPathZeroOrMore()
    {
        var data = """
                   PREFIX : <http://example.org/>
                   :a :subClassOf :b .
                   :b :subClassOf :c .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX : <http://example.org/>
                          SELECT ?x
                          WHERE { :a :subClassOf* ?x }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        // Zero or more: a (self), b, c
        answers.Count.Should().Be(3);
    }

    /// <summary>
    /// SPARQL 1.2 spec §15.1: ORDER BY — results should be ordered
    /// </summary>
    [Fact(Skip = "ORDER BY is not yet implemented")]
    public void TestSparqlOrderBy()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:name "Charlie" .
                   _:b foaf:name "Alice" .
                   _:c foaf:name "Bob" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?name
                          WHERE { ?x foaf:name ?name }
                          ORDER BY ?name
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(3);
        var names = answers.Select(a => a["name"].ToString()).ToList();
        names.Should().BeInAscendingOrder();
    }

    /// <summary>
    /// SPARQL 1.2 spec §15.1: ORDER BY DESC
    /// </summary>
    [Fact(Skip = "ORDER BY is not yet implemented")]
    public void TestSparqlOrderByDesc()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:name "Charlie" .
                   _:b foaf:name "Alice" .
                   _:c foaf:name "Bob" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?name
                          WHERE { ?x foaf:name ?name }
                          ORDER BY DESC(?name)
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(3);
        var names = answers.Select(a => a["name"].ToString()).ToList();
        names.Should().BeInDescendingOrder();
    }

    /// <summary>
    /// SPARQL 1.2 spec §15.3: LIMIT
    /// </summary>
    [Fact(Skip = "LIMIT is not yet implemented")]
    public void TestSparqlLimit()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:name "Alice" .
                   _:b foaf:name "Bob" .
                   _:c foaf:name "Carol" .
                   _:d foaf:name "Dave" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?name
                          WHERE { ?x foaf:name ?name }
                          LIMIT 2
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
    }

    /// <summary>
    /// SPARQL 1.2 spec §15.4: OFFSET
    /// </summary>
    [Fact(Skip = "OFFSET is not yet implemented")]
    public void TestSparqlOffset()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:name "Alice" .
                   _:b foaf:name "Bob" .
                   _:c foaf:name "Carol" .
                   _:d foaf:name "Dave" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?name
                          WHERE { ?x foaf:name ?name }
                          ORDER BY ?name
                          LIMIT 2
                          OFFSET 2
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
        var names = answers.Select(a => a["name"].ToString()).ToList();
        names.Should().Contain("Carol");
        names.Should().Contain("Dave");
    }

    /// <summary>
    /// SPARQL 1.2 spec §15.5: DISTINCT modifier removes duplicate rows
    /// </summary>
    [Fact(Skip = "DISTINCT is not yet implemented")]
    public void TestSparqlDistinct()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:name "Alice" ; foaf:nick "Ali" .
                   _:b foaf:name "Alice" ; foaf:nick "Allie" .
                   _:c foaf:name "Bob" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT DISTINCT ?name
                          WHERE { ?x foaf:name ?name }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        // Two subjects have name "Alice" but DISTINCT collapses them
        answers.Count.Should().Be(2);
    }

    /// <summary>
    /// SPARQL 1.2 spec §11.2: HAVING clause filters aggregate results
    /// </summary>
    [Fact(Skip = "HAVING is not yet implemented")]
    public void TestSparqlHaving()
    {
        var data = """
                   PREFIX : <http://books.example/>
                   :org1 :hasBook :book1 .
                   :book1 :price 5 .
                   :org1 :hasBook :book2 .
                   :book2 :price 8 .
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
                          HAVING (SUM(?lprice) > 20)
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        // org1 total = 13 (filtered out), org2 total = 30 (kept)
        answers.Count.Should().Be(1);
        answers[0]["totalPrice"].ToString().Should().Be("IntegerLiteral(30)");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.2: IN expression
    /// </summary>
    [Fact(Skip = "IN expression is not yet evaluated")]
    public void TestSparqlFilterIn()
    {
        var data = """
                   PREFIX dc: <http://purl.org/dc/elements/1.1/>
                   PREFIX : <http://example.org/book/>
                   :book1 dc:title "SPARQL Tutorial" .
                   :book2 dc:title "The Semantic Web" .
                   :book3 dc:title "Programming .NET" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX dc: <http://purl.org/dc/elements/1.1/>
                          SELECT ?title
                          WHERE {
                            ?book dc:title ?title .
                            FILTER (?title IN ("SPARQL Tutorial", "The Semantic Web"))
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.2: NOT IN expression
    /// </summary>
    [Fact(Skip = "NOT IN expression is not yet evaluated")]
    public void TestSparqlFilterNotIn()
    {
        var data = """
                   PREFIX dc: <http://purl.org/dc/elements/1.1/>
                   PREFIX : <http://example.org/book/>
                   :book1 dc:title "SPARQL Tutorial" .
                   :book2 dc:title "The Semantic Web" .
                   :book3 dc:title "Programming .NET" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX dc: <http://purl.org/dc/elements/1.1/>
                          SELECT ?title
                          WHERE {
                            ?book dc:title ?title .
                            FILTER (?title NOT IN ("SPARQL Tutorial"))
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.4.3: REGEX string filter
    /// </summary>
    [Fact(Skip = "REGEX function is not yet evaluated")]
    public void TestSparqlFilterRegex()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:name "Alice" .
                   _:b foaf:name "Bob" .
                   _:c foaf:name "Alicia" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?name
                          WHERE {
                            ?x foaf:name ?name .
                            FILTER regex(?name, "^Al", "i")
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
        var names = answers.Select(a => a["name"].ToString()).ToList();
        names.Should().Contain("Alice");
        names.Should().Contain("Alicia");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.4.1: BOUND function
    /// </summary>
    [Fact(Skip = "BOUND function is not yet evaluated")]
    public void TestSparqlBound()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:name "Alice" ; foaf:mbox <mailto:alice@example.org> .
                   _:b foaf:name "Bob" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?name
                          WHERE {
                            ?x foaf:name ?name .
                            OPTIONAL { ?x foaf:mbox ?mbox }
                            FILTER (BOUND(?mbox))
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        // Only Alice has mbox
        answers.Count.Should().Be(1);
        answers[0]["name"].ToString().Should().Be("Alice");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.4.1: IF function
    /// </summary>
    [Fact(Skip = "IF function is not yet evaluated")]
    public void TestSparqlIf()
    {
        var data = """
                   PREFIX ns: <http://example.org/ns#>
                   <http://example.org/s1> ns:value 5 .
                   <http://example.org/s2> ns:value 15 .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX ns: <http://example.org/ns#>
                          SELECT ?s (IF(?x > 10, "big", "small") AS ?size)
                          WHERE { ?s ns:value ?x }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
        var s1 = answers.Single(a => a["s"].ToString().Contains("s1"));
        s1["size"].ToString().Should().Be("small");
        var s2 = answers.Single(a => a["s"].ToString().Contains("s2"));
        s2["size"].ToString().Should().Be("big");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.4.1: COALESCE returns first non-error argument
    /// </summary>
    [Fact(Skip = "COALESCE function is not yet evaluated")]
    public void TestSparqlCoalesce()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:name "Alice" ; foaf:mbox <mailto:alice@example.org> .
                   _:b foaf:name "Bob" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?name (COALESCE(str(?mbox), "no email") AS ?contact)
                          WHERE {
                            ?x foaf:name ?name .
                            OPTIONAL { ?x foaf:mbox ?mbox }
                          }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
        var bobAnswer = answers.Single(a => a["name"].ToString() == "Bob");
        bobAnswer["contact"].ToString().Should().Be("no email");
    }

    /// <summary>
    /// SPARQL 1.2 spec §13.3: GRAPH pattern for named graph query
    /// </summary>
    [Fact(Skip = "Named graph GRAPH patterns are not yet implemented")]
    public void TestSparqlNamedGraph()
    {
        var data = """
                   @prefix foaf: <http://xmlns.com/foaf/0.1/> .
                   <http://example.org/graph1> {
                     <http://example.org/alice> foaf:name "Alice" .
                   }
                   <http://example.org/graph2> {
                     <http://example.org/bob> foaf:name "Bob" .
                   }
                   """;
        var dataset = TriGParser.Parse(data, new StringWriter());
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?graph ?name
                          WHERE {
                            GRAPH ?graph {
                              ?x foaf:name ?name .
                            }
                          }
                          """;
        var answers = dataset.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(2);
    }

    /// <summary>
    /// SPARQL 1.2 spec §13.3: GRAPH with specific named graph IRI
    /// </summary>
    [Fact(Skip = "Named graph GRAPH patterns are not yet implemented")]
    public void TestSparqlSpecificNamedGraph()
    {
        var data = """
                   @prefix foaf: <http://xmlns.com/foaf/0.1/> .
                   <http://example.org/graph1> {
                     <http://example.org/alice> foaf:name "Alice" .
                   }
                   <http://example.org/graph2> {
                     <http://example.org/bob> foaf:name "Bob" .
                   }
                   """;
        var dataset = TriGParser.Parse(data, new StringWriter());
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT ?name
                          WHERE {
                            GRAPH <http://example.org/graph1> {
                              ?x foaf:name ?name .
                            }
                          }
                          """;
        var answers = dataset.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
        answers[0]["name"].ToString().Should().Be("Alice");
    }

    /// <summary>
    /// SPARQL 1.2 spec §11.3: COUNT aggregate without GROUP BY
    /// </summary>
    [Fact(Skip = "COUNT aggregate evaluation is not yet verified")]
    public void TestSparqlCount()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:name "Alice" .
                   _:b foaf:name "Bob" .
                   _:c foaf:name "Carol" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT (COUNT(?x) AS ?count)
                          WHERE { ?x foaf:name ?name }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
        answers[0]["count"].ToString().Should().Be("IntegerLiteral(3)");
    }

    /// <summary>
    /// SPARQL 1.2 spec §17.4.3: CONCAT string function in SELECT
    /// </summary>
    [Fact(Skip = "CONCAT function is not yet evaluated")]
    public void TestSparqlConcat()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:givenName "John" ; foaf:familyName "Doe" .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT (CONCAT(?gn, " ", ?fn) AS ?name)
                          WHERE { ?x foaf:givenName ?gn ; foaf:familyName ?fn }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
        answers[0]["name"].ToString().Should().Be("John Doe");
    }

    /// <summary>
    /// SPARQL 1.2 spec §16.1.1: SELECT * wildcard projects all variables
    /// </summary>
    [Fact(Skip = "SELECT * wildcard projection is not yet implemented")]
    public void TestSparqlSelectStar()
    {
        var data = """
                   PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                   _:a foaf:name "Alice" ; foaf:mbox <mailto:alice@example.org> .
                   """;
        var graph = ParseTurtleData(data);
        var queryString = """
                          PREFIX foaf: <http://xmlns.com/foaf/0.1/>
                          SELECT *
                          WHERE { ?x foaf:name ?name ; foaf:mbox ?mbox }
                          """;
        var answers = graph.AnswerSelectQuery(queryString).ToList();
        Assert.NotNull(answers);
        answers.Count.Should().Be(1);
        var answer = answers[0];
        answer.ContainsKey("name").Should().BeTrue();
        answer.ContainsKey("mbox").Should().BeTrue();
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