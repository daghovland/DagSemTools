(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace AlcTableau

open AlcTableau.Rdf
open AlcTableau.Rdf.RDFStore

module Datalog =

    [<StructuralComparison>]
    [<StructuralEquality>]
    type ResourceOrVariable = 
        | Resource of RDFStore.ResourceId
        | Variable of string

    
    [<StructuralComparison>]
    [<StructuralEquality>]
    type ResourceOrWildcard = 
        | Resource of RDFStore.ResourceId
        | Wildcard
    
    [<StructuralComparison>]
    [<StructuralEquality>]
    type TriplePattern = 
        {Subject: ResourceOrVariable; Predicate: ResourceOrVariable; Object: ResourceOrVariable}

    [<StructuralComparison>]
    [<StructuralEquality>]
    type TripleWildcard = 
        {Subject: ResourceOrWildcard; Predicate: ResourceOrWildcard; Object: ResourceOrWildcard}

    let ConstantTriplePattern (triple : RDFStore.Triple) : TriplePattern = 
        {Subject = ResourceOrVariable.Resource triple.subject; Predicate = ResourceOrVariable.Resource triple.predicate; Object = ResourceOrVariable.Resource triple.object}
    
    /// Generate all 8 possible triple patterns with wildcards for a given triple pattern
    /// Duplicate patterns are ok since these are used as a key in a dictionary
    let WildcardTriplePattern (triple : TriplePattern) : TripleWildcard list = 
        let resourceList = [triple.Subject; triple.Predicate; triple.Object]
        let rec generatePatterns (triple: ResourceOrVariable list) : ResourceOrWildcard list list = 
              match triple with
              | [] -> [[]]
              | head :: tail -> 
                let rest = generatePatterns tail
                match head with
                | Variable _ -> 
                     rest |> List.map (fun triplePart -> Wildcard :: triplePart)
                | ResourceOrVariable.Resource r -> 
                    rest |> List.collect (fun triplePart -> [Resource r :: triplePart; Wildcard :: triplePart])
        generatePatterns resourceList |> List.map (fun triplePart ->
            {Subject = List.item 0 triplePart; Predicate = List.item 1 triplePart; Object = List.item 2 triplePart})
        
    [<StructuralComparison>]
    [<StructuralEquality>]
    type Rule = 
        {Head: TriplePattern; Body: TriplePattern list}
    type Substitution = 
        Map<string, RDFStore.ResourceId>
    type PartialRule = 
        {Rule: Rule; Match : TriplePattern}
    type PartialRuleMatch = 
        {Match: PartialRule; Substitution: Substitution}
    
    
    let GetSubstitution (subs : Substitution) (resource : RDFStore.ResourceId, variable : ResourceOrVariable) : Substitution option =
        match variable, resource with
        | Variable v, _  ->
            match subs.TryGetValue v with
            | true, r when r = resource -> Some Map.empty
            | true, _ -> None
            | false, _ -> Some (Map.ofList [(v, resource)])
        | ResourceOrVariable.Resource r, s when r = s -> Some Map.empty
        | _ -> None
    
    let GetSubstitutionOption (subs : Substitution option) (resource : RDFStore.ResourceId, variable : ResourceOrVariable) : Substitution option =
        Option.bind (fun s -> GetSubstitution s (resource, variable)) subs    
    let GetSubstitutions (fact : Triple) (rule : PartialRule)  : Substitution option =
        let factPattern = rule.Match
        let resourceList = [(fact.subject, factPattern.Subject); (fact.predicate, factPattern.Predicate); (fact.object, factPattern.Object)]
        resourceList |> List.fold GetSubstitutionOption (Some Map.empty)
        
    let GetPartialMatch (triple : TriplePattern)  =
        WildcardTriplePattern triple
        
    let GetPartialMatches (rule : Rule) : Map<TripleWildcard, PartialRule list> =
       Map.ofList (rule.Body
       |> List.collect (fun pat ->
           WildcardTriplePattern pat
            |> List.map  (fun t -> (t, [{Rule = rule; Match = pat}]))
            )
       )
    
    let mergeMaps (maps: Map<'Key, 'Value list> list) : Map<'Key, 'Value list> =
        maps |> List.fold
                    (Map.fold (fun acc key value ->
                    match acc.TryGetValue key with
                        | true, existing -> Map.add key (value @ existing) acc
                        | false, _ ->  Map.add key value acc)) Map.empty

    type DatalogProgram(Rules: Rule list) =
        let mutable Rules = Rules
        let mutable RuleMap : Map<TripleWildcard, PartialRule list>  =
                            Rules
                                |> List.map GetPartialMatches
                                |> mergeMaps
                                   
        member this.AddRule(rule: Rule)  =
            Rules <- rule :: Rules
            RuleMap <- mergeMaps [RuleMap; GetPartialMatches rule]
            
        member this.GetRulesForFact(fact: RDFStore.Triple) : PartialRuleMatch list = 
            ConstantTriplePattern fact
                |> WildcardTriplePattern
                |> List.map (fun wildcardFact ->
                    match RuleMap.TryGetValue(wildcardFact) with
                    | true, rules -> rules
                    | false, _ -> [])
                |> List.distinct
                |> List.collect (fun rules -> rules
                                            |> List.map (fun r -> (r, GetSubstitutions fact r))
                                            |> List.choose (fun (r, s) -> Option.map (fun s -> {Match = r; Substitution = s}) s)
                                            )


        