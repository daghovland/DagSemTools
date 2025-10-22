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
using Newtonsoft.Json;
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
    public Datastore TestOntology(string ontology)
    {
        var tmpWriter = new StringWriter();
        var dstore = Parser.ParseString(ontology, tmpWriter);
        var output = tmpWriter.ToString();
        if (!string.IsNullOrEmpty(output))
        {
            _output.WriteLine("Parser output:");
            _output.WriteLine(output);
            Assert.Fail($"Parsing failed {output}");
        }
        return dstore;

    }

    [Fact]
    public void TestSingleTriple()
    {
        var ont = TestOntology("<http://example.org/subject> a <http://example.org/object> .");
        Assert.NotNull(ont);
        Assert.Equal(1u, ont.Triples.TripleCount);
        Assert.NotNull(ont.Triples.GetTriples());
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
        Assert.Equal(3u, ont.Resources.ResourceCount);
        Assert.Equal(1u, ont.Triples.TripleCount);
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
        ont.Triples.TripleCount.Should().Be(1);
        var subjectId = ont.Triples.GetTriples().First().subject;
        var subject = ont.GetGraphElement(subjectId);
        subject.Should().Be(GraphElement.NewNodeOrEdge(RdfResource.NewIri("http://one.example/subject2")));
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
        ont.Triples.TripleCount.Should().Be(1);
        ont.Triples.GetTriples().First().subject.Should().BeGreaterThanOrEqualTo(0);
        ont.Triples.GetTriples().First().predicate.Should().BeGreaterThanOrEqualTo(0);
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
        ont.Triples.TripleCount.Should().Be(1);
        var triple = ont.Triples.GetTriples().First();
        triple.subject.Should().BeGreaterThanOrEqualTo(0);
        triple.predicate.Should().BeGreaterThanOrEqualTo(0);
        triple.obj.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void TestMultipleIntegerObjects()
    {
        var ont = TestOntology("""
                @prefix : <http://example.org/> . 
                :subject :predicate 1, 2, 3 .
            """);
        Assert.NotNull(ont);
        ont.Triples.TripleCount.Should().Be(3);
    }


    [Fact]
    public void TestMultipleStringObjects()
    {
        var ont = TestOntology("""
                                   @prefix : <http://example.org/> . 
                                   :subject :predicate "string1", "string2", "3" .
                               """);
        Assert.NotNull(ont);
        ont.Triples.TripleCount.Should().Be(3);
    }

    [Fact]
    public void Test1()
    {
        var ontology = File.ReadAllText("TestData/example1.ttl");
        var ont = TestOntology(ontology);
        Assert.NotNull(ont);
    }

    [Fact]
    public void TestOnlyPrefixIri()
    {
        var ontology = File.ReadAllText("TestData/onlyPrefixIri.ttl");
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
        var knows = ont.GetGraphElementId(GraphElement.NewNodeOrEdge(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/knows"))));
        ont.GetTriplesWithPredicate(knows).Should().HaveCount(2);
        Assert.NotNull(ont);
    }


    [Fact]
    public void TestExample26()
    {
        var ontology = File.ReadAllText("TestData/example26.ttl");
        var ont = TestOntology(ontology);
        ont.Triples.TripleCount.Should().Be(4);
    }


    [Fact]
    public void TestExample27()
    {
        var ontology = File.ReadAllText("TestData/example27.ttl");
        var ont = TestOntology(ontology);
        ont.Triples.TripleCount.Should().Be(5);
    }



    [Fact]
    public void TestExample30()
    {
        var ontology = File.ReadAllText("TestData/example30.ttl");
        var ont = TestOntology(ontology);
        ont.Triples.TripleCount.Should().Be(7);
    }

    [Fact]
    public void TestReifiedTriple()
    {
        var ontology = File.ReadAllText("TestData/reified_triple.ttl");
        var ont = TestOntology(ontology);
        ont.Triples.TripleCount.Should().Be(2);
        var reifiedTriples = ont.GetReifiedTriplesWithPredicate(
            ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://www.example.org/jobTitle"))))
            .ToList();
        reifiedTriples.Should().HaveCount(1);
        var employee38 = reifiedTriples.First().subject;
        ont.GetTriplesWithSubject(employee38).Should().HaveCount(1);
    }

    [Fact]
    public void TestAnnotatedTriple()
    {
        var ontology = File.ReadAllText("TestData/annotated_triple.ttl");
        var ont = TestOntology(ontology);
        ont.Triples.TripleCount.Should().Be(3);
        ont.ReifiedTriples.QuadCount.Should().Be(1);
    }

    [Fact]
    public void TestTripleTerm()
    {
        var ontology = File.ReadAllText("TestData/triple_term.ttl");
        var ont = TestOntology(ontology);
        ont.Triples.TripleCount.Should().Be(3);
        var reifications = ont.GetTriplesWithPredicate(ont.GetGraphNodeId(RdfResource.NewIri(new IriReference(Namespaces.RdfReifies)))).ToList();
        reifications.Should().HaveCount(1);
        var tripleId = reifications.First().obj;
        ont.GetReifiedTriplesWithId(tripleId).Should().HaveCount(1);
    }


    [Fact]
    public void TestTripleTermWithIri()
    {
        var ontology = File.ReadAllText("TestData/reified_triple_with_iri.ttl");
        var ont = TestOntology(ontology);
        ont.Triples.TripleCount.Should().Be(2);
        ont.ReifiedTriples.QuadCount.Should().Be(1);
    }


    [Fact]
    public void TestTripleSubSetRestriction()
    {
        var ontology = File.ReadAllText("TestData/triple-subset-qualified-restriction.ttl");
        var ont = TestOntology(ontology);
        ont.Triples.TripleCount.Should().Be(16);
        ont.ReifiedTriples.QuadCount.Should().Be(0);
    }


    [Fact]
    public void TestCollection()
    {
        var ontology = File.ReadAllText("TestData/collections.ttl");
        var ont = TestOntology(ontology);
        ont.Triples.TripleCount.Should().Be(8);
        var rdfNilId = ont.GetGraphNodeId(RdfResource.NewIri(new IriReference(Namespaces.RdfNil)));
        ont.GetTriplesWithObject(rdfNilId)
            .Should().HaveCount(2);
        ont.GetTriplesWithPredicate(ont.GetGraphNodeId(RdfResource.NewIri(new IriReference(Namespaces.RdfFirst))))
            .Should().HaveCount(3);
        var listRestId = ont.GetGraphNodeId(RdfResource.NewIri((new IriReference(Namespaces.RdfRest))));

        var emptyListTriple = ont.GetTriplesWithSubject(
            ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://example.org/foo/subject2")))).Single();
        emptyListTriple.obj.Should().Be(rdfNilId);

        var listTriple = ont.GetTriplesWithSubject(
            ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://example.org/foo/subject1")))).Single();
        var listHead = listTriple.obj;
        var headTriples = ont.GetTriplesWithSubject(listHead);
        headTriples.Should().HaveCount(2);
        var secondListElement = ont.GetTriplesWithSubjectPredicate(listHead, listRestId).Single().obj;
        var thirdListElement = ont.GetTriplesWithSubjectPredicate(secondListElement, listRestId).Single().obj;
        var endListElement = ont.GetTriplesWithSubjectPredicate(thirdListElement, listRestId).Single().obj;
        endListElement.Should().Be(rdfNilId);

    }




    [Fact]
    public void TestBlankNodePropertyList()
    {
        var ontology = File.ReadAllText("TestData/blank_node_property_list.ttl");
        var ont = TestOntology(ontology);
        Assert.NotNull(ont);

        var knows = ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/knows")));
        var triplesWithKnows = ont.GetTriplesWithPredicate(knows).ToList();
        triplesWithKnows.Should().HaveCount(1);


        var name = ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name")));
        var triplesWithName = ont.GetTriplesWithPredicate(name).ToList();
        triplesWithName.Should().HaveCount(1);

        triplesWithKnows.First().obj.Should().Be(triplesWithName.First().subject);
    }
    [Fact]
    public void TestAbbreviatedBlankNode()
    {
        var ontology = File.ReadAllText("TestData/abbreviated_blank_nodes.ttl");
        var ont = TestOntology(ontology);
        Assert.NotNull(ont);

        var knows = ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/knows")));
        var triplesWithKnows = ont.GetTriplesWithPredicate(knows).ToList();
        triplesWithKnows.Should().HaveCount(2);


        var name = ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name")));
        var triplesWithName = ont.GetTriplesWithPredicate(name).ToList();
        triplesWithName.Should().HaveCount(3);

        triplesWithKnows.First().obj.Should().Be(triplesWithName.Skip(1).First().subject);

        var mbox = ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/mbox")));

        var triplesWithMail = ont.GetTriplesWithPredicate(mbox).ToList();
        triplesWithMail.Should().HaveCount(1);
        var ontTriples = ont.Triples.GetTriples().Select(tr => ont.GetResourceTriple(tr));

        var eve = ont
            .GetTriplesWithPredicate(ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name"))))
            .Where(tr => ont.GetGraphElement(tr.obj).literal.Equals(RdfLiteral.NewLiteralString("Eve")));
        eve.Should().HaveCount(1);


        var alice = ont
            .GetTriplesWithPredicate(ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name"))))
            .Where(tr => ont.GetGraphElement(tr.obj).literal.Equals(RdfLiteral.NewLiteralString("Alice")));
        alice.Should().HaveCount(1);


        ont.Triples.TripleCount.Should().Be(
            6);


        var ontologyExp = File.ReadAllText("TestData/abbreviated_blank_nodes_expanded.ttl");
        var ontexp = TestOntology(ontologyExp);
        Assert.NotNull(ontexp);

        var ontexpTriples = ontexp.Triples.GetTriples().Select(tr => ont.GetResourceTriple(tr));
        ontexpTriples.Should().BeEquivalentTo(ontTriples);
        ontexp.Triples.TripleCount.Should().Be(ont.Triples.TripleCount);
        ontexp.Resources.ResourceCount.Should().Be(ont.Resources.ResourceCount);

        var triplesWithKnowsE = ontexp.GetTriplesWithPredicate(knows).ToList();
        triplesWithKnowsE.Should().HaveCount(2);

    }

    [Fact]
    public void TestBlankNodePropertyList2()
    {
        var ont = Parser.ParseString("""
               PREFIX foaf: <http://xmlns.com/foaf/0.1/>
               
                [ foaf:name "Alice" ].
            """, _outputWriter);
        var alice = ont
            .GetTriplesWithPredicate(ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name"))))
            .Where(tr => ont.GetGraphElement(tr.obj).literal.Equals(RdfLiteral.NewLiteralString("Alice")));
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
        var knows = ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/knows")));
        ont.GetTriplesWithPredicate(knows).Should().HaveCount(2);
        Assert.NotNull(ont);
        var person2 = ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://example.org/person2")));
        ont.GetTriplesWithObjectPredicate(person2, knows).Should().HaveCount(1);
    }


    [Fact]
    public void TestSparqlExample2()
    {
        var ont = TestOntology("""
                               PREFIX foaf:  <http://xmlns.com/foaf/0.1/> .
                               
                               _:a  foaf:name   "Johnny Lee Outlaw" .
                               _:a  foaf:mbox   <mailto:jlow@example.com> .
                               _:b  foaf:name   "Peter Goodguy" .
                               _:b  foaf:mbox   <mailto:peter@example.org> .
                               _:c  foaf:mbox   <mailto:carol@example.org> .
                               """);
        Assert.NotNull(ont);
        var knows = ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name")));
        ont.GetTriplesWithPredicate(knows).Should().HaveCount(2);

        var person2 = ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/mbox")));
        ont.GetTriplesWithPredicate(knows).Should().HaveCount(2);
    }


    [Fact]
    public void TestPrefixBlankNode()
    {
        var ont = TestOntology("""
                               PREFIX ex:  <http://example.com#> .
                               _:a  ex:name   "Firstname" .
                               """);
        Assert.NotNull(ont);
        ont.Triples.TripleCount.Should().Be(1);
        var knows = ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://example.com#name")));
        ont.GetTriplesWithPredicate(knows).Should().HaveCount(1);

    }


    [Fact]
    public void TestLiteralObjects()
    {
        var ont = TestOntology("""
                                   PREFIX foaf:  <http://xmlns.com/foaf/0.1/> .

                                   _:a  foaf:name   "Johnny Lee Outlaw" .
                                   _:a  foaf:mbox   <mailto:jlow@example.com> .
                                   _:b  foaf:name   "Peter Goodguy" .
                                   _:b  foaf:mbox   <mailto:peter@example.org> .
                                   _:c  foaf:mbox   <mailto:carol@example.org> .
                               """);
        Assert.NotNull(ont);
        ont.Triples.TripleCount.Should().Be(5);
        var name = ont.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://xmlns.com/foaf/0.1/name")));
        var nameTriples = ont.GetTriplesWithPredicate(name).ToList();
        nameTriples.Should().HaveCount(2);
        var nameTripleResources = nameTriples.Select(ont.GetResourceTriple);
        nameTripleResources.First().obj.IsGraphLiteral.Should().BeTrue();

    }


    [Fact]
    public void TestHandlingWrongQuote()
    {
        var outputWriter = new StringWriter();
        Parser.ParseString("""
                                   PREFIX foaf:  <http://xmlns.com/foaf/0.1/> .

                                   _:a  foaf:name   \"Johnny Lee Outlaw" .
                                   _:a  foaf:mbox   <mailto:jlow@example.com> .
                                   _:b  foaf:name   \"Peter Goodguy" .
                                   _:b  foaf:mbox   <mailto:peter@example.org> .
                                   _:c  foaf:mbox   <mailto:carol@example.org> .
                               """, outputWriter);

        var output = outputWriter.ToString();
        output.Should().Contain("line 3:21 mismatched input '\\\"'");
    }

    [Fact]
    public void TestWholeStringMustBeParsed()
    {
        var outputWriter = new StringWriter();
        Parser.ParseString("""
                               PREFIX ex:  <http://example.com#> . ,
                               """, outputWriter);
        var output = outputWriter.ToString();
        output.Should().Contain("line 1:36 extraneous input ','");
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