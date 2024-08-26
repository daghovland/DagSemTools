module AlcTableau.ConjunctiveQueryAnswer

open AlcTableau.ALC
open AlcTableau.ConjunctiveQuery
open ConjunctiveQuery
open IriTools
open System.Collections.Generic
open MapUtils

type AnswerMap = {
    IndividualMap : Map<string, IriReference>
    RoleMap : Map<string, IriReference>
    ConceptMap : Map<string, Concept>
    }

let add_assertion (state : QueryingService.QueryingCache) (assertion : ALC.ABoxAssertion) =
    match Tableau.expand state.reasoner_state [Tableau.NewAssertions.Known [assertion]]
    with
    | Tableau.InConsistent _ -> failwith "Inconsistent"
    | Tableau.Consistent new_reasoner_state ->
        match assertion with
        | ALC.ConceptAssertion (individual, concept) -> 
            { state with reasoner_state = new_reasoner_state
                         known_concept_individuals = addToMapList state.known_concept_individuals concept [individual]
            }
        | ALC.RoleAssertion (left, right, role) -> 
            { state with reasoner_state = new_reasoner_state
                         known_object_predicate_index = addToMapList state.known_object_predicate_index (right, role) [left] }
        | NegativeAssertion aBoxAssertion -> failwith "todo"
        | NegativeRoleAssertion(individual, right, assertedRole) -> failwith "todo"
        | LiteralAssertion(individual, property, value) -> failwith "todo"
        | LiteralAnnotationAssertion(individual, property, value) -> failwith "todo"
        | ObjectAnnotationAssertion(individual, property, value) -> failwith "todo"

let rec answer (state : QueryingService.QueryingCache)  (query : ConjunctiveQuery.ConjunctiveQuery) (answer_map : AnswerMap) =
    match query with
    | [] -> [answer_map]
    | atom :: rest_query -> match atom with
                                    | RoleQuery (RoleSignature.Role role, IndividualSignature.Individual left, IndividualSignature.Variable right) ->
                                            if answer_map.IndividualMap.ContainsKey(right) then
                                                if ReasonerService.check_assertion state.reasoner_state (ALC.RoleAssertion (left, answer_map.IndividualMap.[right], role)) then
                                                    let updated_state = (add_assertion state (ABoxAssertion.RoleAssertion (left, answer_map.IndividualMap.[right], role)))
                                                    answer updated_state rest_query answer_map
                                                else []
                                            else
                                                let known_individuals = state.known_subject_predicate_index.GetValueOrDefault((left, role), [])
                                                let known_answers = known_individuals
                                                                    |> List.collect (fun individual ->
                                                                        let updated_state = (add_assertion state (ABoxAssertion.RoleAssertion (left, individual, role)))
                                                                        let updated_map = { answer_map with IndividualMap = answer_map.IndividualMap.Add(right, individual) }
                                                                        answer updated_state rest_query updated_map )
                                                let inferred_individuals = (state.probable_subject_predicate_index.GetValueOrDefault((left, role), [])
                                                                         |> List.filter (fun i -> not (known_individuals |> List.contains i))
                                                                         |> List.filter (fun i -> ReasonerService.check_assertion state.reasoner_state (ALC.RoleAssertion (left, i, role)))
                                                                         |> List.collect (fun individual ->
                                                                                        let updated_state = (add_assertion state (ABoxAssertion.RoleAssertion (left, individual, role)))
                                                                                        let updated_map = { answer_map with IndividualMap = answer_map.IndividualMap.Add(right, individual) }
                                                                                        answer updated_state rest_query answer_map ))
                                                known_answers @ inferred_individuals
                                    | RoleQuery (RoleSignature.Role role, IndividualSignature.Variable left, IndividualSignature.Individual right) ->
                                            if answer_map.IndividualMap.ContainsKey(left) then
                                                if ReasonerService.check_assertion state.reasoner_state (ALC.RoleAssertion (answer_map.IndividualMap.[left], right, role)) then
                                                    let updated_state = (add_assertion state (ABoxAssertion.RoleAssertion (answer_map.IndividualMap.[left], right, role)))
                                                    answer updated_state rest_query answer_map
                                                else []
                                            else
                                                let known_individuals = state.known_object_predicate_index.GetValueOrDefault((right, role), [])
                                                let known_answers = known_individuals
                                                                    |> List.collect (fun individual ->
                                                                        let updated_state = (add_assertion state (ABoxAssertion.RoleAssertion (individual, right, role)))
                                                                        let updated_map = { answer_map with IndividualMap = answer_map.IndividualMap.Add(left, individual) }
                                                                        answer updated_state rest_query updated_map )
                                                let inferred_individuals = (state.probable_subject_predicate_index.GetValueOrDefault((right, role), [])
                                                                         |> List.filter (fun i -> not (known_individuals |> List.contains i))
                                                                         |> List.filter (fun i -> ReasonerService.check_assertion state.reasoner_state (ALC.RoleAssertion (i, right, role)))
                                                                         |> List.collect (fun individual ->
                                                                                        let updated_state = (add_assertion state (ABoxAssertion.RoleAssertion (individual, right, role)))
                                                                                        let updated_map = { answer_map with IndividualMap = answer_map.IndividualMap.Add(left, individual) }
                                                                                        answer updated_state rest_query answer_map ))
                                                known_answers @ inferred_individuals
                                    | ConceptQuery (ConceptSignature.Concept concept, IndividualSignature.Variable v) ->
                                        if answer_map.IndividualMap.ContainsKey(v) then
                                                if ReasonerService.check_assertion state.reasoner_state (ALC.ConceptAssertion(answer_map.IndividualMap.[v], concept)) then
                                                    let updated_state = (add_assertion state (ABoxAssertion.ConceptAssertion (answer_map.IndividualMap.[v], concept)))
                                                    answer updated_state rest_query answer_map
                                                else []
                                            else
                                                QueryingService.query_concept_individual state concept |> List.collect (fun individual ->
                                                                            let updated_state = (add_assertion state (ABoxAssertion.ConceptAssertion (individual, concept)))
                                                                            let updated_map = { answer_map with IndividualMap = answer_map.IndividualMap.Add(v, individual) }
                                                                            answer updated_state rest_query updated_map )
                                    | ConceptQuery (ConceptSignature.Concept concept, IndividualSignature.Individual i) ->
                                        if ReasonerService.check_assertion state.reasoner_state (ALC.ConceptAssertion(i, concept))
                                        then answer state rest_query answer_map
                                        else []
                                     | RoleQuery (RoleSignature.Role role, IndividualSignature.Individual left, IndividualSignature.Individual right) ->
                                        if ReasonerService.check_assertion state.reasoner_state (ALC.RoleAssertion(left, right, role))
                                        then answer state rest_query answer_map
                                        else []
                                    | _ -> failwith "Not implemented"

let answer_query (state : QueryingService.QueryingCache) (query : ConjunctiveQuery) =
    answer state query { IndividualMap = Map.empty; RoleMap = Map.empty; ConceptMap = Map.empty }