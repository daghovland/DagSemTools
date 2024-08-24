module AlcTableau.Tableau


open System
open System.Collections.Generic
open AlcTableau.ALC
open IriTools

type ReasonerState = {
    known_concept_assertions : Map<IriReference, Concept list>
    probable_concept_assertions : Map<IriReference, Concept list>
    known_role_assertions : Map<IriReference, (IriReference * Role) list>
    probable_role_assertions : Map<IriReference, (IriReference * Role) list>
    subclass_assertions : Map<Concept, Concept list>
}

type ReasoningResult = 
    | Consistent of ReasonerState
    | InConsistent of ReasonerState

let is_consistent_result (result : ReasoningResult) =
    match result with
    | Consistent _ -> true
    | InConsistent _ -> false

type NewAssertions =
    | Disjunction of NewAssertions list
    | Known of ABoxAssertion list
    | Nothing

let new_assertion_is_non_empty (new_assertion : NewAssertions) =
    match new_assertion with
    | Disjunction assertions -> assertions.Length > 0
    | Known assertions -> assertions.Length > 0
    | Nothing -> false


let addToList l v =
       if l |> List.contains v
       then l else l @ [v]
let mergeLists l1 l2 = l2 |> List.fold addToList l1
let mergeMaps (map1: Map<IriReference, 'T list>) (map2: Map<IriReference, 'T list>) =
    map2 |> Map.fold (fun acc key value ->
        let combinedValue = 
            match Map.tryFind key acc with
            | Some existingValue -> value |> List.fold addToList existingValue 
            | None -> value
        acc.Add(key, combinedValue)
    ) map1

let mergeThreeMaps (map1: Map<IriReference, 'T list>) (map2: Map<IriReference, 'T list>) (map3: Map<IriReference, 'T list>) =
    map1 |> mergeMaps map2 |> mergeMaps map3
    
let mergeMapList map (maps : Map<IriReference, 'T list> list) =
    maps |> List.fold (fun acc map -> mergeMaps acc map) map
    

let add_concept_assertion(state: ReasonerState) (key:IriReference) (value : Concept list) =
    let orig_values = state.known_concept_assertions.GetValueOrDefault(key, [])
    { state with known_concept_assertions =  state.known_concept_assertions.Add(key, mergeLists orig_values value) }
        
let add_role_assertion(state: ReasonerState) (key:IriReference) (value : (IriReference * Role) list) =
    let orig_values = state.known_role_assertions.GetValueOrDefault(key, [])
    { state with known_role_assertions =  state.known_role_assertions.Add(key, mergeLists orig_values value) }

let add_subclass_assertion(state: ReasonerState) (key:Concept) (value : Concept) =
    let orig_values = state.subclass_assertions.GetValueOrDefault(key, [])
    { state with subclass_assertions = state.subclass_assertions.Add(key, orig_values @ [value]) }


let individual_is_asserted_concept state (concept : Concept) (individual : IriReference) =
    state.known_concept_assertions.ContainsKey(individual) && state.known_concept_assertions[individual] |> List.contains concept
                         
let has_new_collision state (new_assertion)   =
    match new_assertion with
    | ConceptAssertion (individual, ALC.ConceptName(C)) ->
        individual_is_asserted_concept state (ALC.Negation (ALC.ConceptName(C))) individual   
    | ConceptAssertion (individual, ALC.Negation (ALC.ConceptName(C))) ->
       individual_is_asserted_concept state (ALC.ConceptName(C)) individual  
    | _ -> false

let add_assertion (state : ReasonerState) (new_assertion) =
    match new_assertion with
    | ConceptAssertion (individual, concept) -> add_concept_assertion state individual [concept]
    | RoleAssertion (left, right, role) -> add_role_assertion state left [(right, role)]
    | _ -> state
    
let add_assertion_list (state : ReasonerState) (new_assertions) =
    new_assertions
    |> List.fold (fun state new_assertion -> add_assertion state new_assertion) state

let init_abox_expander (abox : ABoxAssertion list) init_state  =
    abox |> List.fold (fun state assertion ->
        match assertion with
        | ConceptAssertion (individual, concept) -> add_concept_assertion state individual [concept]
        | RoleAssertion (left, right, role) -> add_role_assertion state left [(right, role)]
        | _ -> state )
        init_state

let init_tbox_expander (tbox : TBoxAxiom list) init_state  =
    tbox |> List.fold (fun state assertion ->
        match assertion with
        | Inclusion (c1, c2) -> 
            add_subclass_assertion state c1 c2
        | _ -> state )
        init_state

let init_expander ((tbox, abox) : ALC.knowledgeBase) =
    { known_concept_assertions = Map.empty; probable_concept_assertions = Map.empty; known_role_assertions = Map.empty; probable_role_assertions = Map.empty; subclass_assertions = Map.empty }
    |> init_abox_expander abox
    |> init_tbox_expander tbox

let expandAxiom state new_assertion =
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
        

let expandAssertion state  (assertion : ALC.ABoxAssertion) =
    match assertion with
    | ConceptAssertion (individual, ALC.Conjunction(C,D)) ->
        (if individual_is_asserted_concept state C individual && individual_is_asserted_concept state D individual then
            NewAssertions.Nothing
        else
            NewAssertions.Known [ALC.ConceptAssertion(individual, C) ; ALC.ConceptAssertion(individual, D)])
    | ConceptAssertion (individual, ALC.Disjunction(C,D)) ->
         (if (individual_is_asserted_concept state C individual || individual_is_asserted_concept state D individual) then
            NewAssertions.Nothing
        else
            NewAssertions.Disjunction [
                                       NewAssertions.Known [ALC.ConceptAssertion(individual, C)]
                                       NewAssertions.Known [ALC.ConceptAssertion(individual, D)]
                                       ]
        )
    | ConceptAssertion (individual, ALC.Existential(role, concept)) ->
        if not (state.known_role_assertions.GetValueOrDefault(individual,[])
                |> List.exists (fun (right, r) -> r = role && individual_is_asserted_concept state concept right )) then
            let new_individual = new IriReference($"https://alctableau.example.com/anonymous#{Guid.NewGuid()}")
            NewAssertions.Known [ALC.ConceptAssertion(new_individual, concept); ALC.RoleAssertion(individual, new_individual, role)]
        else
            NewAssertions.Nothing
    | ConceptAssertion (individual, ALC.Universal(role, concept)) ->
        state.known_role_assertions.GetValueOrDefault(individual,[])
            |> List.where (fun (right, r) -> r = role && not (individual_is_asserted_concept state concept right))
            |> List.map (fun (right, r) -> ALC.ConceptAssertion(right, concept))
            |> NewAssertions.Known
    | ConceptAssertion (_individual, ALC.Bottom)  -> NewAssertions.Nothing
    | ConceptAssertion (_individual, ALC.Top)  -> NewAssertions.Nothing
    | ConceptAssertion (_individual, ALC.ConceptName(_iri))  -> NewAssertions.Nothing
    | ConceptAssertion (_individual, ALC.Negation(_concept))  -> NewAssertions.Nothing
    | LiteralAnnotationAssertion (_individual, _property, _right)  -> NewAssertions.Nothing
    | ObjectAnnotationAssertion (_individual, _property, _value)  -> NewAssertions.Nothing
    | RoleAssertion (left, right, role) ->
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
    | LiteralAssertion(_individual, _property, _right) -> NewAssertions.Nothing
    | NegativeAssertion(_assertion) -> failwith "Negative abox assertions are not supported"
    

let rec expand (state : ReasonerState) (nextAssertions : NewAssertions list) =
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
        let positive_states = (assertion_disjunction
            |> List.map (fun assertion -> expand state (assertion :: restAssertions))
            |> List.where (is_consistent_result)
            |> List.map (fun x -> match x with| Consistent s -> s | _ -> failwith "This should not happen"))
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
    
let is_consistent (kb : ALC.knowledgeBase) =
    let normalized_kb = NNF.nnf_kb kb
    let reasoner_state = init_expander normalized_kb
    let (tbox, abox) = normalized_kb
    if abox |> List.exists (has_new_collision reasoner_state)  then
        false
    else
        expand reasoner_state ([NewAssertions.Known abox]) |> is_consistent_result