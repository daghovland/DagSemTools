(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.Rdf

open DagSemTools.Rdf.Ingress
open DagSemTools.Rdf.Query

module QueryProcessor =
    
    let GetBindingsForBGP(datastore: Datastore) (bgp: TriplePattern list) : Map<string, GraphElementId> list =
        let rec aux (patterns: TriplePattern list) (currentBindings: Map<string, GraphElementId> list) : Map<string, GraphElementId> list =
            match patterns with
            | [] -> currentBindings
            | pattern :: rest ->
                let newBindings =
                    currentBindings
                    |> List.collect (fun binding ->
                        let boundPattern =
                            { Subject = 
                                match pattern.Subject with
                                | Variable vName when binding.ContainsKey vName -> Resource binding.[vName]
                                | _ -> pattern.Subject
                              Predicate = 
                                match pattern.Predicate with
                                | Variable vName when binding.ContainsKey vName -> Resource binding.[vName]
                                | _ -> pattern.Predicate
                              Object = 
                                match pattern.Object with
                                | Variable vName when binding.ContainsKey vName -> Resource binding.[vName]
                                | _ -> pattern.Object }
                        datastore.GetTriples(boundPattern)
                        |> Seq.map (fun triple ->
                            let newBinding =
                                [ match pattern.Subject with
                                  | Variable vName when not (binding.ContainsKey vName) -> yield (vName, triple.subject)
                                  | _ -> ()
                                  match pattern.Predicate with
                                  | Variable vName when not (binding.ContainsKey vName) -> yield (vName, triple.predicate)
                                  | _ -> ()
                                  match pattern.Object with
                                  | Variable vName when not (binding.ContainsKey vName) -> yield (vName, triple.obj)
                                  | _ -> () ]
                                |> Map.ofList
                            Map.fold (fun acc k v -> Map.add k v acc) binding newBinding)
                        |> Seq.toList)
                aux rest newBindings
        aux bgp [Map.empty]
    
    let RemoveNonProjectedBindings (projectedVars: string list) (binding: Map<string, GraphElementId>) : Map<string, GraphElementId> =
            projectedVars
            |> List.fold (fun acc var ->
                match binding.TryFind var with
                | Some value -> Map.add var value acc
                | None -> acc) Map.empty
    let public Answer (datastore : Datastore) (query : Query.SelectQuery) : Map<string, GraphElementId> list =
        GetBindingsForBGP datastore query.BGPs
        |> List.map (RemoveNonProjectedBindings query.Projection)
        

