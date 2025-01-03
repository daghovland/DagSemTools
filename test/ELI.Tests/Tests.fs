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
