using Antlr4.Runtime.Misc;
using Microsoft.FSharp.Collections;
using TurtleParser.Unit.Tests;
using Xunit.Abstractions;

namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using AlcTableau.ManchesterAntlr;
using FluentAssertions;
using AlcTableau;
using IriTools;
using ManchesterAntlr;

public class TestParser
{
    private readonly ITestOutputHelper _output;
    private TestOutputTextWriter _errorOutput;
    public TestParser(ITestOutputHelper output)
    {
        _output = output;
        _errorOutput = new TestOutputTextWriter(output);
    }
    public (List<ALC.TBoxAxiom>, List<ALC.ABoxAssertion>) TestOntologyFile(string filename)
    {
        var parsedOntology = ManchesterAntlr.Parser.ParseFile(filename, _errorOutput);
        return TestOntology(parsedOntology);
    }

    private (List<ALC.TBoxAxiom>, List<ALC.ABoxAssertion>) TestOntology(ALC.OntologyDocument parsedOntology)
    {
        parsedOntology.Should().NotBeNull();

        var (prefixes, versionedOntology, KB) = parsedOntology.TryGetOntology();

        prefixes.ToList().Should().Contain(ALC.prefixDeclaration.NewPrefixDefinition("ex", new IriReference("https://example.com/")));

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
        var parsedOntology = ManchesterAntlr.Parser.ParseString(ontology, _errorOutput);
        return TestOntology(parsedOntology);
    }

    [Fact]
    public void TestSmallestOntology()
    {
        var parsed = ManchesterAntlr.Parser.ParseString("Ontology:", _errorOutput);
        parsed.Should().NotBeNull();
    }

    [Fact]
    public void TestErrorHandling()
    {
        var customErrorOutput = new TestOutputTextWriter(_output);
        var parsed = ManchesterAntlr.Parser.ParseString("""
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
        var parsed = ManchesterAntlr.Parser.ParseString("""
                                                        Ontology: <https://ex.com/owl2/families>
                                                        Class: fam:Person
                                                        """, customErrorOutput);
        customErrorOutput.LastError.Should().Be("line 2:7 Prefix fam not defined.");
        
    }
    
    [Fact]
    public void TestOntologyWithIri()
    {
        var parsedOntology = ManchesterAntlr.Parser.ParseString("Prefix: ex: <https://example.com/> Ontology: <https://example.com/ontology>", _errorOutput);
        parsedOntology.Should().NotBeNull();

        var (prefixes, versionedOntology, KB) = parsedOntology.TryGetOntology();

        var prefixDeclarations = prefixes.ToList();
        prefixDeclarations.Should().Contain(ALC.prefixDeclaration.NewPrefixDefinition("ex", new IriReference("https://example.com/")));

        var ontologyIri = versionedOntology.TryGetOntologyIri();
        ontologyIri.Should().NotBeNull();
        ontologyIri.Should().Be(new IriReference("https://example.com/ontology"));

        var namedOntology = (ALC.ontologyVersion.NamedOntology)versionedOntology;
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
        var ontologyTester = () => TestOntology("""
                         Prefax: ex: <https://example.com/> 
                         Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                         Class: ex:Class
                         """);
        ontologyTester.Should().Throw<ParseCanceledException>();
    }

    [Fact]
    public void TestWrongOntology2()
    {
        var ontologyTester = () => TestOntology("""
                                                Prefix: ex: <https://example.com/> 
                                                Ontology: <https://example.com/ontology> <https://example.com/ontology#1> 
                                                Class: ax:Class
                                                """);
        ontologyTester.Should().Throw<KeyNotFoundException>();
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
        tboxAxiomsList[0].Should().BeOfType<ALC.TBoxAxiom.Inclusion>();
        var inclusion = (ALC.TBoxAxiom.Inclusion)tboxAxiomsList[0];
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
            tboxAxiom.Should().BeOfType<ALC.TBoxAxiom.Inclusion>();
            var inclusion = (ALC.TBoxAxiom.Inclusion)tboxAxiom;
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
        tboxAxiomsList[0].Should().BeOfType<ALC.TBoxAxiom.Inclusion>();
        var inclusion = (ALC.TBoxAxiom.Inclusion)tboxAxiomsList[0];
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
        tboxAxiomsList[0].Should().BeOfType<ALC.TBoxAxiom.Inclusion>();
        var inclusion = (ALC.TBoxAxiom.Inclusion)tboxAxiomsList[0];
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
        tboxAxiomsList[0].Should().BeOfType<ALC.TBoxAxiom.Inclusion>();
        var inclusion = (ALC.TBoxAxiom.Inclusion)tboxAxiomsList[0];
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

        tboxAxiomsList.Should().HaveCount(1);
        tboxAxiomsList[0].Should().BeOfType<ALC.TBoxAxiom.Equivalence>();
        var inclusion = (ALC.TBoxAxiom.Equivalence)tboxAxiomsList[0];
        inclusion.Left.Should().Be(ALC.Concept.NewConceptName(new IriReference("https://example.com/Class")));
        inclusion.Right.Should().Be(ALC.Concept.NewConceptName(new IriReference("https://example.com/EqClass")));
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
        assertion.Item2.Should().Be(ALC.Concept.NewConceptName(new IriReference("https://example.com/Class")));
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
        var parsedOntology = ManchesterAntlr.Parser.ParseString("""
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
        var parsedOntology = ManchesterAntlr.Parser.ParseString("""
                                           Prefix: : <https://example.com/> 
                                           Ontology:  
                                           Individual: a 
                                               Facts: r b, s c
                                           """, _errorOutput
        );
        var (prefixes, versionedOntology, (tbox, abox)) = parsedOntology.TryGetOntology();
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
        var parsedOntology = ManchesterAntlr.Parser.ParseFile("TestData/def_example.owl", _errorOutput);
        var (prefixes, versionedOntology, (tbox, abox)) = parsedOntology.TryGetOntology();
        var tboxAxioms = tbox.ToList();
        tboxAxioms.Should().HaveCount(2);

    }



    [Fact]
    public void TestDataTypeFacet()
    {
        var parsedOntology = ManchesterAntlr.Parser.ParseString("""
                                                                Prefix: : <https://example.com/>
                                                                Ontology: 
                                                                Datatype: NegInt
                                                                  Annotations: rdfs:comment "Negative Integer"
                                                                  EquivalentTo: integer[< 0]   
                                                                """, _errorOutput);
        var (prefixes, versionedOntology, (tbox, abox)) = parsedOntology.TryGetOntology();
        var aboxAxioms = abox.ToList();
        aboxAxioms.Should().HaveCount(0);

    }

    [Fact]
    public void TestAnnotationsExample()
    {
        var parsedOntology = ManchesterAntlr.Parser.ParseFile("TestData/annotations.owl", _errorOutput);
        var (prefixes, versionedOntology, (tbox, abox)) = parsedOntology.TryGetOntology();
        var aboxAxioms = abox.ToList();
        aboxAxioms.Should().HaveCount(0);

    }

}