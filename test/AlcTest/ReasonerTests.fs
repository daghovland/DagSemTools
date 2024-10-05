(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

module DagSemTools.AlcTableay.Tests.ReasonerTests

open DagSemTools.AlcTableau
open DagSemTools.AlcTableau.ALC
open Xunit
open DagSemTools
open IriTools
open Tableau
open AlcTableau.ConjunctiveQuery


let initReasonerState concept_map =
        { known_concept_assertions = concept_map
          known_role_assertions = Map.empty
          subclass_assertions = Map.empty
          probable_concept_assertions = Map.empty
          probable_role_assertions = Map.empty
          negative_role_assertion = Map.empty
          functionality = PrepareQueryingCache 
          }

[<Fact>]
let ``collisions should be detected`` () =
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let negation = ALC.Negation(concept)
    let individual = IriReference "http://example.org/individual"
    let concept_map = Map.empty.Add(individual, [negation])
    let reasoner_state = initReasonerState concept_map
    let has_collision = Tableau.has_new_collision reasoner_state (ALC.ConceptAssertion(individual, concept))
    Assert.True(has_collision)


[<Fact>]
let ``collisions should not be falsely detected`` () =
    (
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let negation = ALC.Negation(concept)
    let individual = IriReference "http://example.org/individual"
    let concept_map = Map.empty.Add(individual, [negation])
    
    let reasoner_state = initReasonerState concept_map
    let has_collision = Tableau.has_new_collision reasoner_state (ALC.ConceptAssertion(individual, negation))
    Assert.False(has_collision)
    )

[<Fact>]
let ``opposite collisions should be detected`` () =
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let negation = ALC.Negation(concept)
    let individual = IriReference "http://example.org/individual"
    let concept_map = Map.empty.Add(individual, [concept])
    let reasoner_state = initReasonerState concept_map
    let has_collision = Tableau.has_new_collision reasoner_state (ALC.ConceptAssertion(individual, negation))
    Assert.True(has_collision)

[<Fact>]
let ``adding assertion list should work``() =
    let reasoner_state = add_assertion_list (initReasonerState Map.empty)
                             [ALC.ConceptAssertion(IriReference "http://example.org/individual", ALC.ConceptName(IriReference "http://example.org/concept"))]
    Assert.True(reasoner_state.known_concept_assertions.ContainsKey(IriReference "http://example.org/individual"))
    Assert.Equal(1, reasoner_state.known_concept_assertions.[IriReference "http://example.org/individual"].Length)
    Assert.True(reasoner_state.known_role_assertions.IsEmpty)

[<Fact>]
let ``expander should succeed when no more facts to be added ``() =
    let reasoner_state = initReasonerState Map.empty
    let result = Tableau.expand reasoner_state [] |> is_consistent_result
    Assert.True(result)


    
[<Fact>]
let ``simplest consistent is detected`` () =
    (
     let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
     let individual = IriReference("http://example.org/individual")
     let individualAssertion = ALC.ConceptAssertion(individual, concept)
     let kb = ([], [individualAssertion])
     let state = ReasonerService.init kb
     let reasoningRules = ReasonerService.is_consistent state
     Assert.True(reasoningRules)
    )
     
[<Fact>]
let ``init expander makes tbox`` () =
    let concept = ALC.ConceptName(IriReference "http://example.org/concept")
    let concept2 = ALC.ConceptName(IriReference "http://example.org/concept2")
    let individual = IriReference "http://example.org/individual"
    let tbox = [ALC.Inclusion (concept, concept2)]
    let reasoner_state = init_tbox_expander tbox (initReasonerState Map.empty)
    Assert.True(reasoner_state.subclass_assertions.ContainsKey(concept))
    Assert.Equal(reasoner_state.subclass_assertions[concept].Length, 1)
    Assert.Equal(reasoner_state.subclass_assertions[concept][0], concept2)

[<Fact>]
let ``init expander should work on simplest example`` () =
    let concept = ALC.ConceptName(IriReference "http://example.org/concept")
    let individual = IriReference "http://example.org/individual"
    let individualAssertion = ALC.ConceptAssertion(individual, concept)
    let reasoner_state = init_expander ([], [individualAssertion])
    Assert.True(reasoner_state.known_concept_assertions.ContainsKey(individual))
    Assert.Equal(1, reasoner_state.known_concept_assertions[individual].Length)
    Assert.Equal(concept, reasoner_state.known_concept_assertions[individual].[0])
    Assert.True(reasoner_state.known_role_assertions.IsEmpty)
    Assert.True(reasoner_state.subclass_assertions.IsEmpty)


[<Fact>]
let ``expander should succeed when only an empty fact is to be added ``() =
    (
     let reasoner_state = initReasonerState Map.empty
     let result = Tableau.expand reasoner_state [NewAssertions.Nothing] |> is_consistent_result
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
     let state = ReasonerService.init kb
     let reasoningRules = ReasonerService.is_consistent state
     Assert.False(reasoningRules)
    )


[<Fact>]
let ``collision with universal is detected`` () =
    (
     let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
     let negation = ALC.Negation(concept)
     let individual = IriReference "http://example.org/individual"
     let role = ALC.Role.Iri(IriReference "http://example.org/role")
     let contradiction = ALC.Conjunction(ALC.Existential(role, concept), ALC.Conjunction(ALC.Universal(role, concept), ALC.Universal(role, negation)))
     let individualAssertion = ALC.ConceptAssertion(individual, contradiction)
     let kb = ([], [individualAssertion])
     let state = ReasonerService.init kb
     let reasoningRules = ReasonerService.is_consistent state
     Assert.False(reasoningRules)
    )


[<Fact>]
let ``existential is ok`` () =
    (
     let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
     let individual = IriReference "http://example.org/individual"
     let role = ALC.Role.Iri(IriReference "http://example.org/role")
     let existenial = ALC.Existential(role, concept)
     let individualAssertion = ALC.ConceptAssertion(individual, existenial)
     let kb = ([], [individualAssertion])
     let state = ReasonerService.init kb
     let reasoningRules = ReasonerService.is_consistent state
     Assert.True(reasoningRules)
    )



[<Fact>]
let ``negative existential and universal is collision`` () =
    (
     let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
     let negation = ALC.Negation(concept)
     let individual = IriReference "http://example.org/individual"
     let role = ALC.Role.Iri(IriReference "http://example.org/role")
     let contradiction = ALC.Conjunction(ALC.Existential(role, ALC.Negation concept), ALC.Universal(role, concept))
     let individualAssertion = ALC.ConceptAssertion(individual, contradiction)
     let kb = ([], [individualAssertion])
     let state = ReasonerService.init kb
     let reasoningRules = ReasonerService.is_consistent state
     Assert.False(reasoningRules)
    )


[<Fact>]
let ``disjunctive is ok`` () =
    (
     let concept1 = ALC.ConceptName(IriTools.IriReference("http://example.org/concept1"))
     let negation = ALC.Negation(concept1)
     let individual = IriReference "http://example.org/individual"
     let disjunction = ALC.Disjunction(concept1, negation)
     let individualAssertion = ALC.ConceptAssertion(individual, disjunction)
     let kb = ([], [individualAssertion])
     let state = ReasonerService.init kb
     let reasoningRules = ReasonerService.is_consistent state
     Assert.True(reasoningRules)
    )


[<Fact>]
let ``query over disjunctive is ok`` () =
    (
     let concept1 = ALC.ConceptName(IriTools.IriReference("http://example.org/concept1"))
     let negation = ALC.Negation(concept1)
     let individual = IriReference "http://example.org/individual"
     let disjunction = ALC.Disjunction(concept1, negation)
     let individualAssertion = ALC.ConceptAssertion(individual, disjunction)
     let kb = ([], [individualAssertion])
     match QueryingService.init kb with
        | None -> Assert.False(true)
        | Some state -> 
            let types = QueryingService.query_individual_types state individual
            Assert.Equal(1, types.Length)
    )


[<Fact>]
let ``check queries over disjunctive is ok`` () =
    (
     let concept1 = ALC.ConceptName(IriTools.IriReference("http://example.org/concept1"))
     let negation = ALC.Negation(concept1)
     let individual = IriReference "http://example.org/individual"
     let disjunction = ALC.Disjunction(concept1, negation)
     let individualAssertion = ALC.ConceptAssertion(individual, disjunction)
     let kb = ([], [individualAssertion])
     match QueryingService.init kb with
        | None -> Assert.False(true)
        | Some state -> 
            let answers = AlcTableau.ConjunctiveQueryAnswer.answer state [ConceptQuery(Concept concept1, Individual individual)] { IndividualMap = Map.empty; RoleMap = Map.empty; ConceptMap = Map.empty }
            Assert.Equal(0, answers |> List.length)
    )


[<Fact>]
let ``realisation query over single negation is ok`` () =
    (
     let concept1 = ALC.ConceptName(IriTools.IriReference("http://example.org/concept1"))
     let individual = IriReference "http://example.org/individual"
     let negation = ALC.Negation(concept1)
     let individualAssertion = ALC.ConceptAssertion(individual, negation)
     let kb = ([], [individualAssertion])
     match QueryingService.init kb with
        | None -> Assert.False(true)
        | Some state -> 
            let answers = AlcTableau.ConjunctiveQueryAnswer.answer state [ConceptQuery(Concept concept1, Variable "v")] { IndividualMap = Map.empty; RoleMap = Map.empty; ConceptMap = Map.empty }
            Assert.Equal(0, answers |> List.length)
    )

[<Fact>]
let ``realisation query over single assertion is ok`` () =
    (
     let concept1 = ALC.ConceptName(IriTools.IriReference("http://example.org/concept1"))
     let individual = IriReference "http://example.org/individual"
     let individualAssertion = ALC.ConceptAssertion(individual, concept1)
     let kb = ([], [individualAssertion])
     match QueryingService.init kb with
        | None -> Assert.False(true)
        | Some state -> 
            let answers = AlcTableau.ConjunctiveQueryAnswer.answer state [ConceptQuery(Concept concept1, Variable "v")] { IndividualMap = Map.empty; RoleMap = Map.empty; ConceptMap = Map.empty }
            Assert.Equal(1, answers.Length)
    )



[<Fact>]
let ``positive class assertion query works`` () =
     let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
     let individual = IriReference "http://example.org/individual"
     let individualAssertion = ALC.ConceptAssertion(individual, concept)
     let kb = ([], [individualAssertion])
     match QueryingService.init kb with
        | None -> Assert.False(true)
        | Some state -> 
            let hasType = QueryingService.query_individual_types state individual
            Assert.Equal<Concept list>([concept], hasType)

[<Fact>]
let ``negative class assertion query works`` () =
     let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
     let individual = IriReference "http://example.org/individual"
     let individualAssertion = ALC.ConceptAssertion(individual, concept)
     let negation = ALC.NegativeAssertion individualAssertion
     let kb = ([], [negation])
     match QueryingService.init kb with
        | None -> Assert.False(true)
        | Some state -> 
            let hasType = QueryingService.query_individual_types state individual
            Assert.Equal<Concept list>([ALC.Negation concept], hasType)

[<Fact>]
let ``subclass check works`` () =
     let concept1 = ALC.ConceptName(IriTools.IriReference("http://example.org/concept1"))
     let concept2 = ALC.ConceptName(IriTools.IriReference("http://example.org/concept2"))
     let subclass_assertion = ALC.Inclusion(concept1, concept2)
     let reasoned_subclass = ReasonerService.is_subclass_of [subclass_assertion] concept1 concept2
     Assert.True(reasoned_subclass)
     let reasoned_subclass2 = ReasonerService.is_subclass_of [subclass_assertion] concept2 concept1
     Assert.False(reasoned_subclass2)


[<Fact>]
let ``checking over  disjunctive is ok`` () =
     let concept1 = ALC.ConceptName(IriTools.IriReference("http://example.org/concept1"))
     let negation = ALC.Negation(concept1)
     let individual = IriReference "http://example.org/individual"
     let disjunction = ALC.Disjunction(concept1, negation)
     let individualAssertion = ALC.ConceptAssertion(individual, disjunction)
     let kb = ([], [individualAssertion])
     match ReasonerService.init kb with
        | Tableau.InConsistent _ -> Assert.False(true)
        | Tableau.Consistent state -> 
            let hasType = ReasonerService.check_individual_type state individual concept1
            Assert.False(hasType)

[<Fact>]
let ``simplest query is ok`` () =
    (
     let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
     let individual = IriReference "http://example.org/individual"
     let individualAssertion = ALC.ConceptAssertion(individual, concept)
     let kb = ([], [individualAssertion])
     match QueryingService.init kb with
        | None -> Assert.False(true)
        | Some state -> 
            let types = QueryingService.query_individual_types state individual
            Assert.Equal<Concept list>([concept], types)
    )


[<Fact>]
let ``checking over simplest is ok`` () =
    (
     let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
     let individual = IriReference "http://example.org/individual"
     let individualAssertion = ALC.ConceptAssertion(individual, concept)
     let kb = ([], [individualAssertion])
     match ReasonerService.init kb with
        | Tableau.InConsistent _ -> Assert.False(true)
        | Tableau.Consistent state -> 
            let hasType = ReasonerService.check_individual_type state individual concept
            Assert.True(hasType)
    )


[<Fact>]
let ``disjunctive collision is handles`` () =
    (
     let concept1 = ALC.ConceptName(IriTools.IriReference("http://example.org/concept1"))
     let concept2 = ALC.ConceptName(IriTools.IriReference("http://example.org/concept2"))
     let concept3 = ALC.ConceptName(IriTools.IriReference("http://example.org/concept3"))
     let negation = ALC.Negation(concept1)
     let individual = IriReference "http://example.org/individual"
     let contradiction1 = ALC.Conjunction(concept1, ALC.Negation concept1)
     let contradiction2 = ALC.Conjunction(concept2, ALC.Negation concept2)
     let disjunction = ALC.Disjunction(contradiction1, contradiction2)
     let individualAssertion = ALC.ConceptAssertion(individual, disjunction)
     let kb = ([], [individualAssertion])
     let state = ReasonerService.init kb
     let reasoningRules = ReasonerService.is_consistent state
     Assert.False(reasoningRules)
    )
