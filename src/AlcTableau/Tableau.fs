(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.    
    Contact: hovlanddag@gmail.com
*)


module DagSemTools.AlcTableau.Tableau


open System
open System.Collections.Generic
open ALC
open IriTools
open MapUtils

/// The reasoner can be slightly faster if we only check for consistency
type ReasonerFunctionality =
       | OnlyConsistencty
       | PrepareQueryingCache

type ReasonerState = {
    known_concept_assertions : Map<IriReference, Concept list>
    probable_concept_assertions : Map<IriReference, Concept list>
    known_role_assertions : Map<IriReference, (IriReference * Role) list>
    probable_role_assertions : Map<IriReference, (IriReference * Role) list>
    negative_role_assertion : Map<IriReference, (IriReference * Role) list>
    subclass_assertions : Map<Concept, Concept list>
    functionality : ReasonerFunctionality
}

type ReasoningResult = 
    | Consistent of ReasonerState
    | InConsistent of ReasonerState

let internal is_consistent_result (result : ReasoningResult) =
    match result with
    | Consistent _ -> true
    | InConsistent _ -> false

type internal NewAssertions =
    | Disjunction of NewAssertions list
    | Known of ABoxAssertion list
    | Nothing

let internal new_assertion_is_non_empty (new_assertion : NewAssertions) =
    match new_assertion with
    | Disjunction assertions -> assertions.Length > 0
    | Known assertions -> assertions.Length > 0
    | Nothing -> false


let internal add_concept_assertion(state: ReasonerState) (key:IriReference) (value : Concept list) =
    { state with known_concept_assertions = addToMapList state.known_concept_assertions key value }
        
let internal add_role_assertion(state: ReasonerState) (key:IriReference) (value : (IriReference * Role) list) =
    { state with known_role_assertions =  addToMapList state.known_role_assertions key value }

let internal add_subclass_assertion(state: ReasonerState) (key:Concept) (value : Concept) =
    { state with subclass_assertions = addToMapList state.subclass_assertions key [value] }


let internal individual_is_asserted_concept state (concept : Concept) (individual : IriReference) =
    state.known_concept_assertions.ContainsKey(individual) && state.known_concept_assertions[individual] |> List.contains concept
                         
let internal has_new_collision state (new_assertion)   =
    match new_assertion with
    | ConceptAssertion (individual, ALC.ConceptName(C)) ->
        individual_is_asserted_concept state (ALC.Negation (ALC.ConceptName(C))) individual   
    | ConceptAssertion (individual, ALC.Negation (ALC.ConceptName(C))) ->
       individual_is_asserted_concept state (ALC.ConceptName(C)) individual  
    | _ -> false

let internal add_assertion (state : ReasonerState) (new_assertion) =
    match new_assertion with
    | ConceptAssertion (individual, concept) -> add_concept_assertion state individual [concept]
    | RoleAssertion (left, right, role) -> add_role_assertion state left [(right, role)]
    | _ -> state
    
let internal add_assertion_list (state : ReasonerState) (new_assertions) =
    new_assertions
    |> List.fold (fun state new_assertion -> add_assertion state new_assertion) state

let internal init_abox_expander (abox : ABoxAssertion list) init_state  =
    abox |> List.fold (fun state assertion ->
        match assertion with
        | ConceptAssertion (individual, concept) -> add_concept_assertion state individual [concept]
        | RoleAssertion (left, right, role) -> add_role_assertion state left [(right, role)]
        | _ -> state )
        init_state

let internal init_tbox_expander (tbox : TBoxAxiom list) init_state  =
    tbox |> List.fold (fun state assertion ->
        match assertion with
        | Inclusion (c1, c2) -> 
            add_subclass_assertion state c1 c2 )
        init_state

let internal create_negative_role_assertion_map (abox : ABoxAssertion list) =
    abox |> List.choose (fun assertion -> 
        match assertion with
        | NegativeRoleAssertion (left, right, role) -> Some (left, (right, role))
        | _ -> None)
        |> List.fold (fun map (left, (right, role)) -> addToMapList map left [(right, role)]) Map.empty

let internal init_expander ((tbox, abox) : ALC.knowledgeBase) =
    { known_concept_assertions = Map.empty
      probable_concept_assertions = Map.empty
      known_role_assertions = Map.empty
      negative_role_assertion = create_negative_role_assertion_map abox
      probable_role_assertions = Map.empty
      subclass_assertions = Map.empty
      functionality = PrepareQueryingCache
    }
    |> init_abox_expander abox
    |> init_tbox_expander tbox

let internal expandAxiom state new_assertion =
    match new_assertion with
    | ConceptAssertion (individual, C) -> 
        state.subclass_assertions.GetValueOrDefault(C, [])
        |> List.where (fun superclass -> 
            not (individual_is_asserted_concept state superclass individual)
            )
        |> List.map (fun superclass -> 
            ALC.ConceptAssertion(individual, superclass))
        |> NewAssertions.Known
    | _ -> NewAssertions.Nothing
        
let internal expandConjunction state C D individual =
            (if individual_is_asserted_concept state C individual && individual_is_asserted_concept state D individual then
                NewAssertions.Nothing
            else
                NewAssertions.Known [ALC.ConceptAssertion(individual, C) ; ALC.ConceptAssertion(individual, D)])
            
let internal expandDisjunction state C D individual =
            if (individual_is_asserted_concept state C individual || individual_is_asserted_concept state D individual) then
               NewAssertions.Nothing
            else
                NewAssertions.Disjunction [
                                       NewAssertions.Known [ALC.ConceptAssertion(individual, C)]
                                       NewAssertions.Known [ALC.ConceptAssertion(individual, D)]
                                       ]
let internal expandExistential state role concept individual =
    if not (state.known_role_assertions.GetValueOrDefault(individual,[])
                |> List.exists (fun (right, r) -> r = role && individual_is_asserted_concept state concept right )) then
            let new_individual = new IriReference($"https://alctableau.example.com/anonymous#{Guid.NewGuid()}")
            NewAssertions.Known [ALC.ConceptAssertion(new_individual, concept); ALC.RoleAssertion(individual, new_individual, role)]
        else
            NewAssertions.Nothing
            
let internal expandUniversal state role concept individual =
    state.known_role_assertions.GetValueOrDefault(individual,[])
            |> List.where (fun (right, r) -> r = role && not (individual_is_asserted_concept state concept right))
            |> List.map (fun (right, r) -> ALC.ConceptAssertion(right, concept))
            |> NewAssertions.Known
            
let internal expandRoleAssertion state left right role =
    state.known_concept_assertions.GetValueOrDefault(left,[])
            |> List.filter (fun concept ->
                match concept with
                | ALC.Universal (r, c) -> (r = role && not ( individual_is_asserted_concept state c right))
                | _ -> false) 
            |> List.map (fun concept ->
                match concept with
                | ALC.Universal(r, c) -> ALC.ConceptAssertion(right, c)
                | _ -> raise (new Exception("Only universals should have passed through the filter above")))
            |> NewAssertions.Known
let internal expandAssertion state  (assertion : ALC.ABoxAssertion) =
    match assertion with
    | ConceptAssertion (individual, ALC.Conjunction(C,D)) -> expandConjunction state C D individual
    | ConceptAssertion (individual, ALC.Disjunction(C,D)) -> expandDisjunction state C D individual
    | ConceptAssertion (individual, ALC.Existential(role, concept)) -> expandExistential state role concept individual 
    | ConceptAssertion (individual, ALC.Universal(role, concept)) -> expandUniversal state role concept individual
    | ConceptAssertion (_individual, ALC.Bottom)  -> NewAssertions.Nothing
    | ConceptAssertion (_individual, ALC.Top)  -> NewAssertions.Nothing
    | ConceptAssertion (_individual, ALC.ConceptName(_iri))  -> NewAssertions.Nothing
    | ConceptAssertion (_individual, ALC.Negation(_concept))  -> NewAssertions.Nothing
    | LiteralAnnotationAssertion (_individual, _property, _right)  -> NewAssertions.Nothing
    | ObjectAnnotationAssertion (_individual, _property, _value)  -> NewAssertions.Nothing
    | RoleAssertion (left, right, role) -> expandRoleAssertion state left right role
    | LiteralAssertion(_individual, _property, _right) -> NewAssertions.Nothing
    | NegativeRoleAssertion(_individual, _right, _role) -> failwith "Negative roles are not supported"
    | NegativeAssertion(_assertion) -> failwith "Negative abox assertions are not supported"
    | NegativeLiteralAssertion(individual, property, value) -> failwith "todo"
    

let rec internal expand (state : ReasonerState) (nextAssertions : NewAssertions list) =
    match nextAssertions with
    | [] -> Consistent state
    | NewAssertions.Nothing :: restAssertions -> expand state restAssertions
    | NewAssertions.Known assertion_choice :: restAssertions ->
            let new_state = add_assertion_list state assertion_choice
            (
                if (assertion_choice |> List.forall ( fun a -> not (has_new_collision new_state a))) then
                    let inferredAssertions = assertion_choice |> List.map (expandAxiom  new_state) |> List.where new_assertion_is_non_empty
                    let newAssertions = assertion_choice |> List.map (expandAssertion new_state) |> List.where new_assertion_is_non_empty
                    expand new_state (restAssertions @ inferredAssertions @ newAssertions )
                else
                    InConsistent state
            )
    | (NewAssertions.Disjunction assertion_disjunction) :: restAssertions ->
        match state.functionality with
        | OnlyConsistencty -> handleDisjunctionConsistency state assertion_disjunction restAssertions
        | PrepareQueryingCache -> expandDisjunctionAssertion state assertion_disjunction restAssertions
and internal expandDisjunctionAssertion (state : ReasonerState) (assertion_disjunction : NewAssertions list) restAssertions =
    let positive_states = (assertion_disjunction
            |> List.map (fun assertion -> expand state (assertion :: restAssertions))
            |> List.choose (fun x -> match x with | Consistent s -> Some s | InConsistent _ -> None))
    match positive_states with
            | [] -> InConsistent state
            | [new_state] -> Consistent new_state
            | stateList ->
                let possible_assertions = mergeMapList
                                              state.probable_concept_assertions
                                              (stateList |> List.map (fun state -> mergeMaps state.known_concept_assertions state.probable_concept_assertions))
                let possible_roles = mergeMapList
                                         state.probable_role_assertions
                                         (stateList |> List.map (fun state -> mergeMaps state.known_role_assertions state.probable_role_assertions))
                Consistent { state with probable_concept_assertions = possible_assertions ; probable_role_assertions = possible_roles }
                
and internal handleDisjunctionConsistency (state : ReasonerState) (assertion_disjunction : NewAssertions list) restAssertions =
    match (
        assertion_disjunction
                                |> List.map (fun assertion -> expand state (assertion :: restAssertions))
                                |> List.tryHead
        ) with 
        | Some s -> s
        | None -> InConsistent state        
    