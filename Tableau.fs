module AlcTableau.Tableau


open System
open System.Collections.Generic
open AlcTableau.ALC
open IriTools

type ReasoningResult =
    | Consistent of ABoxAssertion list
    | InConsistent 

let add_element_to_dict_list<'T> (dict : Map<IriReference, 'T list>) (key:IriReference) (value : 'T list) =
    let orig_values = dict.GetValueOrDefault(key, [])
    dict.Add(key, orig_values @ value)


                         
let has_new_collision (concept_assertions : Map<IriReference, Concept list>) (new_assertion)   =
    match new_assertion with
    | ConceptAssertion (individual, ALC.ConceptName(C)) ->
        concept_assertions.ContainsKey(individual) && concept_assertions[individual] |> List.contains (ALC.Negation (ALC.ConceptName(C)))
    | ConceptAssertion (individual, ALC.Negation (ALC.ConceptName(C))) ->
        concept_assertions.ContainsKey(individual) && concept_assertions[individual] |> List.contains (ALC.ConceptName(C))
    | _ -> false

let add_assertion (concept_assertions : Map<IriReference, Concept list>) (role_assertions: Map<IriReference, (IriReference * Role) list>) (new_assertion) =
    match new_assertion with
    | ConceptAssertion (individual, concept) -> (add_element_to_dict_list concept_assertions individual [concept], role_assertions)
    | RoleAssertion (left, right, role) -> (concept_assertions,  add_element_to_dict_list role_assertions left [(right, role)])

let add_assertion_list (concept_assertions : Map<IriReference, Concept list>) (role_assertions: Map<IriReference, (IriReference * Role) list>) (new_assertions) =
    new_assertions
    |> List.fold (fun (concepts, roles) new_assertion -> add_assertion concepts roles new_assertion) (concept_assertions, role_assertions)


let init_expander (Abox : ABoxAssertion list)  =
    Abox |> List.fold (fun (individual_concept_map, individual_role_map) assertion ->
        match assertion with
        | ConceptAssertion (individual, concept) -> (add_element_to_dict_list individual_concept_map individual [concept], individual_role_map)
        | RoleAssertion (left, right, role) -> (individual_concept_map, add_element_to_dict_list individual_role_map left [(right, role)]))
        (Map.empty, Map.empty)

let expandAssertion (role_assertions : Map<IriReference, (IriReference * Role) list>)
    (concept_assertions : Map<IriReference, Concept list>)
    (assertion : ALC.ABoxAssertion) =
    match assertion with
    | ConceptAssertion (individual, ALC.Conjunction(C,D)) ->
        let indAssertions = concept_assertions[individual]
        if (indAssertions |> List.contains C) && (indAssertions |> List.contains D)   then
            []
        else
            [[ALC.ConceptAssertion(individual, C) ; ALC.ConceptAssertion(individual, D)]]
    | ConceptAssertion (individual, ALC.Disjunction(C,D)) -> 
        [[ALC.ConceptAssertion(individual, C)];[ALC.ConceptAssertion(individual, D)]]
        
    | ConceptAssertion (individual, ALC.Existential(role, concept)) ->
        if not (role_assertions.ContainsKey(individual) &&
           role_assertions[individual] |> List.exists (fun (right, r) -> r = role && concept_assertions[right] |> List.contains concept)) then
            let new_individual = new IriReference($"https://alctableau.example.com/anonymous#{Guid.NewGuid()}")
            [[ALC.ConceptAssertion(new_individual, concept); ALC.RoleAssertion(individual, new_individual, role)]]
        else
            []
    | ConceptAssertion (individual, ALC.Universal(role, concept)) ->
        if role_assertions.ContainsKey(individual) then
            role_assertions[individual]
            |> List.where (fun (right, r) -> r = role && not (concept_assertions[right] |> List.contains concept))
            |> List.map (fun (right, r) -> [ALC.ConceptAssertion(right, concept)])
        else
            []
    | _ -> []

let rec expand (concepts : Map<IriReference, Concept list>) (roles : Map<IriReference, (IriReference * Role) list>) (nextAssertions : ABoxAssertion list list) =
    (match nextAssertions with
    | [] -> true
    | nextAssertion :: restAssertions ->
        nextAssertion |> List.where (fun assertion ->
            let (newConcepts, newRoles) = add_assertion concepts roles assertion
            if has_new_collision newConcepts assertion then
                false
            else
                let newAssertions = expandAssertion newRoles newConcepts assertion
                expand newConcepts newRoles (newAssertions @ restAssertions))
        |> List.isEmpty |> not)
        
    
    
let reasoner (Kb : ALC.knowledgeBase) =
    let (concepts, roles) = init_expander (snd Kb)
    if ((snd Kb) |> List.where (has_new_collision concepts)) = [] then
        expand concepts roles ([snd Kb])
    else
        false