module AlcTableau.Tableau


open System
open System.Collections.Generic
open AlcTableau.ALC
open IriTools

type ReasonerState = {
    concept_assertions : Map<IriReference, Concept list>
    role_assertions : Map<IriReference, (IriReference * Role) list>
    subclass_assertions : Map<Concept, Concept list>
}

type ReasoningResult =
    | Consistent of ABoxAssertion list
    | InConsistent 

let add_concept_assertion(state: ReasonerState) (key:IriReference) (value : Concept list) =
    let orig_values = state.concept_assertions.GetValueOrDefault(key, [])
    { state with concept_assertions = state.concept_assertions.Add(key, orig_values @ value) }
        
let add_role_assertion(state: ReasonerState) (key:IriReference) (value : (IriReference * Role) list) =
    let orig_values = state.role_assertions.GetValueOrDefault(key, [])
    { state with role_assertions = state.role_assertions.Add(key, orig_values @ value) }


let add_subclass_assertion(state: ReasonerState) (key:Concept) (value : Concept) =
    let orig_values = state.subclass_assertions.GetValueOrDefault(key, [])
    { state with subclass_assertions = state.subclass_assertions.Add(key, orig_values @ [value]) }


let individual_is_asserted_concept state (concept : Concept) (individual : IriReference) =
    state.concept_assertions.ContainsKey(individual) && state.concept_assertions[individual] |> List.contains concept
                         
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
    
let add_assertion_list (state : ReasonerState) (new_assertions) =
    new_assertions
    |> List.fold (fun state new_assertion -> add_assertion state new_assertion) state

let init_abox_expander (abox : ABoxAssertion list) init_state  =
    abox |> List.fold (fun state assertion ->
        match assertion with
        | ConceptAssertion (individual, concept) -> add_concept_assertion state individual [concept]
        | RoleAssertion (left, right, role) -> add_role_assertion state left [(right, role)])
        init_state

let init_tbox_expander (tbox : TBoxAxiom list) init_state  =
    tbox |> List.fold (fun state assertion ->
        match assertion with
        | Inclusion (c1, c2) -> 
            add_subclass_assertion state c1 c2
        | _ -> state )
        init_state

let init_expander ((tbox, abox) : ALC.knowledgeBase) =
    { concept_assertions = Map.empty; role_assertions = Map.empty; subclass_assertions = Map.empty }
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
    | _ -> []
        

let expandAssertion state  (assertion : ALC.ABoxAssertion) =
    match assertion with
    | ConceptAssertion (individual, ALC.Conjunction(C,D)) ->
        (if individual_is_asserted_concept state C individual && individual_is_asserted_concept state D individual then
            []
        else
            [[ALC.ConceptAssertion(individual, C) ; ALC.ConceptAssertion(individual, D)]])
    | ConceptAssertion (individual, ALC.Disjunction(C,D)) ->
         (if (individual_is_asserted_concept state C individual || individual_is_asserted_concept state D individual) then
            []
        else
            [[ALC.ConceptAssertion(individual, C)];[ALC.ConceptAssertion(individual, D)]]
        )
    | ConceptAssertion (individual, ALC.Existential(role, concept)) ->
        if not (state.role_assertions.GetValueOrDefault(individual,[])
                |> List.exists (fun (right, r) -> r = role && individual_is_asserted_concept state concept right )) then
            let new_individual = new IriReference($"https://alctableau.example.com/anonymous#{Guid.NewGuid()}")
            [[ALC.ConceptAssertion(new_individual, concept); ALC.RoleAssertion(individual, new_individual, role)]]
        else
            []
    | ConceptAssertion (individual, ALC.Universal(role, concept)) ->
        state.role_assertions.GetValueOrDefault(individual,[])
            |> List.where (fun (right, r) -> r = role && not (individual_is_asserted_concept state concept right))
            |> List.map (fun (right, r) -> [ALC.ConceptAssertion(right, concept)])
    | ConceptAssertion (_individual, ALC.Bottom)  -> []
    | ConceptAssertion (_individual, ALC.Top)  -> []
    | ConceptAssertion (_individual, ALC.ConceptName(_iri))  -> []
    | ConceptAssertion (_individual, ALC.Negation(_concept))  -> []
    | RoleAssertion (left, right, role) ->
        state.concept_assertions.GetValueOrDefault(left,[])
            |> List.filter (fun concept ->
                match concept with
                | ALC.Universal (r, c) -> (r = role && not ( individual_is_asserted_concept state c right))
                | _ -> false) 
            |> List.map (fun concept ->
                match concept with
                | ALC.Universal(r, c) -> [ALC.ConceptAssertion(right, c)]
                | _ -> raise (new Exception("Only universals should have passed through the filter above")))

let rec expand (state : ReasonerState) (nextAssertions : ABoxAssertion list list list) =
    match nextAssertions with
    | [] -> true
    | [] :: restAssertions -> expand state restAssertions
    | nextAssertion :: restAssertions ->
        nextAssertion |> List.exists (fun assertion_choice ->        
                let new_state = add_assertion_list state assertion_choice
                (
                        if (assertion_choice |> List.forall ( fun a -> not (has_new_collision new_state a))) then
                            let inferredAssertions = assertion_choice |> List.map (expandAxiom  new_state) |> List.where (fun x -> not x.IsEmpty)
                            let newAssertions = assertion_choice |> List.map (expandAssertion new_state) |> List.where (fun x -> not x.IsEmpty)
                            expand new_state (restAssertions @ [inferredAssertions] @ newAssertions )
                        else
                            false
                )
        )
    
let is_consistent (kb : ALC.knowledgeBase) =
    let normalized_kb = NNF.nnf_kb kb
    let reasoner_state = init_expander normalized_kb
    let (tbox, abox) = normalized_kb
    if abox |> List.exists (has_new_collision reasoner_state)  then
        false
    else
        expand reasoner_state ([[abox]])