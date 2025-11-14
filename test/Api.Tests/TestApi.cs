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
        var ont = DagSemTools.Api.TurtleParser.Parse(ontology, outputWriter);

        Assert.NotNull(ont);
        var labels = ont.GetTriplesWithSubjectPredicate(
            new IriReference("http://dbpedia.org/datatype/FuelEfficiency"),
            new IriReference("http://www.w3.org/2000/01/rdf-schema#label"));
        labels.Count().Should().Be(1, "There is one label on fuel efficiency");
    }

    [Fact]
    public void TestAbbreviatedBlankNode()
    {
        var ontology = new FileInfo("TestData/abbreviated_blank_nodes.ttl");
        var ont = DagSemTools.Api.TurtleParser.Parse(ontology, outputWriter);
        Assert.NotNull(ont);


        var knows = ont.GetTriplesWithPredicate(new IriReference("http://xmlns.com/foaf/0.1/knows")).ToList();
        knows.Should().HaveCount(2);


        var name = ont.GetTriplesWithPredicate(new IriReference("http://xmlns.com/foaf/0.1/name")).ToList();
        name.Should().HaveCount(3);
        var isKnown = knows.First().Object;
        var bobHasName = name.Skip(1).First().Subject;
        isKnown.Should().Be(bobHasName);

        var mbox = ont.GetTriplesWithPredicate(new IriReference("http://xmlns.com/foaf/0.1/mbox"));

        mbox.Should().HaveCount(1);

        var eve = ont.GetTriplesWithPredicate(new IriReference("http://xmlns.com/foaf/0.1/name"))
            .Where(tr => tr.Object.Equals(RdfLiteral.StringRdfLiteral("Eve")));
        eve.Should().HaveCount(1);
    }

    [Fact]
    public void TestDatalogReasoning()
    {
        var ontology = new FileInfo("TestData/data.ttl");
        var ont = DagSemTools.Api.TurtleParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetTriplesWithPredicateObject(
            new IriReference("https://example.com/data#predicate"),
            new IriReference("https://example.com/data#object"));
        resultsData.Should().HaveCount(1);
        var resultsBefore = ont.GetTriplesWithPredicateObject(
            new IriReference("https://example.com/data#predicate"),
            new IriReference("https://example.com/data#object2"));
        resultsBefore.Should().BeEmpty();
        Assert.NotNull(ont);
        var datalogFile = new FileInfo("TestData/rules.datalog");
        ont.LoadDatalog(datalogFile);
        var resultsAfter = ont.GetTriplesWithPredicateObject(
            new IriReference("https://example.com/data#predicate"),
            new IriReference("https://example.com/data#object2"));
        resultsAfter.Should().HaveCount(1);
    }

    [Fact]
    public void TestA()
    {
        var ontology = new FileInfo("TestData/test2.ttl");
        var ont = DagSemTools.Api.TurtleParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetTriplesWithObject(
            new IriReference("http://example.com/data#property")).ToList();
        resultsData.Should().HaveCount(1);
        resultsData.First().Predicate.Should().Be(new IriReference(Namespaces.RdfType));

    }

    [Fact]
    public void TestDatalog2()
    {
        var ontology = new FileInfo("TestData/test2.ttl");
        var ont = DagSemTools.Api.TurtleParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetTriplesWithObject(
            new IriReference("http://example.com/data#property")).ToList();
        resultsData.Should().HaveCount(1);
        resultsData.First().Predicate.Should().Be(new IriReference(Namespaces.RdfType));

        var datalogFile = new FileInfo("TestData/test2.datalog");
        ont.LoadDatalog(datalogFile);

        resultsData = ont.GetTriplesWithObject(
            new IriReference("http://example.com/data#property")).ToList();
        resultsData.Should().HaveCount(3);

    }



    [Fact]
    public void TestDatalogStratified()
    {
        var ontology = new FileInfo("TestData/test_stratified.ttl");
        var ont = DagSemTools.Api.TurtleParser.Parse(ontology, outputWriter);
        var resultsData = ont.GetTriplesWithObject(
            new IriReference("http://example.com/data#Type")).ToList();
        resultsData.Should().HaveCount(1);
        resultsData.First().Predicate.Should().Be(new IriReference(Namespaces.RdfType));

        resultsData = ont.GetTriplesWithObject(
            new IriReference("http://example.com/data#Type3")).ToList();
        resultsData.Should().HaveCount(0);

        var datalogFile = new FileInfo("TestData/test_stratified.datalog");
        ont.LoadDatalog(datalogFile);

        resultsData = ont.GetTriplesWithObject(
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
        var expected = RdfLiteral.StringRdfLiteral("SPARQL Tutorial");
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
        var expected1 = RdfLiteral.StringRdfLiteral("Johnny Lee Outlaw");
        var expected2 = RdfLiteral.StringRdfLiteral("Peter Goodguy");
        (actual.Equals(expected1) || actual.Equals(expected2)).Should().BeTrue();
        var actualMbox = answer["mbox"];
        var expectedMbox1 = new IriResource(new IriReference("mailto:peter@example.org"));
        var expectedMbox2 = new IriResource(new IriReference("mailto:carol@example.org"));
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
        var expected = new IriResource(new IriReference("http://example.org/ns#x"));
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
        var expected = new IriResource(new IriReference("http://example.org/ns#y"));
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
        var expected = new IriResource(new IriReference("http://example.org/ns#z"));
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
            var alice = new RdfLiteral(DagSemTools.Ingress.RdfLiteral.NewLiteralString("Alice"));
            var bob = new RdfLiteral(DagSemTools.Ingress.RdfLiteral.NewLiteralString("Bob"));
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
        var expected = new RdfLiteral(DagSemTools.Ingress.RdfLiteral.NewLiteralString("John Doe"));
        actual.Should().Be(expected);
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
        var expected = new RdfLiteral(DagSemTools.Ingress.RdfLiteral.NewLiteralString("John Doe"));
        actual.Should().Be(expected);
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
            answer.Count.Should().Be(2);
            var actual = answer["name"];
            var alice = new RdfLiteral(DagSemTools.Ingress.RdfLiteral.NewLiteralString("Alice"));
            var bob = new RdfLiteral(DagSemTools.Ingress.RdfLiteral.NewLiteralString("Bob"));
            actual.Should().BeOneOf(alice, bob);
        }
    }
    private IGraph ParseTurtleData(string data)
    {
        var writer = new StringWriter();
        var graph = TurtleParser.Parse(data, writer);
        if (!string.IsNullOrEmpty(writer.ToString()))
        {
            output.WriteLine("Parser warnings/errors:");
            output.WriteLine(writer.ToString());
            Assert.Fail("Parser warnings/errors:");
        }

        return graph;
    }
}