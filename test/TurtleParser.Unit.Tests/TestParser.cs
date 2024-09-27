using AlcTableau;
using AlcTableau.Rdf;
using AlcTableau.TurtleAntlr;
using FluentAssertions;
using IriTools;
using TestUtils;
using Xunit.Abstractions;

namespace TurtleParser.Unit.Tests;

public class TestParser : IDisposable, IAsyncDisposable
{

    private ITestOutputHelper _output;
    private TextWriter _outputWriter;
    public TestParser(ITestOutputHelper output)
    {
        _output = output;
        _outputWriter = new TestOutputTextWriter(_output);
    }
    public TripleTable TestOntology(string ontology)
    {
        return AlcTableau.TurtleAntlr.Parser.ParseString(ontology, _outputWriter);

    }

    [Fact]
    public void TestSingleTriple()
    {
        var ont = TestOntology("<http://example.org/subject> a <http://example.org/object> .");
        Assert.NotNull(ont);
        Assert.Equal(1u, ont.TripleCount);
        Assert.NotNull(ont.TripleList);
    }

    [Fact]
    public void TestSpecExample21()
    {
        var ont = TestOntology(
            """
            <http://example.org/#spiderman>
            <http://www.perceive.net/schemas/relationship/enemyOf>
            <http://example.org/#green-goblin> .
            """);
        Assert.NotNull(ont);
        Assert.Equal(3u, ont.ResourceCount);
        Assert.Equal(1u, ont.TripleCount);
    }


    [Fact]
    public void TestSpecExamplePredicateList()
    {
        var ont = TestOntology(
            """
            <http://example.org/#spiderman> <http://www.perceive.net/schemas/relationship/enemyOf> <http://example.org/#green-goblin> ;
                <http://xmlns.com/foaf/0.1/name> "Spiderman" .
            """);
        Assert.NotNull(ont);
    }


    [Fact]
    public void TestDuplicateTriples()
    {
        var ont = TestOntology(
            """
            @base <http://one.example/> .
            <subject2> <predicate2> <object2> .     # relative IRI references, e.g., http://one.example/subject2
            <subject2> <predicate2> <object2> .     # relative IRI references, e.g., http://one.example/subject2
            
            """);
        Assert.NotNull(ont);
        ont.TripleCount.Should().Be(1);
        var subjectId = ont.TripleList[0].subject;
        var subjectIri = ont.ResourceList[subjectId].iri;
        subjectIri.Should().Be("http://one.example/subject2");
    }


    [Fact]
    public void TestPrefixCanBeUpdated()
    {
        var ont = TestOntology(
            """
            @prefix p: <http://two.example/> .
            PREFIX p: <http://two.example/>
            """);
        Assert.NotNull(ont);

    }

    [Fact]
    public void TestAllIriWritings()
    {
        var ont = TestOntology(
            """
            # A triple with all resolved IRIs
            <http://one.example/subject1> <http://one.example/predicate1> <http://one.example/object1> .

            @base <http://one.example/> .
            <subject2> <predicate2> <object2> .     # relative IRI references, e.g., http://one.example/subject2

            BASE <http://one.example/>
            <subject2> <predicate2> <object2> .     # relative IRI references, e.g., http://one.example/subject2

            @prefix p: <http://two.example/> .
            p:subject3 p:predicate3 p:object3 .     # prefixed name, e.g., http://two.example/subject3

            PREFIX p: <http://two.example/>
            p:subject3 p:predicate3 p:object3 .     # prefixed name, e.g., http://two.example/subject3

            @prefix p: <path/> .                    # prefix p: now stands for http://one.example/path/
            p:subject4 p:predicate4 p:object4 .     # prefixed name, e.g., http://one.example/path/subject4

            PrEfIx : <http://another.example/>       # empty prefix
            :subject5 :predicate5 :object5 .        # prefixed name, e.g., http://another.example/subject5

            :subject6 a :subject7 .                 # same as :subject6 <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> :subject7 .

            <http://伝言.example/?user=أ&channel=R%26D> a :subject8 . # a multi-script subject IRI .
            """);
        Assert.NotNull(ont);
    }

    [Fact]
    public void RelativePrefixWorksFine()
    {
        var ont = TestOntology("""
                               BASE <http://one.example/>
                               @prefix p: <path/> .                    # prefix p: now stands for http://one.example/path/
                               p:subject4 p:predicate4 p:object4 .     # prefixed name, e.g., http://one.example/path/subject4
                               """);
        ont.TripleCount.Should().Be(1);
        ont.TripleList[0].subject.Should().BeGreaterOrEqualTo(0);
        ont.TripleList[0].predicate.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void TestNumberLiterals()
    {
        var ont = TestOntology(
            """
                PREFIX : <http://example.org/elements/>
                <http://en.wikipedia.org/wiki/Helium>
                    :atomicNumber 2 ;               # xsd:integer
                    :atomicMass 4.002602 ;          # xsd:decimal
                    :specificGravity 1.663E-4 .     # xsd:double
            """);
        Assert.NotNull(ont);
    }

    [Fact]
    public void TestReleativeIris()
    {
        var ont = TestOntology(
            """
            @base <http://example.org/> .
            <#green-goblin> <#enemyOf> <#spiderman> .
            """);
        Assert.NotNull(ont);
    }

    [Fact]
    public void TestPrefixes()
    {
        var ont = TestOntology(
            """
            @prefix : <http://example.org/> .
            :spiderman :enemyOf :green-goblin .
            """);
        Assert.NotNull(ont);
        ont.TripleCount.Should().Be(1);
        var triple = ont.TripleList[0];
        triple.subject.Should().BeGreaterOrEqualTo(0);
        triple.predicate.Should().BeGreaterOrEqualTo(0);
        triple.@object.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void TestMultipleIntegerObjects()
    {
        var ont = TestOntology("""
                @prefix : <http://example.org/> . 
                :subject :predicate 1, 2, 3 .
            """);
        Assert.NotNull(ont);
        ont.TripleCount.Should().Be(3);
    }


    [Fact]
    public void TestMultipleStringObjects()
    {
        var ont = TestOntology("""
                                   @prefix : <http://example.org/> . 
                                   :subject :predicate "string1", "string2", "3" .
                               """);
        Assert.NotNull(ont);
        ont.TripleCount.Should().Be(3);
    }

    [Fact]
    public void Test1()
    {
        var ontology = File.ReadAllText("TestData/example1.ttl");
        var ont = TestOntology(ontology);
        Assert.NotNull(ont);
    }


    [Fact]
    public void TestQuotedLiterals()
    {
        var ontology = File.ReadAllText("TestData/quotedliterals.ttl");
        var ont = TestOntology(ontology);
        Assert.NotNull(ont);
    }

    [Fact]
    public void TestBooleans()
    {
        var ontology = File.ReadAllText("TestData/booleans.ttl");
        var ont = TestOntology(ontology);
        Assert.NotNull(ont);
    }

    [Fact]
    public void TestBlankNodes()
    {
        var ontology = File.ReadAllText("TestData/blank_nodes.ttl");
        var ont = TestOntology(ontology);
        var knows = ont.ResourceMap[RDFStore.Resource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/knows"))];
        ont.GetTriplesWithPredicate(knows).Should().HaveCount(2);
        Assert.NotNull(ont);
    }


    [Fact]
    public void TestExample26()
    {
        var ontology = File.ReadAllText("TestData/example26.ttl");
        var ont = TestOntology(ontology);
        ont.TripleCount.Should().Be(4);
    }


    [Fact]
    public void TestCollection()
    {
        var ontology = File.ReadAllText("TestData/collections.ttl");
        var ont = TestOntology(ontology);
        ont.TripleCount.Should().Be(8);
        ont.GetTriplesWithObject(ont.ResourceMap[RDFStore.Resource.NewIri(new IriReference(Namespaces.RdfNil))])
            .Should().HaveCount(2);
        ont.GetTriplesWithPredicate(ont.ResourceMap[RDFStore.Resource.NewIri(new IriReference(Namespaces.RdfFirst))])
            .Should().HaveCount(3);
    }


    
    
    [Fact]
    public void TestBlankNodePropertyList()
    {
        var ontology = File.ReadAllText("TestData/blank_node_property_list.ttl");
        var ont = TestOntology(ontology);
        Assert.NotNull(ont);

        var knows = ont.ResourceMap[RDFStore.Resource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/knows"))];
        var triplesWithKnows = ont.GetTriplesWithPredicate(knows).ToList();
        triplesWithKnows.Should().HaveCount(1);


        var name = ont.ResourceMap[RDFStore.Resource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name"))];
        var triplesWithName = ont.GetTriplesWithPredicate(name).ToList();
        triplesWithName.Should().HaveCount(1);

        triplesWithKnows.First().@object.Should().Be(triplesWithName.First().subject);
    }
    [Fact]
    public void TestAbbreviatedBlankNode()
    {
        var ontology = File.ReadAllText("TestData/abbreviated_blank_nodes.ttl");
        var ont = TestOntology(ontology);
        Assert.NotNull(ont);

        var knows = ont.ResourceMap[RDFStore.Resource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/knows"))];
        var triplesWithKnows = ont.GetTriplesWithPredicate(knows).ToList();
        triplesWithKnows.Should().HaveCount(2);


        var name = ont.ResourceMap[RDFStore.Resource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name"))];
        var triplesWithName = ont.GetTriplesWithPredicate(name).ToList();
        triplesWithName.Should().HaveCount(3);

        triplesWithKnows.First().@object.Should().Be(triplesWithName.Skip(1).First().subject);

        var mbox = ont.ResourceMap[RDFStore.Resource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/mbox"))];

        var triplesWithMail = ont.GetTriplesWithPredicate(mbox).ToList();
        triplesWithMail.Should().HaveCount(1);
        var ontTriples = ont.TripleList.Select(tr => ont.GetResourceTriple(tr));

        var eve = ont
            .GetTriplesWithPredicate(ont.ResourceMap[RDFStore.Resource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name"))])
            .Where(tr => ont.ResourceList[tr.@object].literal.Equals("Eve"));
        eve.Should().HaveCount(1);


        var alice = ont
            .GetTriplesWithPredicate(ont.ResourceMap[RDFStore.Resource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name"))])
            .Where(tr => ont.ResourceList[tr.@object].literal.Equals("Alice"));
        alice.Should().HaveCount(1);


        ont.TripleCount.Should().Be(
            6);


        var ontologyExp = File.ReadAllText("TestData/abbreviated_blank_nodes_expanded.ttl");
        var ontexp = TestOntology(ontologyExp);
        Assert.NotNull(ontexp);

        var ontexpTriples = ontexp.TripleList.Select(tr => ont.GetResourceTriple(tr));
        ontexpTriples.Should().BeEquivalentTo(ontTriples);
        ontexp.TripleCount.Should().Be(ont.TripleCount);
        ontexp.ResourceCount.Should().Be(ont.ResourceCount);

        var triplesWithKnowsE = ontexp.GetTriplesWithPredicate(knows).ToList();
        triplesWithKnowsE.Should().HaveCount(2);

    }

    [Fact]
    public void TestBlankNodePropertyList2()
    {
        var ont = AlcTableau.TurtleAntlr.Parser.ParseString("""
               PREFIX foaf: <http://xmlns.com/foaf/0.1/>
               
                [ foaf:name "Alice" ].
            """, _outputWriter);
        var alice = ont
            .GetTriplesWithPredicate(ont.ResourceMap[RDFStore.Resource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name"))])
            .Where(tr => ont.ResourceList[tr.@object].literal.Equals("Alice"));
        alice.Should().HaveCount(1);

    }

    [Fact]
    public void TestBlankNodes2()
    {
        var ont = TestOntology("""
                                    prefix foaf: <http://xmlns.com/foaf/0.1/>
                                    prefix : <http://example.org/>
                                    [] foaf:knows :person1, :person2 .
                                    """);
        var knows = ont.ResourceMap[RDFStore.Resource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/knows"))];
        ont.GetTriplesWithPredicate(knows).Should().HaveCount(2);
        Assert.NotNull(ont);
        var person2 = ont.ResourceMap[RDFStore.Resource.NewIri(new IriReference("http://example.org/person2"))];
        ont.GetTriplesWithObjectPredicate(person2, knows).Should().HaveCount(1);
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