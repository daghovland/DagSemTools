(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.Rdf

open System.Numerics
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
            | Query.QueryComponent.Filter _expr ->
                // TODO: implement FILTER evaluation
                GetBindingsForGraphGroup datastore rest currentBindings
            | Query.QueryComponent.Union groups ->
                let unionResults =
                    groups
                    |> List.collect (fun group -> GetBindingsForGraphGroup datastore group currentBindings)
                    |> List.distinct
                GetBindingsForGraphGroup datastore rest unionResults
            | Query.QueryComponent.Minus minusPattern ->
                let minusResults = GetBindingsForGraphGroup datastore minusPattern [Map.empty]
                let filtered =
                    currentBindings
                    |> List.filter (fun binding ->
                        minusResults
                        |> List.forall (fun minusBinding ->
                            // Keep if no compatible binding exists in minus set
                            not (minusBinding |> Map.forall (fun k v -> binding.TryFind k = Some v))))
                GetBindingsForGraphGroup datastore rest filtered
            | Query.QueryComponent.Values (vars, rows) ->
                let newBindings =
                    currentBindings
                    |> List.collect (fun binding ->
                        rows
                        |> List.choose (fun row ->
                            let resolvedRow =
                                List.zip vars (row |> List.ofSeq)
                                |> List.choose (fun (v, t) ->
                                    match t with
                                    | Term.Resource id -> Some (v, id)
                                    | Term.Variable _ -> None) // UNDEF - skip
                                |> Map.ofList
                            let compatible =
                                resolvedRow
                                |> Map.forall (fun k v ->
                                    match binding.TryFind k with
                                    | Some existing -> existing = v
                                    | None -> true)
                            if compatible then
                                Some (Map.fold (fun acc k v -> Map.add k v acc) binding resolvedRow)
                            else None))
                GetBindingsForGraphGroup datastore rest newBindings
            | Query.QueryComponent.Bind (expr, varName) ->
                // TODO: implement expression evaluation; for now skip
                GetBindingsForGraphGroup datastore rest currentBindings
            | Query.QueryComponent.Subquery subSelect ->
                let subResults = Answer datastore subSelect
                let newBindings =
                    currentBindings
                    |> List.collect (fun binding ->
                        subResults
                        |> List.choose (fun subBinding ->
                            let compatible =
                                subBinding
                                |> Map.forall (fun k v ->
                                    match binding.TryFind k with
                                    | Some existing -> existing = v
                                    | None -> true)
                            if compatible then
                                Some (Map.fold (fun acc k v -> Map.add k v acc) binding subBinding)
                            else None))
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


    and public Answer (datastore : Datastore) (query : Query.SelectQuery) : Map<string, GraphElementId> list =
        let results = GetBindingsForGraphGroup datastore query.Query [Map.empty]
        
        let evalExpr (binding: Map<string, GraphElementId>) (expr: Expression) : GraphElementId option =
            match expr with
            | ExprVariable v -> binding.TryFind v
            | ExprAggregate _ -> None // Aggregates handled after grouping
            | ExprTerm (Term.Resource id) -> Some id
            | ExprTerm (Term.Variable v) -> binding.TryFind v
            | ExprBinaryOp _ -> None // TODO: implement
            | ExprUnaryOp _ -> None  // TODO: implement
            | ExprBuiltInCall _ -> None // TODO: implement

        let resultsAfterGrouping =
            if query.GroupBy.IsEmpty then
                let hasAggregates = 
                    query.Projection 
                    |> List.exists (function ProjectionElement.ProjectExpression(ExprAggregate _, _) -> true | _ -> false)
                if hasAggregates then
                    if results.IsEmpty then [[]] // One empty group for aggregates over empty set
                    else [results]
                else
                    results |> List.map (fun r -> [r])
            else
                results
                |> List.groupBy (fun binding ->
                    query.GroupBy 
                    |> List.map (fun expr -> evalExpr binding expr)
                )
                |> List.map snd

        let evalAggregate (group: Map<string, GraphElementId> list) (agg: Aggregate) : GraphElementId option =
            match agg with
            | Sum(distinct, Term.Variable v) ->
                let values = 
                    let allValues = 
                        group 
                        |> List.choose (fun b -> b.TryFind v)
                    if distinct then
                        allValues |> List.distinct
                    else
                        allValues
                    |> List.choose (fun id -> 
                        let gel = datastore.Resources.GetGraphElement id
                        DagSemTools.Rdf.Ingress.tryGetNonNegativeIntegerLiteral gel)
                if values.IsEmpty then 
                    Some (datastore.Resources.AddLiteralResource (DagSemTools.Ingress.RdfLiteral.IntegerLiteral (BigInteger 0)))
                else
                    let sum = values |> List.reduce (+)
                    Some (datastore.Resources.AddLiteralResource (DagSemTools.Ingress.RdfLiteral.IntegerLiteral sum))
            | Count(distinct, termOpt) ->
                let count = 
                    match termOpt with
                    | None -> // COUNT(*)
                        group.Length
                    | Some (Term.Variable v) ->
                        let values = 
                            group 
                            |> List.choose (fun b -> b.TryFind v)
                        if distinct then
                            (values |> List.distinct).Length
                        else
                            values.Length
                    | Some (Term.Resource r) ->
                        if distinct then 1 else group.Length
                Some (datastore.Resources.AddLiteralResource (DagSemTools.Ingress.RdfLiteral.IntegerLiteral (BigInteger count)))
            | Min(distinct, Term.Variable v) ->
                let values = 
                    group 
                    |> List.choose (fun b -> b.TryFind v)
                    |> List.choose (fun id -> 
                        let gel = datastore.Resources.GetGraphElement id
                        DagSemTools.Rdf.Ingress.tryGetNonNegativeIntegerLiteral gel)
                if values.IsEmpty then None
                else
                    let min = values |> List.min
                    Some (datastore.Resources.AddLiteralResource (DagSemTools.Ingress.RdfLiteral.IntegerLiteral min))
            | Max(distinct, Term.Variable v) ->
                let values = 
                    group 
                    |> List.choose (fun b -> b.TryFind v)
                    |> List.choose (fun id -> 
                        let gel = datastore.Resources.GetGraphElement id
                        DagSemTools.Rdf.Ingress.tryGetNonNegativeIntegerLiteral gel)
                if values.IsEmpty then None
                else
                    let max = values |> List.max
                    Some (datastore.Resources.AddLiteralResource (DagSemTools.Ingress.RdfLiteral.IntegerLiteral max))
            | Avg(distinct, Term.Variable v) ->
                let values = 
                    let allValues = 
                        group 
                        |> List.choose (fun b -> b.TryFind v)
                    if distinct then
                        allValues |> List.distinct
                    else
                        allValues
                    |> List.choose (fun id -> 
                        let gel = datastore.Resources.GetGraphElement id
                        DagSemTools.Rdf.Ingress.tryGetNonNegativeIntegerLiteral gel)
                if values.IsEmpty then None
                else
                    let sum = values |> List.reduce (+)
                    let avg = sum / (BigInteger values.Length)
                    Some (datastore.Resources.AddLiteralResource (DagSemTools.Ingress.RdfLiteral.IntegerLiteral avg))
            | _ -> None

        resultsAfterGrouping
        |> List.map (fun group ->
            let firstBinding = group.Head
            query.Projection
            |> List.fold (fun acc element ->
                match element with
                | ProjectionElement.ProjectVariable var ->
                    match firstBinding.TryFind var with
                    | Some value -> Map.add var value acc
                    | None -> acc
                | ProjectionElement.ProjectExpression (expr, alias) ->
                    match expr with
                    | ExprAggregate agg ->
                        match evalAggregate group agg with
                        | Some value -> Map.add alias value acc
                        | None -> acc
                    | _ ->
                        match evalExpr firstBinding expr with
                        | Some value -> Map.add alias value acc
                        | None -> acc
            ) Map.empty
        )
        