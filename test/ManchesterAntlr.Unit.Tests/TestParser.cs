/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.OwlOntology;
using DagSemTools.Ingress;
using TestUtils;
using Xunit.Abstractions;
using FluentAssertions;
using IriTools;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.InMemory;

namespace DagSemTools.Manchester.Parser.Unit.Tests;

public class TestParser
{
    private readonly ITestOutputHelper _output;
    private TestOutputTextWriter _errorOutput;
    private static InMemorySink _inMemorySink = new InMemorySink();

    private ILogger _logger =
        new LoggerConfiguration()
            .WriteTo.Sink(_inMemorySink)
            .WriteTo.Console()
            .CreateLogger();


    public TestParser(ITestOutputHelper output)
    {
        _output = output;
        _errorOutput = new TestOutputTextWriter(output);
    }
    public List<Axiom> TestOntologyFile(string filename)
    {
        var parsedOntology = Manchester.Parser.Parser.ParseFile(filename, _errorOutput);
        return TestOntology(parsedOntology);
    }

    private List<Axiom> TestOntology(OntologyDocument parsedOntology)
    {
        parsedOntology.Should().NotBeNull();

        var prefixes = parsedOntology.Prefixes;
        var versionedOntology = parsedOntology.Ontology;
        prefixes.ToList().Should().Contain(prefixDeclaration.NewPrefixDefinition("ex", new IriReference("https://example.com/")));

        var ontologyIri = versionedOntology.TryGetOntologyIri();
        ontologyIri.Should().NotBeNull();
        ontologyIri.Should().Be(new IriReference("https://example.com/ontology"));

        var ontologyVersionIri = versionedOntology.TryGetOntologyVersionIri();
        ontologyVersionIri.Should().NotBeNull();
        ontologyVersionIri.Should().Be(new IriReference("https://example.com/ontology#1"));
        return parsedOntology.Ontology.Axioms.ToList();
    }


    public List<Axiom> TestOntology(string ontology)
    {
        var parsedOntology = Manchester.Parser.Parser.ParseString(ontology, _errorOutput);
        return TestOntology(parsedOntology);
    }

    [Fact]
    public void TestSmallestOntology()
    {
        var parsed = Manchester.Parser.Parser.ParseString("Ontology:", _errorOutput);
        parsed.Should().NotBeNull();
    }

    [Fact]
    public void TestErrorHandling()
    {
        var customErrorOutput = new TestOutputTextWriter(_output);
        var parsed = Manchester.Parser.Parser.ParseString("""
                                                          Prefix fam: <https://ex.com/owl2/families#>
                                                          Ontology: <https://ex.com/owl2/families>
                                                          Class: fam:Person
                                                          """, customErrorOutput);
        customErrorOutput.LastError.Should().Be("line 1:0 mismatched input 'Prefix' expecting {'Ontology:', 'Prefix:'}");

    }

    [Fact]
    public void TestLackingPrefixErrorHandling()
    {
        var customErrorOutput = new TestOutputTextWriter(_output);
        var parsed = Manchester.Parser.Parser.ParseString("""
                                                          Ontology: <https://ex.com/owl2/families>
                                                          Class: fam:Person
                                                          """, customErrorOutput);
        customErrorOutput.LastError.Should().Be("line 2:7 Prefix fam not defined.");

    }

    [Fact]
    public void TestOntologyWithIri()
    {
        var parsedOntology = Manchester.Parser.Parser.ParseString("Prefix: ex: <https://example.com/> Ontology: <https://example.com/ontology>", _errorOutput);
        parsedOntology.Should().NotBeNull();

        var prefixes = parsedOntology.Prefixes;

        var prefixDeclarations = prefixes.ToList();
        prefixDeclarations.Should().Contain(prefixDeclaration.NewPrefixDefinition("ex", new IriReference("https://example.com/")));

        var versionedOntology = parsedOntology.Ontology;
        var ontologyIri = versionedOntology.TryGetOntologyIri();
        ontologyIri.Should().NotBeNull();
        ontologyIri.Should().Be(new IriReference("https://example.com/ontology"));

        versionedOntology.TryGetOntologyIri().Should().Be(new IriReference("https://example.com/ontology"));
    }
    [Fact]
    public void TestOntologyWithVersonIri()
    {
        TestOntology("Prefix: ex: <https://example.com/> Ontology: <https://example.com/ontology> <https://example.com/ontology#1>");

    }

    [Fact]
    public void TestOntologyWithClass()
    {
        _ = TestOntology("""
                                        Prefix: ex: <https://example.com/> 
                                        Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                        Class: ex:Class
                                        """);
    }


    [Fact]
    public void TestWrongOntology()
    {
        var ontologyString = """
                         Prefax: ex: <https://example.com/> 
                         Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                         Class: ex:Class
                         """;
        var customErrorOutput = new TestOutputTextWriter(_output);
        var parsedOntology = Manchester.Parser.Parser.ParseString(ontologyString, customErrorOutput);
        customErrorOutput.LastError.Should().Be("line 1:0 mismatched input 'Prefax' expecting {'Ontology:', 'Prefix:'}");

    }

    [Fact]
    public void TestWrongOntology2()
    {
        var customErrorOutput = new TestOutputTextWriter(_output);
        var parsedOntology = Manchester.Parser.Parser.ParseString("""
                                                                  Prefix: ex: <https://example.com/> 
                                                                  Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                                                  Class: ax:Class
                                                                  """, customErrorOutput);
        customErrorOutput.LastError.Should().Be("line 3:7 Prefix ax not defined.");
    }

    [Fact]
    public void TestOntologyWithSubClass()
    {
        var tboxAxiomsList = TestOntology("""
                                            Prefix: ex: <https://example.com/> 
                                            Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                            Class: ex:Class
                                            SubClassOf: ex:SuperClass
                                            """
                                        ).Where(ax => ax.IsAxiomClassAxiom);
        tboxAxiomsList.Should().HaveCount(1);
        var inclusionAxiom = tboxAxiomsList.First();
        var (sub, sup) = GetSubClassAxiom(inclusionAxiom);
        sub.Should().Be(ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/Class"))));
        sup.Should().Be(ClassExpression.NewClassName(Iri.NewFullIri((new IriReference("https://example.com/SuperClass")))));
    }

    private static (ClassExpression sub, ClassExpression sup) GetSubClassAxiom(Axiom axiom)
    {
        axiom.IsAxiomClassAxiom.Should().BeTrue();
        var (sub, sup) = axiom switch
        {
            Axiom.AxiomClassAxiom claxs => claxs.Item switch
            {
                ClassAxiom.SubClassOf subAx => (subAx.Item2, subAx.Item3),
                _ => throw new ArgumentOutOfRangeException($"Test failure: Expected subClassAxiom, found: {claxs}")
            },
            _ => throw new Exception("Test failure")
        };
        return (sub, sup);
    }

    private static ClassExpression[] GetEqClassAxiom(Axiom axiom)
    => axiom switch
    {
        Axiom.AxiomClassAxiom claxs => claxs.Item switch
        {
            ClassAxiom.EquivalentClasses subAx => ListModule.ToArray(subAx.Item2),
            _ => throw new ArgumentOutOfRangeException($"Test failure: Expected EquivalentClasses, found: {claxs}")
        },
        _ => throw new Exception("Test failure")
    };

    private static (Individual individual, ClassExpression cls) GetClassAssertionAxiom(Axiom axiom)
    =>
        axiom switch
        {
            Axiom.AxiomAssertion claxs => claxs.Item switch
            {
                Assertion.ClassAssertion subAx => (subAx.Item3, subAx.Item2),
                _ => throw new ArgumentOutOfRangeException($"Test failure: Expected class assertion, but got {claxs}")
            },
            _ => throw new Exception("Test failure")
        };
    private static (ObjectPropertyExpression role, Individual left, Individual right) GetRoleAssertionAxiom(Axiom axiom)
        =>
            axiom switch
            {
                Axiom.AxiomAssertion claxs => claxs.Item switch
                {
                    Assertion.ObjectPropertyAssertion subAx => (subAx.Item2, subAx.Item3, subAx.Item4),
                    _ => throw new ArgumentOutOfRangeException("Test failure")
                },
                _ => throw new Exception("Test failure")
            };

    [Fact]
    public void TestOntologyWithSubClasses()
    {
        var tboxAxiomsList = TestOntology("""
                                               Prefix: ex: <https://example.com/> 
                                               Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                               Class: ex:Class
                                               SubClassOf: ex:SuperClass1, ex:SuperClass2
                                               """
        ).Where(ax => ax.IsAxiomClassAxiom).ToList();
        tboxAxiomsList.Should().HaveCount(2);
        foreach (var tboxAxiom in tboxAxiomsList)
        {
            var (sub, sup) = GetSubClassAxiom(tboxAxiom);
            sub.Should().Be(ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/Class"))));
            sup.Should().BeOneOf(
                ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/SuperClass1"))),
                ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/SuperClass2"))));
        }
    }
    [Fact]
    public void TestOntologyWithSubClassAndNegation()
    {
        var tboxAxiomsList = TestOntology("""
                                               Prefix: ex: <https://example.com/> 
                                               Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                               Class: ex:Class
                                               SubClassOf: not ex:SuperClass
                                               """
        ).Where(ax => ax.IsAxiomClassAxiom).ToList();
        tboxAxiomsList.Should().HaveCount(1);
        var (sub, sup) = GetSubClassAxiom(tboxAxiomsList[0]);
        sub.Should().Be(ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/Class"))));
        sup.Should().Be(ClassExpression.NewObjectComplementOf(ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/SuperClass")))));
    }

    [Fact]
    public void TestOntologyWithSubClassAndExistential()
    {
        var tboxAxiomsList = TestOntology("""
                                               Prefix: ex: <https://example.com/> 
                                               Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                               Class: ex:Class
                                               SubClassOf: ex:Role some ex:SuperClass
                                               """
        ).Where(ax => ax.IsAxiomClassAxiom).ToList();
        tboxAxiomsList.Should().HaveCount(1);
        var (sub, sup) = GetSubClassAxiom(tboxAxiomsList.First());
        sub.Should().Be(ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/Class"))));
        sup.Should().Be(ClassExpression.NewObjectSomeValuesFrom(ObjectPropertyExpression.NewNamedObjectProperty(Iri.NewFullIri(new IriReference("https://example.com/Role"))),
            ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/SuperClass")))));
    }

    [Fact]
    public void TestOntologyWithSubClassAndUniversal()
    {
        var tboxAxiomsList = TestOntology("""
                                               Prefix: ex: <https://example.com/> 
                                               Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                               Class: ex:Class
                                               SubClassOf: ex:Role only ex:SuperClass
                                               """
        ).Where(ax => ax.IsAxiomClassAxiom).ToList();
        tboxAxiomsList.Should().HaveCount(1);
        var (sub, sup) = GetSubClassAxiom(tboxAxiomsList[0]);
        sub.Should().Be(ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/Class"))));
        sup.Should().Be(ClassExpression.NewObjectAllValuesFrom(
            ObjectPropertyExpression.NewNamedObjectProperty(Iri.NewFullIri(new IriReference("https://example.com/Role"))),
            ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/SuperClass")))));
    }


    [Fact]
    public void TestOntologyWithEquivalentClass()
    {
        var tboxAxiomsList = TestOntology("""
                                        Prefix: ex: <https://example.com/> 
                                        Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                        Class: ex:Class
                                        EquivalentTo: ex:EqClass
                                        """
        ).Where(ax => ax.IsAxiomClassAxiom);

        tboxAxiomsList.Should().HaveCount(1);
        var eqClasses = GetEqClassAxiom(tboxAxiomsList.First());
        eqClasses.Should().Contain(ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/Class"))));
        eqClasses.Should().Contain(ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/EqClass"))));
    }


    [Fact]
    public void TestOntologyWithobjectProperty()
    {
        var tboxAxioms = TestOntology("""
                                           Prefix: ex: <https://example.com/> 
                                           Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                           ObjectProperty: ex:Role
                                           """
        );


    }

    [Fact]
    public void TestOntologyWithAboxAssertion()
    {
        var aboxAxioms = TestOntology("""
                                                        Prefix: ex: <https://example.com/> 
                                                        Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                                        Class: ex:Class
                                                            EquivalentTo: ex:EqClass
                                                        Individual: ex:ind1 
                                                            Types: ex:Class , ex:Class2
                                                        """
        ).Where(ax => ax.IsAxiomAssertion);

        aboxAxioms.Should().HaveCount(2);
        var (individual, cls) = GetClassAssertionAxiom(aboxAxioms.First());
        individual.Should().Be(Individual.NewNamedIndividual(Iri.NewFullIri(new IriReference("https://example.com/ind1"))));
        cls.Should().Be(ClassExpression.NewClassName(Iri.NewFullIri(new IriReference("https://example.com/Class"))));
    }


    [Fact]
    public void TestOntologyWithRoleAssertion()
    {
        var aboxAxioms = TestOntology("""
                                           Prefix: ex: <https://example.com/> 
                                           Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                           Class: ex:Class
                                               EquivalentTo: ex:EqClass
                                           Individual: ex:ind1 
                                               Facts: ex:Role ex:ind2
                                           """
        ).Where(ax => ax.IsAxiomAssertion);

        aboxAxioms.Should().HaveCount(1);
        var (role, left, right) = GetRoleAssertionAxiom(aboxAxioms.First());
        left.Should().Be(Individual.NewNamedIndividual(Iri.NewFullIri(new IriReference("https://example.com/ind1"))));
        right.Should().Be(Individual.NewNamedIndividual(Iri.NewFullIri(new IriReference("https://example.com/ind2"))));
        role.Should().Be(ObjectPropertyExpression.NewNamedObjectProperty(Iri.NewFullIri(new IriReference("https://example.com/Role"))));
    }


    [Fact]
    public void TestOntologyWithRoleandTypeAssertions()
    {
        var aboxAxioms = TestOntology("""
                                           Prefix: ex: <https://example.com/> 
                                           Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                           Class: ex:Class
                                               EquivalentTo: ex:EqClass
                                           Individual: ex:ind1 
                                               Facts: ex:Role ex:ind2
                                            Individual: ex:Ind2
                                                Types: ex:Class
                                           """
        ).Where(ax => ax.IsAxiomAssertion);

        aboxAxioms.Should().HaveCount(2);
    }

    [Fact]
    public void TestEmptyPrefix()
    {
        var _ = TestOntology("""
                                           Prefix: : <https://example.com/empty/>
                                           Prefix: ex: <https://example.com/> 
                                           Ontology: <https://example.com/ontology> <https://example.com/ontology#1>   
                                           Class: Teacher
                                           """
        );
    }

    [Fact]
    public void TestAlcExample()
    {
        var _ = TestOntology("""
                                  Prefix: : <https://example.com/empty/>
                                  Prefix: ex: <https://example.com/> 
                                  Ontology: <https://example.com/ontology> <https://example.com/ontology#1>   
                                  Class: Teacher 
                                    SubClassOf: 
                                        Person,
                                        teaches some Course
                                  Class: PGC SubClassOf: not Person
                                  Class: Person
                                    SubClassOf: teaches some Course
                                  """
        );
    }

    /// <summary>
    /// From "An introduction to Description Logic" by Baader, Horrocks, Sattler
    /// Page 70
    /// </summary>
    [Fact]
    public void TestAlcTableauExample()
    {
        var parsedOntology = Manchester.Parser.Parser.ParseString("""
                                                                    Prefix: : <http://example.com/>
                                                                  Ontology: 
                                                                  Individual: a 
                                                                      Types: A and s some F, B and s only F
                                                                  """, _errorOutput
        );
    }

    [Fact]
    public void TestAlcTableauFromFile()
    {
        var _ = TestOntologyFile("TestData/alctableauex.owl");
    }


    [Fact]
    public void TestOntologyWithManyRoleAssertions()
    {
        var parsedOntology = Manchester.Parser.Parser.ParseString("""
                                                                  Prefix: : <https://example.com/> 
                                                                  Ontology:  
                                                                  Individual: a 
                                                                      Facts: r b, s c
                                                                  """, _errorOutput
        );

        var aboxAxioms = parsedOntology.Ontology.Axioms.Where(ax => ax.IsAxiomAssertion);
        aboxAxioms.Should().HaveCount(2);
        foreach (var axiom in aboxAxioms)
        {
            var (role, left, right) = GetRoleAssertionAxiom(axiom);
            left.Should().Be(Individual.NewNamedIndividual(Iri.NewFullIri(new IriReference("https://example.com/a"))));
            right.Should().BeOneOf(Individual.NewNamedIndividual(Iri.NewFullIri(new IriReference("https://example.com/b"))),
                Individual.NewNamedIndividual(Iri.NewFullIri(new IriReference("https://example.com/c"))));
        }
    }

    [Fact(Skip = "Not implemented yet, See Issue https://github.com/daghovland/AlcTableau/issues/2")]
    public void TestDefinitionExample()
    {
        var parsedOntology = Manchester.Parser.Parser.ParseFile("TestData/def_example.owl", _errorOutput);
        var tboxAxioms = parsedOntology.Ontology.Axioms;
        tboxAxioms.Should().HaveCount(2);

    }



    [Fact]
    public void TestDataTypeFacet()
    {
        var parsedOntology = Manchester.Parser.Parser.ParseString("""
                                                                  Prefix: : <https://example.com/>
                                                                  Ontology: 
                                                                  Datatype: NegInt
                                                                    Annotations: rdfs:comment "Negative Integer"
                                                                    EquivalentTo: integer[< 0]   
                                                                  """, _errorOutput);
        var aboxAxioms = parsedOntology.Ontology.Axioms.Where(
            axiom => axiom.IsAxiomAssertion).ToList();
        aboxAxioms.Should().HaveCount(0);

    }

    [Fact]
    public void TestAnnotationsExample()
    {
        var parsedOntology = Manchester.Parser.Parser.ParseFile("TestData/annotations.owl", _errorOutput);
        var aboxAxioms = parsedOntology.Ontology.Axioms.Where(
            axiom => axiom.IsAxiomAssertion).ToList();
        aboxAxioms.Should().HaveCount(0);

    }

}