namespace DagSemTools.ELI.Tests

open DagSemTools
open DagSemTools.ELI.Axioms
open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.Ingress
open DagSemTools.OwlOntology
open IriTools
open Xunit
open Faqt

module TestClassAxioms =

    [<Fact>]
    let ``Subclass axiom is extracted`` () =
        //Arrange
        let subClassIri = (FullIri (IriReference "https://example.com/subclass"))
        let superClassIri = (FullIri (IriReference "https://example.com/superclass"))
        let axiom = OwlOntology.AxiomClassAxiom (SubClassOf ([], ClassName subClassIri, ClassName superClassIri))
        //Act
        let translatedAxioms = ELI.ELIExtractor.ELIAxiomxtractor axiom
        //Assert
        let expectedAxiom = Some [ELI.Axioms.SubClassAxiom ([ELI.Axioms.ELIClass.ClassName subClassIri],[superClassIri])]
        Assert.Equal(expectedAxiom, translatedAxioms)
        
    [<Fact>]
    let ``EquivalentClass axiom is extracted`` () =
        //Arrange
        let subClassIri = (FullIri "https://example.com/subclass")
        let superClassIri = (FullIri "https://example.com/superclass")
        let axiom = OwlOntology.AxiomClassAxiom (EquivalentClasses ([], [ClassName subClassIri; ClassName superClassIri]))
        //Act
        let translatedAxioms = ELI.ELIExtractor.ELIAxiomxtractor axiom
        //Assert
        let expectedAxiom1 = ELI.Axioms.SubClassAxiom ([ELI.Axioms.ELIClass.ClassName subClassIri],[superClassIri])
        let expectedAxiom2 = ELI.Axioms.SubClassAxiom ([ELI.Axioms.ELIClass.ClassName superClassIri],[subClassIri])
        let expectedAxiomList = Some [expectedAxiom2; expectedAxiom1]
        Assert.Equal(expectedAxiomList, translatedAxioms)