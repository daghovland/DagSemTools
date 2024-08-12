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
    let reasoner_state = { concept_assertions = concept_map; role_assertions = Map.empty; subclass_assertions = Map.empty }
    let has_collision = Tableau.has_new_collision reasoner_state (ALC.ConceptAssertion(individual, concept))
    Assert.True(has_collision)


[<Fact>]
let ``collisions should not be falsely detected`` () =
    (
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let negation = ALC.Negation(concept)
    let individual = IriReference "http://example.org/individual"
    let concept_map = Map.empty.Add(individual, [negation])
    let reasoner_state = { concept_assertions = concept_map; role_assertions = Map.empty; subclass_assertions = Map.empty }
    let has_collision = Tableau.has_new_collision reasoner_state (ALC.ConceptAssertion(individual, negation))
    Assert.False(has_collision)
    )

[<Fact>]
let ``opposite collisions should be detected`` () =
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let negation = ALC.Negation(concept)
    let individual = IriReference "http://example.org/individual"
    let concept_map = Map.empty.Add(individual, [concept])
    let reasoner_state = { concept_assertions = concept_map; role_assertions = Map.empty; subclass_assertions = Map.empty }
    let has_collision = Tableau.has_new_collision reasoner_state (ALC.ConceptAssertion(individual, negation))
    Assert.True(has_collision)

[<Fact>]
let ``adding assertion list should work``() =
    let reasoner_state = add_assertion_list
                             { concept_assertions = Map.empty; role_assertions = Map.empty; subclass_assertions = Map.empty }
                             [ALC.ConceptAssertion(IriReference "http://example.org/individual", ALC.ConceptName(IriReference "http://example.org/concept"))]
    Assert.True(reasoner_state.concept_assertions.ContainsKey(IriReference "http://example.org/individual"))
    Assert.Equal(1, reasoner_state.concept_assertions.[IriReference "http://example.org/individual"].Length)
    Assert.True(reasoner_state.role_assertions.IsEmpty)

[<Fact>]
let ``expander should succeed when no more facts to be added ``() =
    let reasoner_state = { concept_assertions = Map.empty; role_assertions = Map.empty; subclass_assertions = Map.empty }
    let result = Tableau.expand reasoner_state []
    Assert.True(result)


    
[<Fact>]
let ``simplest consistent is detected`` () =
    (
     let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
     let individual = IriReference("http://example.org/individual")
     let individualAssertion = ALC.ConceptAssertion(individual, concept)
     let kb = ([], [individualAssertion])
     let reasoningRules = Tableau.is_consistent kb
     Assert.True(reasoningRules)
    )
     
[<Fact>]
let ``init expander makes tbox`` () =
    let concept = ALC.ConceptName(IriReference "http://example.org/concept")
    let concept2 = ALC.ConceptName(IriReference "http://example.org/concept2")
    let individual = IriReference "http://example.org/individual"
    let tbox = [ALC.Inclusion (concept, concept2)]
    let reasoner_state = init_tbox_expander tbox { concept_assertions = Map.empty; role_assertions = Map.empty; subclass_assertions = Map.empty }
    Assert.True(reasoner_state.subclass_assertions.ContainsKey(concept))
    Assert.Equal(reasoner_state.subclass_assertions[concept].Length, 1)
    Assert.Equal(reasoner_state.subclass_assertions[concept][0], concept2)

[<Fact>]
let ``init expander should work on simplest example`` () =
    let concept = ALC.ConceptName(IriReference "http://example.org/concept")
    let individual = IriReference "http://example.org/individual"
    let individualAssertion = ALC.ConceptAssertion(individual, concept)
    let reasoner_state = init_expander ([], [individualAssertion])
    Assert.True(reasoner_state.concept_assertions.ContainsKey(individual))
    Assert.Equal(1, reasoner_state.concept_assertions[individual].Length)
    Assert.Equal(concept, reasoner_state.concept_assertions[individual].[0])
    Assert.True(reasoner_state.role_assertions.IsEmpty)
    Assert.True(reasoner_state.subclass_assertions.IsEmpty)


[<Fact>]
let ``expander should succeed when only an empty fact is to be added ``() =
    (
     let reasoner_state = { concept_assertions = Map.empty; role_assertions = Map.empty; subclass_assertions = Map.empty }
     let result = Tableau.expand reasoner_state [[]]
     Assert.True(result)
    )

[<Fact>]
let ``simplest collision is detected`` () =
    (
     let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
     let negation = ALC.Negation(concept)
     let individual = IriReference "http://example.org/individual"
     let contradiction = ALC.Conjunction(concept, negation)
     let individualAssertion = ALC.ConceptAssertion(individual, contradiction)
     let kb = ([], [individualAssertion])
     let reasoningRules = Tableau.is_consistent kb
     Assert.False(reasoningRules)
    )


[<Fact>]
let ``collision with universal is detected`` () =
    (
     let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
     let negation = ALC.Negation(concept)
     let individual = IriReference "http://example.org/individual"
     let role = ALC.Role(IriReference "http://example.org/role")
     let contradiction = ALC.Conjunction(ALC.Existential(role, concept), ALC.Conjunction(ALC.Universal(role, concept), ALC.Universal(role, negation)))
     let individualAssertion = ALC.ConceptAssertion(individual, contradiction)
     let kb = ([], [individualAssertion])
     let reasoningRules = Tableau.is_consistent kb
     Assert.False(reasoningRules)
    )


[<Fact>]
let ``existential is ok`` () =
    (
     let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
     let individual = IriReference "http://example.org/individual"
     let role = ALC.Role(IriReference "http://example.org/role")
     let existenial = ALC.Existential(role, concept)
     let individualAssertion = ALC.ConceptAssertion(individual, existenial)
     let kb = ([], [individualAssertion])
     let reasoningRules = Tableau.is_consistent kb
     Assert.True(reasoningRules)
    )



[<Fact>]
let ``negative existential and universal is collision`` () =
    (
     let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
     let negation = ALC.Negation(concept)
     let individual = IriReference "http://example.org/individual"
     let role = ALC.Role(IriReference "http://example.org/role")
     let contradiction = ALC.Conjunction(ALC.Existential(role, ALC.Negation concept), ALC.Universal(role, concept))
     let individualAssertion = ALC.ConceptAssertion(individual, contradiction)
     let kb = ([], [individualAssertion])
     let reasoningRules = Tableau.is_consistent kb
     Assert.False(reasoningRules)
    )
