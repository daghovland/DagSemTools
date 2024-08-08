module AlcTableau.Tableau


open System
open System.Collections.Generic
open AlcTableau.ALC
open IriTools

let add_element_to_dict_list<'T> (dict : Dictionary<IriReference, 'T list>) (key:IriReference) (value : 'T list) =
    let orig_values = dict.GetValueOrDefault(key, [])
    dict.Add(key, orig_values @ value)
                         
let rec expandAssertion (role_assertions : Dictionary<IriReference, (IriReference * Role) list>)
    (concept_assertions : Dictionary<IriReference, Concept list>)
    (assertion : ALC.ABoxAssertion) =
    match assertion with
    | ConceptAssertion (individual, ALC.Conjunction(C,D)) ->
        let indAssertions = concept_assertions[individual]
        if (indAssertions |> List.contains C) && (indAssertions |> List.contains D)   then
            []
        else
            add_element_to_dict_list concept_assertions individual [C; D]
            [ALC.ConceptAssertion(individual, C) ; ALC.ConceptAssertion(individual, D)]
    | ConceptAssertion (individual, ALC.Disjunction(C,D)) -> 
        add_element_to_dict_list concept_assertions individual [C]
        [ALC.ConceptAssertion(individual, C)]
        // TODO add branching
    | ConceptAssertion (individual, ALC.Existential(role, concept)) ->
        if not (role_assertions.ContainsKey(individual) &&
           role_assertions[individual] |> List.exists (fun (right, r) -> r = role && concept_assertions[right] |> List.contains concept)) then
            let new_individual = new IriReference($"https://alctableau.example.com/anonymous#{Guid.NewGuid()}")
            add_element_to_dict_list role_assertions individual [(new_individual, role)]
            add_element_to_dict_list concept_assertions new_individual [concept]
            [ALC.ConceptAssertion(new_individual, concept); ALC.RoleAssertion(individual, new_individual, role)]
        else
            []
    | ConceptAssertion (individual, ALC.Universal(role, concept)) ->
        if role_assertions.ContainsKey(individual) then
            role_assertions[individual]
            |> List.where (fun (right, r) -> r = role && not (concept_assertions[right] |> List.contains concept))
            |> List.map (fun (right, r) -> ALC.ConceptAssertion(right, concept))
        else
            []


let rec expand (concepts : Dictionary<IriReference, Concept list>) (roles) (nextAssertions : ALC.ABoxAssertion list) =
    if nextAssertions = [] then (concepts, roles)
    else
        let newAssertions = List.collect (fun assertion -> expandAssertion roles concepts assertion) nextAssertions
        expand concepts roles newAssertions
    
let init_expander (Abox : ABoxAssertion list) =
    let individual_concept_map = Dictionary<IriReference, Concept list>()
    let individual_role_map = Dictionary<IriReference, (IriReference * Role) list>()
    for assertion in Abox do
        match assertion with
        | ConceptAssertion (individual, concept) -> add_element_to_dict_list individual_concept_map individual [concept]
        | RoleAssertion (left, right, role) -> add_element_to_dict_list individual_role_map left [(right, role)]
    (individual_concept_map, individual_role_map)
    
let reasoner (Kb : ALC.knowledgeBase) =
    let (concepts, roles) = init_expander (snd Kb)
    let expanded = expand concepts roles (snd Kb)
    expanded