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
        
    let ApplySubstitutionResource (sub : Substitution) (res : ResourceOrVariable) : ResourceId =
        match res with
        | ResourceOrVariable.Resource r -> r
        | Variable v -> sub[v]
    let ApplySubstitutionTriple sub (triple : TriplePattern) : Triple =
        {Rdf.RDFStore.subject = ApplySubstitutionResource sub triple.Subject
         RDFStore.predicate = ApplySubstitutionResource sub triple.Predicate
         RDFStore.object = ApplySubstitutionResource sub triple.Object 
         }
    type PartialRule = 
        {Rule: Rule; Match : TriplePattern}
    type PartialRuleMatch = 
        {Match: PartialRule; Substitution: Substitution}
    
    
    let GetSubstitution (resource : RDFStore.ResourceId, variable : ResourceOrVariable) (subs : Substitution)  : Substitution option =
        match variable, resource with
        | Variable v, _  ->
            match subs.TryGetValue v with
            | true, r when r = resource -> Some Map.empty
            | true, _ -> None
            | false, _ -> Some (Map.ofList [(v, resource)])
        | ResourceOrVariable.Resource r, s when r = s -> Some Map.empty
        | _ -> None
    
    let GetSubstitutionOption (subs : Substitution option) (resource : RDFStore.ResourceId, variable : ResourceOrVariable) : Substitution option =
        Option.bind (GetSubstitution (resource, variable)) subs    
    let GetSubstitutions (fact : Triple) (factPattern : TriplePattern)  : Substitution option =
        let resourceList = [(fact.subject, factPattern.Subject); (fact.predicate, factPattern.Predicate); (fact.object, factPattern.Object)]
        resourceList |> List.fold GetSubstitutionOption (Some Map.empty)
        
    (*
        For a given triple/fact and a rule, return 
        all matches (PartialRuleMatch) such that the fact is an instance of the match in the rule.
    *)
    let GetMatchesForRule fact rule =
        rule.Rule.Body
        |> List.map (fun r -> r, GetSubstitutions fact r) 
        |> List.choose (fun (r, s) -> Option.map (fun s -> {Match = rule; Substitution = s}) s)
        
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

    let GetMappedResource (sub : Substitution) (resource : ResourceOrVariable ) : ResourceOrVariable  =
              match resource with
              | ResourceOrVariable.Resource _ -> resource
              | Variable v -> match sub.TryGetValue v with
                              | true, r -> ResourceOrVariable.Resource r
                              | false, _ -> Variable v

    let evaluatePattern (rdf : TripleTable) (triplePattern : TriplePattern) (sub : Substitution)  =
        let mappedTriple : TriplePattern = {
                            TriplePattern.Subject = GetMappedResource sub triplePattern.Subject
                            TriplePattern.Predicate = GetMappedResource sub triplePattern.Predicate
                            TriplePattern.Object = GetMappedResource sub triplePattern.Object
                            }
        let matchedTriples = (
            match mappedTriple.Subject, mappedTriple.Predicate, mappedTriple.Object with
            | ResourceOrVariable.Resource s, Variable p, Variable o -> 
                    rdf.GetTriplesWithSubject(s)
            | Variable s, ResourceOrVariable.Resource p, Variable o -> 
                    rdf.GetTriplesWithObject(p)
            | Variable s, Variable p, ResourceOrVariable.Resource o -> 
                    rdf.GetTriplesWithSubject(o)
            | ResourceOrVariable.Resource s, ResourceOrVariable.Resource p, Variable o -> 
                    rdf.GetTriplesWithSubjectPredicate(s, p)
            | Variable s, ResourceOrVariable.Resource p, ResourceOrVariable.Resource o -> 
                    rdf.GetTriplesWithObjectPredicate(o, p)
            | ResourceOrVariable.Resource s, ResourceOrVariable.Resource p, ResourceOrVariable.Resource o -> 
                    match rdf.ThreeKeysIndex.TryGetValue {subject = s; predicate = p; object = o} with
                    | false,_ -> []
                    | true, v -> [rdf.GetTripleListEntry v]                    
            | ResourceOrVariable.Resource s, Variable p, ResourceOrVariable.Resource o ->
                rdf.GetTriplesWithSubjectObject (s, o)
            | Variable s, Variable p, Variable o -> rdf.TripleList
            ) 
        matchedTriples |> Seq.choose (fun t -> GetSubstitutions t mappedTriple)
                            
    let evaluate (rdf : TripleTable) (ruleMatch : PartialRuleMatch) (fact : Triple) : Substitution seq =
         ruleMatch.Match.Rule.Body
        |> List.fold ( fun subs tr -> subs |> Seq.collect (evaluatePattern rdf tr) ) [ruleMatch.Substitution]  
    
    type DatalogProgram (Rules: Rule list, tripleStore : Rdf.TripleTable) =
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
                |> List.collect (List.collect (GetMatchesForRule fact))
                
        member this.materialise() =
            for triple in tripleStore.TripleList do
                for rules in this.GetRulesForFact triple do
                    for subs in evaluate tripleStore rules triple do
                        let newTriple = ApplySubstitutionTriple subs rules.Match.Rule.Head
                        tripleStore.AddTriple newTriple