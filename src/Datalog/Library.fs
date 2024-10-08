(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Datalog

open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress

module Datalog =

    [<StructuralComparison>]
    [<StructuralEquality>]
    type ResourceOrVariable = 
        | Resource of Ingress.ResourceId
        | Variable of string

    let ResourceOrVariableToString (tripleTable : Datastore) (res : ResourceOrVariable) : string =
        match res with
        | Resource r -> (tripleTable.GetResource r).ToString()
        | Variable v -> $"?{v}"
    
    [<StructuralComparison>]
    [<StructuralEquality>]
    type ResourceOrWildcard = 
        | Resource of Ingress.ResourceId
        | Wildcard
    
    
    [<StructuralComparison>]
    [<StructuralEquality>]
    type TriplePattern = 
        {Subject: ResourceOrVariable; Predicate: ResourceOrVariable; Object: ResourceOrVariable}

    let TriplePatternToString (tripleTable : Datastore) (triplePatter : TriplePattern) : string =
        $"[{ResourceOrVariableToString tripleTable triplePatter.Subject}, {ResourceOrVariableToString tripleTable triplePatter.Predicate}, {ResourceOrVariableToString tripleTable triplePatter.Object} ]"
    
    [<StructuralComparison>]
    [<StructuralEquality>]
    type RuleAtom = 
        | PositiveTriple of TriplePattern
        | NotTriple of TriplePattern
    
    let ResourceAtom (tripleTable : Datastore) (ruleAtom : RuleAtom) : string =
        match ruleAtom with
        | PositiveTriple t -> TriplePatternToString tripleTable t
        | NotTriple t -> $"not {TriplePatternToString tripleTable t}"
        
    [<StructuralComparison>]
    [<StructuralEquality>]
    type TripleWildcard = 
        {Subject: ResourceOrWildcard; Predicate: ResourceOrWildcard; Object: ResourceOrWildcard}

    let ConstantTriplePattern (triple : Ingress.Triple) : TriplePattern = 
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
        {Head: TriplePattern; Body: RuleAtom list}
        
    let RuleToString (tripleTable : Datastore) (rule : Rule) : string =
        let mutable headString = rule.Head |> TriplePatternToString tripleTable
        headString <- headString + " :- "
        rule.Body |> List.iter (fun atom -> headString <- headString + ResourceAtom tripleTable atom + ", ")
        headString
        
    (* Safe rules are those where the head only has variable that are in the body *)
    let isSafeRule (rule) : bool =
        let variablesInBody = rule.Body
                                |> Seq.collect (fun atom -> match atom with
                                                            | PositiveTriple t -> [t.Subject; t.Predicate; t.Object]
                                                            | NotTriple t -> [t.Subject; t.Predicate; t.Object]
                                )
                                |> Seq.choose (fun r -> match r with
                                                        | Variable v -> Some (Variable v)
                                                        | _ -> None
                                )
        let variablesInHead = [rule.Head.Subject; rule.Head.Predicate; rule.Head.Object]
        variablesInHead |> Seq.forall (fun v -> variablesInBody |> Seq.exists (fun b -> b = v))
        
    type Substitution = 
        Map<string, Ingress.ResourceId>
        
    let ApplySubstitutionResource (sub : Substitution) (res : ResourceOrVariable) : ResourceId =
        match res with
        | ResourceOrVariable.Resource r -> r
        | Variable v -> sub[v]
    let ApplySubstitutionTriple sub (triple : TriplePattern) : Triple =
        {
         Ingress.subject = ApplySubstitutionResource sub triple.Subject
         Ingress.predicate = ApplySubstitutionResource sub triple.Predicate
         Ingress.object = ApplySubstitutionResource sub triple.Object 
        }
    type PartialRule = 
        {Rule: Rule; Match : TriplePattern}
    type PartialRuleMatch = 
        {Match: PartialRule; Substitution: Substitution}
    
    
    let GetSubstitution (resource : Ingress.ResourceId, variable : ResourceOrVariable) (subs : Substitution)  : Substitution option =
        match variable, resource with
        | Variable v, _  ->
            match subs.TryGetValue v with
            | true, r when r = resource -> Some subs
            | true, _ -> None
            | false, _ -> Some (subs.Add (v, resource))
        | ResourceOrVariable.Resource r, s when r = s -> Some subs
        | _ -> None
    
    let GetSubstitutionOption (subs : Substitution option) (resource : Ingress.ResourceId, variable : ResourceOrVariable) : Substitution option =
        Option.bind (GetSubstitution (resource, variable)) subs    
    let GetSubstitutions (subs) (fact : Triple) (factPattern : TriplePattern)  : Substitution option =
        let resourceList = [
                            (fact.subject, factPattern.Subject)
                            (fact.predicate, factPattern.Predicate)
                            (fact.object, factPattern.Object)
                            ]
        resourceList |> Seq.fold GetSubstitutionOption (Some subs)
        
    (*
        For a given triple/fact and a rule, return 
        all matches (PartialRuleMatch) such that the fact is an instance of the match in the rule.
    *)
    let GetMatchesForRule fact rule =
        rule.Rule.Body
        |> Seq.choose (fun r -> match r with
                                | PositiveTriple t -> Some t
                                | NotTriple t -> None
                    )
        |> Seq.map (fun r -> r, GetSubstitutions (Map.empty) fact r) 
        |> Seq.choose (fun (r, s) -> Option.map (fun s -> {Match = rule; Substitution = s}) s)
        
    let GetPartialMatch (triple : TriplePattern)  =
        WildcardTriplePattern triple
        
    let GetPartialMatches (rule : Rule) : Map<TripleWildcard, PartialRule list> =
       Map.ofSeq (rule.Body
       |> Seq.choose (fun atom -> match atom with
                                    | PositiveTriple t -> Some t
                                    | NotTriple t -> None
                    )
       |> Seq.collect (fun pat ->
           WildcardTriplePattern pat
            |> Seq.map  (fun t -> (t, [{Rule = rule; Match = pat}]))
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
            | Variable s, Variable p, Variable o -> rdf.GetTriples()
            ) 
        matchedTriples |> Seq.choose (fun t -> GetSubstitutions sub t mappedTriple)
    
    
                            
    let evaluatePositive (rdf : TripleTable) (ruleMatch : PartialRuleMatch) : Substitution seq =
         ruleMatch.Match.Rule.Body
        |> Seq.choose (fun atom -> match atom with
                                    | PositiveTriple t -> Some t
                                    | NotTriple t -> None
                    )
        |> Seq.fold
            ( fun subs tr ->
                    subs |> Seq.collect (evaluatePattern rdf tr) )
            [ruleMatch.Substitution]  
    
    let evaluate (rdf : TripleTable) (ruleMatch : PartialRuleMatch)  : Substitution seq =
        ruleMatch.Match.Rule.Body
        |> Seq.choose (fun atom -> match atom with
                                    | PositiveTriple _ -> None
                                    | NotTriple t -> Some t
                    )
        |> Seq.fold
            ( fun subs tr ->
                    subs |> Seq.choose
                                (fun sub -> if Seq.isEmpty (evaluatePattern rdf tr sub)
                                            then
                                                Some sub
                                            else None
                                )
            )
            (evaluatePositive rdf ruleMatch)
    
