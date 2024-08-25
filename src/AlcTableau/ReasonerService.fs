(*
    Copyright (C) 2024 Dag Hovland
    
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    
    Contact: hovlanddag@gmail.com
*)

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

/// Just Checks if kb has at least one interpretation, no preparation for querying
let check_consistency (kb : ALC.knowledgeBase) =
    let normalized_kb = NNF.nnf_kb kb
    let reasoner_state = Tableau.init_expander normalized_kb
    let (tbox, abox) = normalized_kb
    if abox |> List.exists (Tableau.has_new_collision reasoner_state)  then
        Tableau.InConsistent reasoner_state
    else
        Tableau.expand {reasoner_state with functionality = Tableau.ReasonerFunctionality.OnlyConsistencty} ([Tableau.NewAssertions.Known abox]) 

let is_consistent (reasoner_state : Tableau.ReasoningResult) =
    Tableau.is_consistent_result reasoner_state
    
/// Checks if individual is of concept in all interpretations of the knowledgebase
let check_individual_type (reasoner_state : Tableau.ReasonerState) (individual : IriReference) (concept : ALC.Concept) =
    let assertion = Tableau.Known [ALC.ConceptAssertion (individual, ALC.Negation concept)]
    Tableau.expand {reasoner_state with functionality = Tableau.ReasonerFunctionality.OnlyConsistencty} [assertion] |> Tableau.is_consistent_result |> not
    
/// Checks if there is an interpretation of tbox where concept has at least one instance
let is_satisfiable_class tbox concept =
    let individual = IriReference($"http://www.example.com/individual/{Guid.NewGuid()}")
    let assertion = ALC.ConceptAssertion (individual, concept)
    check_consistency (tbox, [assertion])
    |> Tableau.is_consistent_result

// Checks if sub <= super in tbox    
let is_subclass_of tbox sub super =
    let concept = ALC.Conjunction (sub, ALC.Negation super)
    is_satisfiable_class tbox concept |> not
    
// Checks if c1 = c2 in tbox
let is_equivalent_class tbox c1 c2 =
    is_subclass_of tbox c1 c2 && is_subclass_of tbox c2 c1
    
    
/// This is like "SELECT ?type WHERE { <individual> a ?type }"
/// Called realisation of the individual name in DL litterature
let get_individual_types (reasoner_state : Tableau.ReasonerState) (individual : IriReference) =
    let easy_answers = reasoner_state.known_concept_assertions.GetValueOrDefault(individual, [])
    let inferred_answers =
       reasoner_state.probable_concept_assertions.GetValueOrDefault(individual, [])
       |> List.where (fun c -> not (easy_answers |> List.contains c))
       |> List.where (fun c -> check_individual_type reasoner_state individual c)
    easy_answers @ inferred_answers

 
    
 /// Checks that assertion is true in all interpretations of the knowledgebase
let check_assertion (reasoner_state : Tableau.ReasonerState) (assertion : ALC.ABoxAssertion) =
    let assertion = Tableau.Known [ALC.NegativeAssertion assertion]
    Tableau.expand {reasoner_state with functionality=Tableau.ReasonerFunctionality.OnlyConsistencty} [assertion] |> Tableau.is_consistent_result |> not