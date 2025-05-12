(*
    Copyright (C) 2024-2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.ELI.Tests

open DagSemTools
open DagSemTools.Datalog
open DagSemTools.ELI.Axioms
open DagSemTools.Rdf
open DagSemTools.Ingress
open DagSemTools.OwlOntology
open IriTools
open Xunit
open Serilog
open Serilog.Sinks.InMemory
open Faqt

module TestClassAxioms =
    let inMemorySink = new InMemorySink()
    let logger =
        LoggerConfiguration()
            .WriteTo.Sink(inMemorySink)
            .CreateLogger()
            
    [<Fact>]
    let ``Subclass axiom is extracted`` () =
        //Arrange
        let subClassIri = (FullIri(IriReference "https://example.com/subclass"))
        let superClassIri = (FullIri(IriReference "https://example.com/superclass"))

        let axiom =
            SubClassOf([], ClassName subClassIri, ClassName superClassIri)
        //Act
        let translatedAxioms = ELI.ELIExtractor.ELIAxiomExtractor logger axiom
        //Assert
        let expectedAxiom =
            Some
                [ ELI.Axioms.DirectlyTranslatableConceptInclusion(
                      [ ELI.Axioms.ComplexConcept.AtomicConcept subClassIri ],
                      [ superClassIri ]
                  ) ]

        Assert.Equal(expectedAxiom, translatedAxioms)
        inMemorySink.LogEvents.Should().BeEmpty()

    [<Fact>]
    let ``EquivalentClass axiom is extracted`` () =
        //Arrange
        let subClassIri = (FullIri (IriReference "https://example.com/subclass"))
        let superClassIri = (FullIri (IriReference "https://example.com/superclass"))

        let axiom =
            EquivalentClasses([], [ ClassName subClassIri; ClassName superClassIri ])
        //Act
        let translatedAxioms = ELI.ELIExtractor.ELIAxiomExtractor logger axiom
        //Assert
        let expectedAxiom1 =
            ELI.Axioms.DirectlyTranslatableConceptInclusion(
                [ ELI.Axioms.ComplexConcept.AtomicConcept subClassIri ],
                [ superClassIri ]
            )

        let expectedAxiom2 =
            ELI.Axioms.DirectlyTranslatableConceptInclusion(
                [ ELI.Axioms.ComplexConcept.AtomicConcept superClassIri ],
                [ subClassIri ]
            )

        let expectedAxiomList = Some [ expectedAxiom2; expectedAxiom1 ]
        Assert.Equal(expectedAxiomList, translatedAxioms)
        inMemorySink.LogEvents.Should().BeEmpty()


    [<Fact>]
    let ``Subclass axiom creates datalog rule`` () =
        //Arrange
        let resources = new GraphElementManager(10u)
        let subClassIri = (FullIri(IriReference "https://example.com/subclass"))
        let superClassIri = (FullIri(IriReference "https://example.com/superclass"))

        let axiom =
            ELI.Axioms.DirectlyTranslatableConceptInclusion(
                [ ELI.Axioms.ComplexConcept.AtomicConcept subClassIri ],
                [ superClassIri ]
            )
        //Act
        let translatedRules = ELI.ELI2RL.GenerateTBoxRL logger resources [ axiom ]
        //Assert
        let expectedRules: Datalog.Rule seq =
            [ { Head = NormalHead
                  { Subject = Term.Variable "X"
                    Predicate = Term.Resource(resources.AddNodeResource(Iri(IriReference Namespaces.RdfType)))
                    Object =
                      Term.Resource(
                          resources.AddNodeResource(Iri(IriReference "https://example.com/superclass"))
                      ) }
                Body =
                  [ PositiveTriple
                        { Subject = Term.Variable "X"
                          Predicate =
                            Term.Resource(resources.AddNodeResource(Iri(IriReference Namespaces.RdfType)))
                          Object =
                            Term.Resource(
                                resources.AddNodeResource(Iri(IriReference "https://example.com/subclass"))
                            ) } ] } ]

        Assert.Equal<Rule seq>(expectedRules, translatedRules)
        inMemorySink.LogEvents.Should().BeEmpty


    // >= 1 (E U F) subClassof A
    // E U F occurs negatively, so should lead to
    // E_C  subClassOf (E U F)_C, and F_C subClassOf (E U F)_C
    // E subClassOf E_C, F subClassOf F_C
    // >= 1 (E U F) also occurs neg. so should lead to
    // (Note re-writing on page 2043)
    // (E U F)_C subClassOf forall t^-1 (>= 1 (E U F))_C
    // Finally also
    // A_C subClassOf A
    // (>= 1 (E U F))_C subClassOf A_C
    [<Fact>]
    let ``Subclass axiom normalization handles qualified union`` () =
        //Arrange
        // Arrange
        let tripleTable = new Datastore(100u)
        let errorOutput = new System.IO.StringWriter()
        
        let Airi = IriReference "https://example.com/class/A"
        let A = ClassName (FullIri Airi)
        let Eiri = IriReference "https://example.com/class/E"
        let E = ClassName (FullIri Eiri)
        let Firi = IriReference "https://example.com/class/F"
        let F = ClassName (FullIri Firi)
        
        let union = ObjectUnionOf [E; F]
        let roleIri = IriReference "https://example.com/property/t"
        let role = NamedObjectProperty (FullIri roleIri)
        let restriction = ObjectMinQualifiedCardinality(1, role, union)
        let classAxiom = (SubClassOf ([], restriction, A))
        //Act
        let normalizedAxioms = ELI.ELIExtractor.SubClassAxiomNormalization logger classAxiom
        //Assert
        normalizedAxioms.Length.Should().Be(7) |> ignore
        
        
        // E subClassOf E_C
        let ESubClassFormulas =
            normalizedAxioms
                  |> List.filter (fun (NormalizedConceptInclusion (subclassConjunction = union; superclass = superclas)) -> union = [FullIri Eiri])
        ESubClassFormulas.Should().HaveLength(1) |> ignore
        let E_Concept = ESubClassFormulas
                            |> List.map (fun (NormalizedConceptInclusion (subclassConjunction = union; superclass = superclas)) -> superclas)
                            |> List.head
        let E_C = 
            match (E_Concept) with
                | AtomicNamedConcept cls -> cls
                | _ -> failwith $"Test error on {E_Concept}"
        
        
        
        // E_C subClassOf (E U F)_C and E_F subClassOf (E U F)_C
        let unionSubClassFormulas =
            normalizedAxioms
                  |> List.filter (fun (NormalizedConceptInclusion (union, superclas)) -> union = [E_C])
        unionSubClassFormulas.Should().HaveLength(1) |> ignore
        let E_U_F_C_Concept = unionSubClassFormulas
                            |> List.map (fun (NormalizedConceptInclusion (union, superclas)) -> superclas)
                            |> List.head
        let E_U_F_C = 
            match (E_U_F_C_Concept) with
                | AtomicNamedConcept cls -> cls
                | _ -> failwith $"Test error on {E_U_F_C_Concept}"
        
        // (E U F)_C subClassOf forall t^-1 (>= 1 (E U F))_C
        let expectedRestrictionRule =  normalizedAxioms
                                        |> List.filter (fun (NormalizedConceptInclusion (subClassList, superClass)) ->
                                            subClassList = [E_U_F_C])
        expectedRestrictionRule.Should().HaveLength(1) |> ignore
        let restrictionFormaula =
                                match (expectedRestrictionRule|> List.head) with
                                | NormalizedConceptInclusion (subClassList, superClass) -> superClass
                                | _ -> failwith "test failure"
        let restrictionConceptIri =
            match restrictionFormaula with
            | AllValuesFrom (role, concept) -> concept 
        inMemorySink.LogEvents.Should().BeEmpty



    [<Fact(Skip="https://github.com/daghovland/DagSemTools/issues/76")>]
    let ``Max qualified cardinality 1 is translated correctly`` () =

        // Arrange
        let tripleTable = new Datastore(100u)
        let errorOutput = new System.IO.StringWriter()
        
        let Airi = IriReference "https://example.com/class/A"
        let A = FullIri Airi
        let Eiri = IriReference "https://example.com/class/E"
        let E = FullIri Eiri
        let owlSameAsResource = tripleTable.Resources.AddNodeResource (RdfResource.Iri (IriReference Namespaces.OwlSameAs))
        
        let roleIri = IriReference "https://example.com/property/t"
        let role = NamedObjectProperty (FullIri roleIri)
        let negative_equality = NotTriple {
            Subject = Term.Variable "Y1"
            Predicate = Term.Resource owlSameAsResource
            Object = Term.Variable "Y2"
        }
        
        //Act
        let translatedRules = ELI.ELI2RL.getQualifiedAtMostOneNormalizedRule tripleTable.Resources [A] role E 
        
        //Assert
        translatedRules.Should().ContainExactlyOneItem() |> ignore
        let rule = translatedRules |> Seq.head
        rule.Body.Should().NotContain(negative_equality) |> ignore
        
        inMemorySink.LogEvents.Should().BeEmpty
