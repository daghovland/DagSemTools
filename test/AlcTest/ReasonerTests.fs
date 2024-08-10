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
let ``collisions should be detected`` () =
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let negation = ALC.Negation(concept)
    let individual = IriReference "http://example.org/individual"
    let concept_map = Map.empty.Add(individual, [negation])
    let has_collision = Tableau.has_new_collision concept_map (ALC.ConceptAssertion(individual, concept))
    Assert.True(has_collision)


[<Fact>]
let ``opposite collisions should be detected`` () =
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let negation = ALC.Negation(concept)
    let individual = IriReference "http://example.org/individual"
    let concept_map = Map.empty.Add(individual, [concept])
    let has_collision = Tableau.has_new_collision concept_map (ALC.ConceptAssertion(individual, negation))
    Assert.True(has_collision)

[<Fact>]
let ``adding assertion list should work``() =
    let (concepts, roles) = add_assertion_list Map.empty (Map.empty) [ALC.ConceptAssertion(IriReference "http://example.org/individual", ALC.ConceptName(IriReference "http://example.org/concept"))]
    Assert.True(concepts.ContainsKey(IriReference "http://example.org/individual"))
    Assert.Equal(1, concepts.[IriReference "http://example.org/individual"].Length)
    Assert.True(roles.IsEmpty)

[<Fact>]
let ``expander should succeed when no more facts to be added ``() =
    let result = Tableau.expand Map.empty Map.empty []
    Assert.True(result)


[<Fact>]
let ``expander should succeed when only an empty fact is to be added ``() =
    let result = Tableau.expand Map.empty Map.empty [[]]
    Assert.True(result)

[<Fact>]
let ``collisions should not be falsely detected`` () =
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let negation = ALC.Negation(concept)
    let individual = IriReference "http://example.org/individual"
    let concept_map = Map.empty.Add(individual, [negation])
    let has_collision = Tableau.has_new_collision concept_map (ALC.ConceptAssertion(individual, negation))
    Assert.False(has_collision)

[<Fact>]
let ``init expender should work on simplest example``() =
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let individual = IriReference "http://example.org/individual"
    let individualAssertion = ALC.ConceptAssertion(individual, concept)
    
    let (concepts, roles) = init_expander [individualAssertion]
    Assert.True(concepts.ContainsKey(individual))
    Assert.Equal(1, concepts.[individual].Length)
    Assert.Equal(concept, concepts.[individual].[0])
    Assert.True(roles.IsEmpty)

[<Fact>]
let ``Simplest collision is detected`` () =
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let negation = ALC.Negation(concept)
    let individual = IriReference "http://example.org/individual"
    let contradiction = ALC.Conjunction(concept, negation)
    let individualAssertion = ALC.ConceptAssertion(individual, contradiction)
    let kb = ([], [individualAssertion])
    let reasoningRules = Tableau.reasoner kb
    Assert.False(reasoningRules)


[<Fact>]
let ``Simplest consistent is detected`` () =
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let individual = IriReference "http://example.org/individual"
    let individualAssertion = ALC.ConceptAssertion(individual, concept)
    let kb = ([], [individualAssertion])
    let reasoningRules = Tableau.reasoner ([], [individualAssertion])
    Assert.True(reasoningRules)
    
 