(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.Rdf

open Microsoft.FSharp.Collections
open DagSemTools.Rdf.Ingress
open DagSemTools.Rdf.Query

module QueryProcessor =
    
    let rec GetBindingsForGraphGroup
        (datastore : Datastore)
        (patterns: QueryComponent list)
        (currentBindings: Map<string, GraphElementId> list)
        : Map<string, GraphElementId> list =
        match patterns with
        | [] -> currentBindings
        | pattern :: rest ->
            match pattern with
            | Group groupPattern -> GetBindingsForGraphGroup datastore groupPattern currentBindings
            | Query.QueryComponent.Optional (Query.OptionalPattern.Optional optionalGroup) ->
                let (newBindings : Map<string, GraphElementId> list) =
                    currentBindings
                    |> List.collect (fun binding ->
                        let optionalMatches = GetBindingsForGraphGroup datastore optionalGroup [binding]
                        if List.isEmpty optionalMatches then
                            [binding]
                        else
                            optionalMatches)
                GetBindingsForGraphGroup datastore rest newBindings
            | Query.QueryComponent.Pattern pattern -> 
                let newBindings =
                    currentBindings
                    |> List.collect (fun binding ->
                        let boundPattern =
                            { Graph = 
                                  match pattern.Graph with
                                  | Term.Variable vName when binding.ContainsKey vName -> Term.Resource binding.[vName]
                                  | _ -> pattern.Graph
                              Subject = 
                                match pattern.Subject with
                                | Term.Variable vName when binding.ContainsKey vName -> Term.Resource binding.[vName]
                                | _ -> pattern.Subject
                              Predicate = 
                                match pattern.Predicate with
                                | Term.Variable vName when binding.ContainsKey vName -> Term.Resource binding.[vName]
                                | _ -> pattern.Predicate
                              Object = 
                                match pattern.Object with
                                | Term.Variable vName when binding.ContainsKey vName -> Term.Resource binding.[vName]
                                | _ -> pattern.Object }
                        datastore.GetQuads(boundPattern)
                        |> Seq.map (fun triple ->
                            let newBindingPairs =
                                [ match pattern.Graph with
                                  | Term.Variable vName when not (binding.ContainsKey vName) -> yield (vName, triple.tripleId)
                                  | _ -> ()
                                  match pattern.Subject with
                                  | Term.Variable vName when not (binding.ContainsKey vName) -> yield (vName, triple.subject)
                                  | _ -> ()
                                  match pattern.Predicate with
                                  | Term.Variable vName when not (binding.ContainsKey vName) -> yield (vName, triple.predicate)
                                  | _ -> ()
                                  match pattern.Object with
                                  | Term.Variable vName when not (binding.ContainsKey vName) -> yield (vName, triple.obj)
                                  | _ -> () ]
                            List.fold (fun acc (k, v) -> Map.add k v acc) binding newBindingPairs)
                        |> Seq.toList)

                GetBindingsForGraphGroup datastore rest newBindings
        
    
    let RemoveNonProjectedBindings (projectedVars: ProjectionElement list) (binding: Map<string, GraphElementId>) : Map<string, GraphElementId> =
            projectedVars
            |> List.fold (fun acc element ->
                match element with
                | ProjectionElement.ProjectVariable var ->
                    match binding.TryFind var with
                    | Some value -> Map.add var value acc
                    | None -> acc
                | ProjectionElement.ProjectExpression (_, alias) ->
                    // Aggregates are not yet supported in the Answer processor
                    match binding.TryFind alias with
                    | Some value -> Map.add alias value acc
                    | None -> acc
                ) Map.empty
    let public Answer (datastore : Datastore) (query : Query.SelectQuery) : Map<string, GraphElementId> list =
        let results = GetBindingsForGraphGroup datastore query.Query [Map.empty]
        results
        |> List.map (RemoveNonProjectedBindings query.Projection)
        