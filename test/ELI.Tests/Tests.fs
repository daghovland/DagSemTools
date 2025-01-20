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
                  { Subject = ResourceOrVariable.Variable "X"
                    Predicate = ResourceOrVariable.Resource(resources.AddNodeResource(Iri(IriReference Namespaces.RdfType)))
                    Object =
                      ResourceOrVariable.Resource(
                          resources.AddNodeResource(Iri(IriReference "https://example.com/superclass"))
                      ) }
                Body =
                  [ PositiveTriple
                        { Subject = ResourceOrVariable.Variable "X"
                          Predicate =
                            ResourceOrVariable.Resource(resources.AddNodeResource(Iri(IriReference Namespaces.RdfType)))
                          Object =
                            ResourceOrVariable.Resource(
                                resources.AddNodeResource(Iri(IriReference "https://example.com/subclass"))
                            ) } ] } ]

        Assert.Equal<Rule seq>(expectedRules, translatedRules)
        inMemorySink.LogEvents.Should().BeEmpty


    // >= 1 (E U F) subClassof A
    // Should lead to
    // 
    [<Fact>]
    let ``Subclass axiom normalization handles qualified union`` () =
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
                  { Subject = ResourceOrVariable.Variable "X"
                    Predicate = ResourceOrVariable.Resource(resources.AddNodeResource(Iri(IriReference Namespaces.RdfType)))
                    Object =
                      ResourceOrVariable.Resource(
                          resources.AddNodeResource(Iri(IriReference "https://example.com/superclass"))
                      ) }
                Body =
                  [ PositiveTriple
                        { Subject = ResourceOrVariable.Variable "X"
                          Predicate =
                            ResourceOrVariable.Resource(resources.AddNodeResource(Iri(IriReference Namespaces.RdfType)))
                          Object =
                            ResourceOrVariable.Resource(
                                resources.AddNodeResource(Iri(IriReference "https://example.com/subclass"))
                            ) } ] } ]

        Assert.Equal<Rule seq>(expectedRules, translatedRules)
        inMemorySink.LogEvents.Should().BeEmpty
