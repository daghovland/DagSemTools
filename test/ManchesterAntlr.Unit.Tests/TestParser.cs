using Antlr4.Runtime.Misc;
using Microsoft.FSharp.Collections;

namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using AlcTableau.ManchesterAntlr;
using FluentAssertions;
using AlcTableau;
using IriTools;

public class TestParser
{


    private ALC.OntologyDocument TestFile(string filename){
        using TextReader textReader = File.OpenText(filename);
        return TestReader(textReader);
    }

    private ALC.OntologyDocument TestReader(TextReader textReader, Dictionary<string, IriReference> prefixes){
        
        var input = new AntlrInputStream(textReader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        parser.ErrorHandler = new BailErrorStrategy();
        IParseTree tree = parser.ontologyDocument();
        var visitor = new ManchesterVisitor();
        return visitor.Visit(tree);
    }
    
    public ALC.OntologyDocument TestReader(TextReader textReader) =>
        TestReader(textReader, new Dictionary<string, IriReference>());
    public ALC.OntologyDocument TestString(string owl){
        using TextReader textReader = new StringReader(owl);
        return TestReader(textReader);
    }
    
    public (List<ALC.TBoxAxiom>, List<ALC.ABoxAssertion>) TestOntology(string ontology)
    {
        var parsedOntology = TestString(ontology);
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

    [Fact]
    public void TestSmallestOntology()
    {
        var parsed = TestString("Ontology:");
        parsed.Should().NotBeNull();
    }

    
    [Fact]
    public void TestOntologyWithIri()
    {
        var parsedOntology = TestString("Prefix: ex: <https://example.com/> Ontology: <https://example.com/ontology>");
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
        inclusion.Sup.Should().Be(ALC.Concept.NewExistential(new IriReference("https://example.com/Role"), ALC.Concept.NewConceptName(new IriReference("https://example.com/SuperClass"))));
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
        assertion.Left.Should().Be(new IriReference("https://example.com/ind1"));
        assertion.Right.Should().Be(new IriReference("https://example.com/ind2"));
        assertion.AssertedRole.Should().Be(new IriReference("https://example.com/Role"));
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
            var (_, _) = TestOntology("""
                                        Prefix: : <https://example.com/empty/>
                                        Prefix: ex: <https://example.com/> 
                                        Ontology: <https://example.com/ontology> <https://example.com/ontology#1>   
                                        Class: Course EquivalentTo: UGC and PGC
                                        Class: PGC SubClassOf: not UGC
                                        Class: Professor EquivalentTo: Teacher and teaches some PGC 
                                        Individual: Betty Types: Professor
                                            Facts: teaches CS600
                                        Individual: Hugo Types: Student
                                            Facts: attends CS600
                                        Individual: CS600 Types: Course
                                      """
            );
    }
}