module AlcTableau.ReasonerService

open System
open System.Collections.Generic
open AlcTableau.ALC
open IriTools

open IriTools

let init (kb : ALC.knowledgeBase) =
    let normalized_kb = NNF.nnf_kb kb
    let reasoner_state = Tableau.init_expander normalized_kb
    let (tbox, abox) = normalized_kb
    if abox |> List.exists (Tableau.has_new_collision reasoner_state)  then
        Tableau.InConsistent reasoner_state
    else
        Tableau.expand reasoner_state ([Tableau.NewAssertions.Known abox]) 

let is_consistent (reasoner_state : Tableau.ReasoningResult) =
    Tableau.is_consistent_result reasoner_state
    
let check_individual_type (reasoner_state : Tableau.ReasonerState) (individual : IriReference) (concept : ALC.Concept) =
    let assertion = Tableau.Known [ALC.ConceptAssertion (individual, ALC.Negation concept)]
    Tableau.expand reasoner_state [assertion] |> Tableau.is_consistent_result |> not
    
let get_individual_types (reasoner_state : Tableau.ReasonerState) (individual : IriReference) =
    let easy_answers = reasoner_state.known_concept_assertions.GetValueOrDefault(individual, [])
    let inferred_answers =
       reasoner_state.probable_concept_assertions.GetValueOrDefault(individual, [])
       |> List.where (fun c -> not (easy_answers |> List.exists (fun c2 -> c2 = c)))
       |> List.where (fun c -> check_individual_type reasoner_state individual c)
    easy_answers @ inferred_answers

let is_satisfiable_class tbox concept =
    let individual = IriReference($"http://www.example.com/individual/{Guid.NewGuid()}")
    let assertion = ALC.ConceptAssertion (individual, concept)
    init (tbox, [assertion])
    |> Tableau.is_consistent_result
    
let is_subclass_of tbox sub super =
    let concept = ALC.Conjunction (sub, ALC.Negation super)
    is_satisfiable_class tbox concept |> not
    
let is_equivalent_class tbox c1 c2 =
    is_subclass_of tbox c1 c2 && is_subclass_of tbox c2 c1