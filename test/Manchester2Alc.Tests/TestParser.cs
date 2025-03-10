/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.AlcTableau;
using DagSemTools.Ingress;
using TestUtils;
using Xunit.Abstractions;
using FluentAssertions;
using IriTools;
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
    public (List<ALC.TBoxAxiom>, List<ALC.ABoxAssertion>) TestOntologyFile(string filename)
    {
        var parsedOntology = Manchester.Parser.Parser.ParseFile(filename, _errorOutput);
        var alcOntology = DagSemTools.OWL2ALC.Translator.translateDocument(_logger, parsedOntology);
        return TestOntology(alcOntology);
    }

    private (List<ALC.TBoxAxiom>, List<ALC.ABoxAssertion>) TestOntology(ALC.OntologyDocument parsedOntology)
    {
        parsedOntology.Should().NotBeNull();

        var (prefixes, versionedOntology, KB) = parsedOntology.TryGetOntology();

        prefixes.ToList().Should().Contain(prefixDeclaration.NewPrefixDefinition("ex", new IriReference("https://example.com/")));

        var ontologyIri = versionedOntology.TryGetOntologyIri();
        ontologyIri.Should().NotBeNull();
        ontologyIri.Should().Be(new IriReference("https://example.com/ontology"));

        var ontologyVersionIri = versionedOntology.TryGetOntologyVersionIri();
        ontologyVersionIri.Should().NotBeNull();
        ontologyVersionIri.Should().Be(new IriReference("https://example.com/ontology#1"));
        return (KB.Item1.ToList(), KB.Item2.ToList());
    }


    public (List<ALC.TBoxAxiom>, List<ALC.ABoxAssertion>) TestOntology(string ontology)
    {
        var parsedOntology = Manchester.Parser.Parser.ParseString(ontology, _errorOutput);
        var alcOntology = DagSemTools.OWL2ALC.Translator.translateDocument(_logger, parsedOntology);
        return TestOntology(alcOntology);
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
        var parsedOwlOntology = Manchester.Parser.Parser.ParseString("Prefix: ex: <https://example.com/> Ontology: <https://example.com/ontology>", _errorOutput);
        var parsedOntology = DagSemTools.OWL2ALC.Translator.translateDocument(_logger, parsedOwlOntology);
        parsedOntology.Should().NotBeNull();

        var (prefixes, versionedOntology, KB) = parsedOntology.TryGetOntology();

        var prefixDeclarations = prefixes.ToList();
        prefixDeclarations.Should().Contain(prefixDeclaration.NewPrefixDefinition("ex", new IriReference("https://example.com/")));

        var ontologyIri = versionedOntology.TryGetOntologyIri();
        ontologyIri.Should().NotBeNull();
        ontologyIri.Should().Be(new IriReference("https://example.com/ontology"));

        var namedOntology = (ontologyVersion.NamedOntology)versionedOntology;
        namedOntology.OntologyIri.Should().Be(new IriReference("https://example.com/ontology"));
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
        var (tboxAxiomsList, _) = TestOntology("""
                                            Prefix: ex: <https://example.com/> 
                                            Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                            Class: ex:Class
                                            SubClassOf: ex:SuperClass
                                            """
                                        );
        tboxAxiomsList.Should().HaveCount(1);
        tboxAxiomsList[0].Should().BeOfType<ALC.TBoxAxiom>();
        var inclusion = (ALC.TBoxAxiom)tboxAxiomsList[0];
        inclusion.Sub.Should().Be(ALC.Concept.NewConceptName(new IriReference("https://example.com/Class")));
        inclusion.Sup.Should().Be(ALC.Concept.NewConceptName(new IriReference("https://example.com/SuperClass")));
    }


    [Fact]
    public void TestOntologyWithSubClasses()
    {
        var (tboxAxiomsList, _) = TestOntology("""
                                               Prefix: ex: <https://example.com/> 
                                               Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                               Class: ex:Class
                                               SubClassOf: ex:SuperClass1, ex:SuperClass2
                                               """
        );
        tboxAxiomsList.Should().HaveCount(2);
        foreach (var tboxAxiom in tboxAxiomsList)
        {
            tboxAxiom.Should().BeOfType<ALC.TBoxAxiom>();
            var inclusion = (ALC.TBoxAxiom)tboxAxiom;
            inclusion.Sub.Should().Be(ALC.Concept.NewConceptName(new IriReference("https://example.com/Class")));
            inclusion.Sup.Should().BeOneOf(
                ALC.Concept.NewConceptName(new IriReference("https://example.com/SuperClass1")),
                ALC.Concept.NewConceptName(new IriReference("https://example.com/SuperClass2")));
        }
    }
    [Fact]
    public void TestOntologyWithSubClassAndNegation()
    {
        var (tboxAxiomsList, _) = TestOntology("""
                                               Prefix: ex: <https://example.com/> 
                                               Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                               Class: ex:Class
                                               SubClassOf: not ex:SuperClass
                                               """
        );
        tboxAxiomsList.Should().HaveCount(1);
        tboxAxiomsList[0].Should().BeOfType<ALC.TBoxAxiom>();
        var inclusion = (ALC.TBoxAxiom)tboxAxiomsList[0];
        inclusion.Sub.Should().Be(ALC.Concept.NewConceptName(new IriReference("https://example.com/Class")));
        inclusion.Sup.Should().Be(ALC.Concept.NewNegation(ALC.Concept.NewConceptName(new IriReference("https://example.com/SuperClass"))));
    }

    [Fact]
    public void TestOntologyWithSubClassAndExistential()
    {
        var (tboxAxiomsList, _) = TestOntology("""
                                               Prefix: ex: <https://example.com/> 
                                               Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                               Class: ex:Class
                                               SubClassOf: ex:Role some ex:SuperClass
                                               """
        );
        tboxAxiomsList.Should().HaveCount(1);
        tboxAxiomsList[0].Should().BeOfType<ALC.TBoxAxiom>();
        var inclusion = (ALC.TBoxAxiom)tboxAxiomsList[0];
        inclusion.Sub.Should().Be(ALC.Concept.NewConceptName(new IriReference("https://example.com/Class")));
        inclusion.Sup.Should().Be(ALC.Concept.NewExistential(ALC.Role.NewIri(new IriReference("https://example.com/Role")), ALC.Concept.NewConceptName(new IriReference("https://example.com/SuperClass"))));
    }

    [Fact]
    public void TestOntologyWithSubClassAndUniversal()
    {
        var (tboxAxiomsList, _) = TestOntology("""
                                               Prefix: ex: <https://example.com/> 
                                               Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                               Class: ex:Class
                                               SubClassOf: ex:Role only ex:SuperClass
                                               """
        );
        tboxAxiomsList.Should().HaveCount(1);
        tboxAxiomsList[0].Should().BeOfType<ALC.TBoxAxiom>();
        var inclusion = tboxAxiomsList[0];
        inclusion.Sub.Should().Be(ALC.Concept.NewConceptName(new IriReference("https://example.com/Class")));
        inclusion.Sup.Should().Be(ALC.Concept.NewUniversal(ALC.Role.NewIri(new IriReference("https://example.com/Role")), ALC.Concept.NewConceptName(new IriReference("https://example.com/SuperClass"))));
    }


    [Fact]
    public void TestOntologyWithEquivalentClass()
    {
        var (tboxAxiomsList, aboxAxioms) = TestOntology("""
                                        Prefix: ex: <https://example.com/> 
                                        Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                        Class: ex:Class
                                        EquivalentTo: ex:EqClass
                                        """
        );

        tboxAxiomsList.Should().HaveCount(2);
        tboxAxiomsList[0].Should().BeOfType<ALC.TBoxAxiom>();
        var inclusion1 = tboxAxiomsList[0];
        inclusion1.Sub.Should().BeOneOf(ALC.Concept.NewConceptName(new IriReference("https://example.com/Class")),
            ALC.Concept.NewConceptName(new IriReference("https://example.com/EqClass")));
        inclusion1.Sub.Should().BeOneOf(ALC.Concept.NewConceptName(new IriReference("https://example.com/EqClass")),
            ALC.Concept.NewConceptName(new IriReference("https://example.com/Class")));
    }


    [Fact]
    public void TestOntologyWithobjectProperty()
    {
        var (_, aboxAxioms) = TestOntology("""
                                           Prefix: ex: <https://example.com/> 
                                           Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                           ObjectProperty: ex:Role
                                           """
        );


    }

    [Fact]
    public void TestOntologyWithAboxAssertion()
    {
        var (_, aboxAxioms) = TestOntology("""
                                                        Prefix: ex: <https://example.com/> 
                                                        Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                                        Class: ex:Class
                                                            EquivalentTo: ex:EqClass
                                                        Individual: ex:ind1 
                                                            Types: ex:Class , ex:Class2
                                                        """
        );

        aboxAxioms.Should().HaveCount(2);
        aboxAxioms[0].Should().BeOfType<ALC.ABoxAssertion.ConceptAssertion>();
        var assertion = (ALC.ABoxAssertion.ConceptAssertion)aboxAxioms[0];
        assertion.Individual.Should().Be(new IriReference("https://example.com/ind1"));
        assertion.Item2.Should().BeOneOf(ALC.Concept.NewConceptName(new IriReference("https://example.com/Class")),
            ALC.Concept.NewConceptName(new IriReference("https://example.com/Class2")));
    }


    [Fact]
    public void TestOntologyWithRoleAssertion()
    {
        var (_, aboxAxioms) = TestOntology("""
                                           Prefix: ex: <https://example.com/> 
                                           Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                           Class: ex:Class
                                               EquivalentTo: ex:EqClass
                                           Individual: ex:ind1 
                                               Facts: ex:Role ex:ind2
                                           """
        );

        aboxAxioms.Should().HaveCount(1);
        aboxAxioms[0].Should().BeOfType<ALC.ABoxAssertion.RoleAssertion>();
        var assertion = (ALC.ABoxAssertion.RoleAssertion)aboxAxioms[0];
        assertion.Individual.Should().Be(new IriReference("https://example.com/ind1"));
        assertion.Right.Should().Be(new IriReference("https://example.com/ind2"));
        assertion.AssertedRole.Should().Be(ALC.Role.NewIri(new IriReference("https://example.com/Role")));
    }


    [Fact]
    public void TestOntologyWithRoleandTypeAssertions()
    {
        var (_, aboxAxioms) = TestOntology("""
                                           Prefix: ex: <https://example.com/> 
                                           Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                           Class: ex:Class
                                               EquivalentTo: ex:EqClass
                                           Individual: ex:ind1 
                                               Facts: ex:Role ex:ind2
                                            Individual: ex:Ind2
                                                Types: ex:Class
                                           """
        );

        aboxAxioms.Should().HaveCount(2);

    }


    [Fact]
    public void TestEmptyPrefix()
    {
        var (_, _) = TestOntology("""
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
        var (_, _) = TestOntology("""
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
        var (_, _) = TestOntologyFile("TestData/alctableauex.owl");
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
        var alcOntology = DagSemTools.OWL2ALC.Translator.translateDocument(_logger, parsedOntology);
        var (prefixes, versionedOntology, (tbox, abox)) = alcOntology.TryGetOntology();
        var aboxAxioms = abox.ToList();
        aboxAxioms.Should().HaveCount(2);
        foreach (var axiom in aboxAxioms)
        {
            axiom.Should().BeOfType<ALC.ABoxAssertion.RoleAssertion>();
            var assertion = (ALC.ABoxAssertion.RoleAssertion)axiom;
            assertion.Individual.Should().Be(new IriReference("https://example.com/a"));
        }
    }

    [Fact(Skip = "Not implemented yet, See Issue https://github.com/daghovland/AlcTableau/issues/2")]
    public void TestDefinitionExample()
    {
        var parsedOntology = Manchester.Parser.Parser.ParseFile("TestData/def_example.owl", _errorOutput);
        var alcOntology = DagSemTools.OWL2ALC.Translator.translateDocument(_logger, parsedOntology);
        var (prefixes, versionedOntology, (tbox, abox)) = alcOntology.TryGetOntology();
        var tboxAxioms = tbox.ToList();
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
        var alcOntology = DagSemTools.OWL2ALC.Translator.translateDocument(_logger, parsedOntology);
        var (prefixes, versionedOntology, (tbox, abox)) = alcOntology.TryGetOntology();
        var aboxAxioms = abox.ToList();
        aboxAxioms.Should().HaveCount(0);

    }

    [Fact]
    public void TestAnnotationsExample()
    {
        var parsedOntology = Manchester.Parser.Parser.ParseFile("TestData/annotations.owl", _errorOutput);
        var alcOntology = DagSemTools.OWL2ALC.Translator.translateDocument(_logger, parsedOntology);
        var (prefixes, versionedOntology, (tbox, abox)) = alcOntology.TryGetOntology();
        var aboxAxioms = abox.ToList();
        aboxAxioms.Should().HaveCount(0);

    }

}