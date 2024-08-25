(*

    Copyright (C) 2024 Dag Hovland
    
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    
    Contact: hovlanddag@gmail.com

*)
module AlcTableau.QueryingService

open System
open System.Collections.Generic
open AlcTableau.ALC
open IriTools

type QueryingCache = {
    reasoner_state : Tableau.ReasonerState
    known_concept_individuals : Map<ALC.Concept, IriReference list>
    known_object_predicate_index : Map<(IriReference * ALC.Role), IriReference list>
    known_subject_predicate_index : Map<(IriReference * ALC.Role), IriReference list>
    probable_concept_individuals : Map<ALC.Concept, IriReference list>
    probable_object_predicate_index : Map<(IriReference * ALC.Role), IriReference list>
    probable_subject_predicate_index : Map<(IriReference * ALC.Role), IriReference list>
}

let cache_concept_answers (reasoner_state : Tableau.ReasonerState) =
    let knownObjectPredicateIndex = MapUtils.invert_assertions_map reasoner_state.known_role_assertions
    let probableObjectPredicateIndex = MapUtils.invert_assertions_map reasoner_state.probable_role_assertions
    { reasoner_state = reasoner_state
      known_concept_individuals = MapUtils.invert_assertions_map reasoner_state.known_concept_assertions
      known_object_predicate_index = knownObjectPredicateIndex
      probable_concept_individuals = MapUtils.invert_assertions_map reasoner_state.probable_concept_assertions
      probable_object_predicate_index =  probableObjectPredicateIndex
      probable_subject_predicate_index = MapUtils.invert_index_map knownObjectPredicateIndex
      known_subject_predicate_index = MapUtils.invert_index_map probableObjectPredicateIndex 
    }

let init (kb : ALC.knowledgeBase) =
    match ReasonerService.init kb with
    | Tableau.InConsistent _ -> None
    | Tableau.Consistent reasoner_state -> Some (cache_concept_answers reasoner_state)
    

/// This is like "SELECT ?individual WHERE { ?individual a <concept> }"
/// Called "instance retrieval" in DL litterature
let query_concept_individual (query_cache : QueryingCache) (concept : ALC.Concept) =
    let known_individuals = query_cache.known_concept_individuals.GetValueOrDefault(concept, [])
    let inferred_individuals = (query_cache.probable_concept_individuals.GetValueOrDefault(concept, [])
        |> List.filter (fun i -> not (known_individuals |> List.contains i))
        |> List.filter (fun i -> ReasonerService.check_individual_type query_cache.reasoner_state i concept)
    )
    known_individuals @ inferred_individuals
    
/// This is like "SELECT ?type WHERE { <individual> a ?type }"
let query_individual_types (query_cache : QueryingCache) (individual : IriReference) =
    ReasonerService.get_individual_types query_cache.reasoner_state individual
    
/// This is like "SELECT ?individual WHERE { ?individual <role> <concept> }"
let query_role_object (query_cache : QueryingCache) (role : ALC.Role) (object : IriReference) =
    let known_individuals = query_cache.known_object_predicate_index.GetValueOrDefault((object, role), [])
    let inferred_individuals = (query_cache.probable_object_predicate_index.GetValueOrDefault((object, role), [])
        |> List.filter (fun i -> not (known_individuals |> List.contains i))
        |> List.filter (fun i -> ReasonerService.check_assertion query_cache.reasoner_state (ALC.RoleAssertion (i, object, role)))
    )
    known_individuals @ inferred_individuals