module AlcTableau.Querying


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

type NewAssertions =
    | Disjunction of ABoxAssertion list
    | Known of ABoxAssertion list
    | Nothing

let new_assertion_is_non_empty (new_assertion : NewAssertions) =
    match new_assertion with
    | Disjunction assertions -> assertions.Length > 0
    | Known assertions -> assertions.Length > 0
    | Nothing -> false

type ReasoningResult =
    | Consistent of ABoxAssertion list
    | InConsistent 

let add_concept_assertion(state: ReasonerState) (key:IriReference) (value : Concept list) =
    let orig_values = state.known_concept_assertions.GetValueOrDefault(key, [])
    { state with known_concept_assertions =  state.known_concept_assertions.Add(key, orig_values @ value) }
        
let add_role_assertion(state: ReasonerState) (key:IriReference) (value : (IriReference * Role) list) =
    let orig_values = state.known_role_assertions.GetValueOrDefault(key, [])
    { state with known_role_assertions =  state.known_role_assertions.Add(key, orig_values @ value) }


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
            NewAssertions.Disjunction [ALC.ConceptAssertion(individual, C);ALC.ConceptAssertion(individual, D)]
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
    | [] -> true
    | NewAssertions.Nothing :: restAssertions -> expand state restAssertions
    | NewAssertions.Known assertion_choice :: restAssertions ->
            let new_state = add_assertion_list state assertion_choice
            (
                if (assertion_choice |> List.forall ( fun a -> not (has_new_collision new_state a))) then
                    let inferredAssertions = assertion_choice |> List.map (expandAxiom  new_state) |> List.where new_assertion_is_non_empty
                    let newAssertions = assertion_choice |> List.map (expandAssertion new_state) |> List.where new_assertion_is_non_empty
                    expand new_state (restAssertions @ inferredAssertions @ newAssertions )
                else
                    false
            )
    | NewAssertions.Disjunction assertion_disjunction :: restAssertions ->
        raise (NotImplementedException "Disjunctions are not supported yet")        
    
let is_consistent (kb : ALC.knowledgeBase) =
    let normalized_kb = NNF.nnf_kb kb
    let reasoner_state = init_expander normalized_kb
    let (tbox, abox) = normalized_kb
    if abox |> List.exists (has_new_collision reasoner_state)  then
        false
    else
        expand reasoner_state ([NewAssertions.Known abox])