(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Datalog

open System
open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.Rdf


[<StructuralComparison>]
[<StructuralEquality>]
type Term = 
    | Resource of GraphElementId
    | Variable of string
    member this.ToString (manager : GraphElementManager) : string=
                match this with
                | Resource res -> (manager.GetGraphElement res).ToString()
                | Variable vName -> vName
    member internal this.GetHashCodeForUnification : int =
        match this with
        | Resource res -> res.GetHashCode()
        | Variable vName -> 100
        
[<StructuralComparison>]
[<StructuralEquality>]
type ResourceOrWildcard = 
    | Resource of GraphElementId
    | Wildcard

[<StructuralComparison>]
[<StructuralEquality>]
type TriplePattern =
    {Subject: Term; Predicate: Term; Object: Term}
    member private this.GeneralToString(termToStringFunction : Term -> string ) : string  =
        $"[{termToStringFunction this.Subject}, {termToStringFunction this.Predicate}, {termToStringFunction this.Object}]"
    override this.ToString() =
        this.GeneralToString(fun t -> t.ToString())
    member this.ToString(manager : GraphElementManager) =
        this.GeneralToString(fun (t : Term) ->t.ToString(manager))
    

[<StructuralComparison>]
[<StructuralEquality>]
type RuleHead =
    NormalHead of pattern: TriplePattern
    | Contradiction
    member this.GetVariables() =
        match this with
        | NormalHead triplePattern -> [triplePattern.Subject; triplePattern.Predicate; triplePattern.Object]
        | Contradiction -> []
    override this.ToString() =
        match this with
        | NormalHead tp -> tp.ToString()
        | Contradiction -> "FALSE"
    member this.ToString(manager) =
        match this with
        | NormalHead tp -> tp.ToString(manager)
        | Contradiction -> "FALSE"
        
[<StructuralComparison>]
[<StructuralEquality>]
type RuleAtom = 
    | PositiveTriple of TriplePattern
    | NotTriple of TriplePattern
    override this.ToString () =
        match this with
        | PositiveTriple tp -> tp.ToString()
        | NotTriple tp -> $"NOT {tp.ToString()}"
    member this.ToString (manager) =
        match this with
        | PositiveTriple tp -> tp.ToString(manager)
        | NotTriple tp -> $"NOT {tp.ToString(manager)}"


[<StructuralComparison>]
[<StructuralEquality>]
type TripleWildcard = 
    {Subject: ResourceOrWildcard; Predicate: ResourceOrWildcard; Object: ResourceOrWildcard}

[<StructuralComparison>]
[<StructuralEquality>]
type Rule = 
    {Head: RuleHead; Body: RuleAtom list}
    override this.ToString () =
        let bodyString = this.Body
                            |> List.map (fun el -> el.ToString())
                            |> String.concat ","
        $"{this.Head.ToString()} :- {bodyString}"
    member this.ToString (manager) =
        let bodyString = this.Body
                            |> List.map (fun el -> el.ToString(manager))
                            |> String.concat ","
        $"{this.Head.ToString(manager)} :- {bodyString} .\n"
type Substitution = 
    Map<string, Ingress.GraphElementId>
type PartialRule = 
    {Rule: Rule; Match : TriplePattern}
type PartialRuleMatch = 
    {Match: PartialRule; Substitution: Substitution}


module Datalog =
    let emptySubstitution : Substitution = Map.empty
    let isFact (rule) = rule.Body |> List.isEmpty
    let VariableToString (v : string) : string = $"?{v}"
    
    let ResourceToString (tripleTable: Datastore) r : string =
        (tripleTable.GetGraphElement r).ToString()
    
    let ResourceOrVariableToString (tripleTable : Datastore) res : string =
        match res with
        | Term.Resource r -> (tripleTable.GetGraphElement r).ToString()
        | Variable v -> VariableToString v
    

    let TriplePatternToString (tripleTable : Datastore) (triplePattern : TriplePattern) : string =
        $"[{ResourceOrVariableToString tripleTable triplePattern.Subject},
        {ResourceOrVariableToString tripleTable triplePattern.Predicate},
        {ResourceOrVariableToString tripleTable triplePattern.Object} ]"
    
    
    let ResourceAtom (tripleTable : Datastore) (ruleAtom : RuleAtom) : string =
        match ruleAtom with
        | PositiveTriple t -> TriplePatternToString tripleTable t
        | NotTriple t -> $"not {TriplePatternToString tripleTable t}"
        
    let ConstantTriplePattern (triple : Ingress.Triple) : TriplePattern = 
        {Subject = Term.Resource triple.subject; Predicate = Term.Resource triple.predicate; Object = Term.Resource triple.obj}
    
    /// Generate all 8 possible triple patterns with wildcards for a given triple pattern
    /// Duplicate patterns are ok since these are used as a key in a dictionary
    let WildcardTriplePattern (triple : TriplePattern) : TripleWildcard list = 
        let resourceList = [triple.Subject; triple.Predicate; triple.Object]
        let rec generatePatterns (triple: Term list) : ResourceOrWildcard list list = 
              match triple with
              | [] -> [[]]
              | head :: tail -> 
                let rest = generatePatterns tail
                match head with
                | Variable _ -> 
                     rest |> List.map (fun triplePart -> Wildcard :: triplePart)
                | Term.Resource r -> 
                    rest |> List.collect (fun triplePart -> [Resource r :: triplePart; Wildcard :: triplePart])
        generatePatterns resourceList |> List.map (fun triplePart ->
            {Subject = List.item 0 triplePart; Predicate = List.item 1 triplePart; Object = List.item 2 triplePart})
        
    let RuleHeadToString (tripleTable : Datastore) ruleHead : string =
        match ruleHead with
        | NormalHead triplePattern -> TriplePatternToString tripleTable triplePattern
        | Contradiction -> "false"
    let RuleToString (tripleTable : Datastore) (rule : Rule) : string =
        let mutable headString = rule.Head |> RuleHeadToString tripleTable
        headString <- headString + " :- "
        rule.Body |> List.iter (fun atom -> headString <- headString + ResourceAtom tripleTable atom + ", ")
        headString
        
    (* Safe rules are those where the head only has variable that are in the body *)
    let GetUnsafeHeadVariables (rule) =
        let variablesInBody = rule.Body
                                |> Seq.collect (fun atom -> match atom with
                                                            | PositiveTriple t -> [t.Subject; t.Predicate; t.Object]
                                                            | NotTriple t -> [t.Subject; t.Predicate; t.Object]
                                )
                                |> Seq.choose (fun r -> match r with
                                                        | Variable v -> Some (v)
                                                        | _ -> None
                                )
        let variablesInHead = rule.Head.GetVariables()
                                |> Seq.choose (fun r -> match r with
                                                        | Variable v -> Some (v)
                                                        | _ -> None
                                )
        variablesInHead
                    |> Seq.filter (fun v -> variablesInBody
                                                |> Seq.forall (fun b -> b <> v))
        
    let isSafeRule (rule) =
        let unsafeHeadVariables = GetUnsafeHeadVariables rule
        if unsafeHeadVariables |> Seq.isEmpty then
            true
        else
            let unsafeVarsString = String.concat ", " unsafeHeadVariables
            raise (new ArgumentException($"Unsafe variables {unsafeVarsString} in rule: {rule.ToString()}"))
        
    let ApplySubstitutionResource (sub : Substitution) (res : Term) : GraphElementId =
        match res with
        | Term.Resource r -> r
        | Variable v -> match sub.TryGetValue v with
                        | true, r -> r
                        | false, _ -> failwith "Head of rule not fully instantiated. Invalid datalog rule"
    let ApplySubstitutionTriple sub (triple : TriplePattern) : Triple =
        {
         Ingress.subject = ApplySubstitutionResource sub triple.Subject
         Ingress.predicate = ApplySubstitutionResource sub triple.Predicate
         Ingress.obj = ApplySubstitutionResource sub triple.Object 
        }
    
    
    let GetSubstitution (resource : Ingress.GraphElementId, variable : Term) (subs : Substitution)  : Substitution option =
        match variable, resource with
        | Variable v, _  ->
            match subs.TryGetValue v with
            | true, r when r = resource -> Some subs
            | true, _ -> None
            | false, _ -> Some (subs.Add (v, resource))
        | Term.Resource r, s when r = s -> Some subs
        | _ -> None
    
    let GetSubstitutionOption (subs : Substitution option) (resource, variable) : Substitution option =
        Option.bind (GetSubstitution (resource, variable)) subs    
    let GetSubstitutions (subs) (fact : Triple) (factPattern : TriplePattern)  : Substitution option =
        let resourceList = [
                            (fact.subject, factPattern.Subject)
                            (fact.predicate, factPattern.Predicate)
                            (fact.obj, factPattern.Object)
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

    let GetMappedResource (sub : Substitution) (resource : Term ) : Term  =
              match resource with
              | Term.Resource _ -> resource
              | Variable v -> match sub.TryGetValue v with
                              | true, r -> Term.Resource r
                              | false, _ -> Variable v

    let evaluatePattern (rdf : TripleTable) (triplePattern : TriplePattern) (sub : Substitution)  =
        let mappedTriple : TriplePattern = {
                            TriplePattern.Subject = GetMappedResource sub triplePattern.Subject
                            TriplePattern.Predicate = GetMappedResource sub triplePattern.Predicate
                            TriplePattern.Object = GetMappedResource sub triplePattern.Object
                            }
        let matchedTriples = (
            match mappedTriple.Subject, mappedTriple.Predicate, mappedTriple.Object with
            | Term.Resource s, Variable _p, Variable _o -> 
                    rdf.GetTriplesWithSubject(s)
            | Variable _s, Term.Resource p, Variable _o -> 
                    rdf.GetTriplesWithPredicate(p)
            | Variable _s, Variable _p, Term.Resource o -> 
                    rdf.GetTriplesWithObject(o)
            | Term.Resource s, Term.Resource p, Variable _o -> 
                    rdf.GetTriplesWithSubjectPredicate(s, p)
            | Variable _s, Term.Resource p, Term.Resource o -> 
                    rdf.GetTriplesWithObjectPredicate(o, p)
            | Term.Resource s, Term.Resource p, Term.Resource o -> 
                    match rdf.ThreeKeysIndex.TryGetValue {subject = s; predicate = p; obj = o} with
                    | false,_ -> []
                    | true, v -> [rdf.GetTripleListEntry v]                    
            | Term.Resource s, Variable p, Term.Resource o ->
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
    
