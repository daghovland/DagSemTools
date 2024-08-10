module AlcTest.ReasonerTests

open System
open AlcTableau.ALC
open Xunit
open AlcTableau
open IriTools
open System.IO
open Manchester.Printer
open Tableau

[<Fact>]
let ``Simplest collision is detected`` () =
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let negation = ALC.Negation(concept)
    let individual = IriReference "http://example.org/individual"
    let contradiction = ALC.Conjunction(concept, negation)
    let individualAssertion = ALC.ConceptAssertion(individual, contradiction)
    let kb = ([], [individualAssertion])
    let reasoningRules = Tableau.reasoner ([], [individualAssertion])
    Assert.False(reasoningRules)


[<Fact>]
let ``Simplest consistent is detected`` () =
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let individual = IriReference "http://example.org/individual"
    let individualAssertion = ALC.ConceptAssertion(individual, concept)
    let kb = ([], [individualAssertion])
    let reasoningRules = Tableau.reasoner ([], [individualAssertion])
    Assert.True(reasoningRules)