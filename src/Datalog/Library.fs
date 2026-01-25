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
open DagSemTools.Rdf.Query

   
[<StructuralComparison>]
[<StructuralEquality>]
type ResourceOrWildcard = 
    | Resource of GraphElementId
    | Wildcard

[<CustomComparison>]
[<CustomEquality>]
type RuleHead =
    | NormalHead of pattern: QuadPattern
    | Contradiction
    member this.GetVariables() =
        match this with
        | NormalHead graphPattern -> graphPattern.GetVariables()
        | Contradiction -> []
    override this.ToString() =
        match this with
        | NormalHead tp -> tp.ToString()
        | Contradiction -> "false"
    member this.ToString(manager) =
        match this with
        | NormalHead tp -> tp.ToString(manager)
        | Contradiction -> "false"

    interface System.IComparable with
        member this.CompareTo(obj) =
            match obj with
            | :? RuleHead as other ->
                match this, other with
                | Contradiction, Contradiction -> 0
                | Contradiction, _ -> -1
                | _, Contradiction -> 1
                | NormalHead p1, NormalHead p2 -> 
                    compare p1 p2
            | _ -> 1  
            
    override this.Equals(obj) =
        match obj with
        | :? RuleHead as other -> (this :> System.IComparable).CompareTo(other) = 0
        | _ -> false
        
    override this.GetHashCode() =
        match this with
        | Contradiction -> -1
        | NormalHead p -> hash p

[<StructuralComparison>]
[<StructuralEquality>]
type RuleAtom = 
    | PositivePattern of QuadPattern
    | NotPattern of QuadPattern
    | NotEqualsAtom of Term * Term
    override this.ToString () =
        match this with
        | PositivePattern tp -> tp.ToString()
        | NotPattern tp -> $"not {tp.ToString()}"
        | NotEqualsAtom (t1, t2) -> $"{t1.ToString()} != {t2.ToString()}"
    member this.ToString (manager) =
        match this with
        | PositivePattern tp -> tp.ToString(manager)
        | NotPattern tp -> $"not {tp.ToString(manager)}"
        | NotEqualsAtom (t1, t2) -> $"{t1.ToString(manager)} != {t2.ToString(manager)}"
    member this.GetVariables() =
         match this with
            | PositivePattern t -> t.GetVariables()
            | NotPattern t -> t.GetVariables()
            | NotEqualsAtom (t1, t2) -> 
                    [
                        match t1 with | Variable v1 -> yield v1 | _ -> ()
                        match t2 with | Variable v2 -> yield v2 | _ -> ()
                    ]

[<StructuralComparison>]
[<StructuralEquality>]
type QuadWildcard = 
    {Graph: ResourceOrWildcard; Subject: ResourceOrWildcard; Predicate: ResourceOrWildcard; Object: ResourceOrWildcard}



[<StructuralComparison>]
[<StructuralEquality>]
type Rule = 
    {Head: RuleHead; Body: RuleAtom list}
    override this.ToString () =
        let bodyString = this.Body
                            |> List.map (fun el -> el.ToString())
                            |> String.concat ","
        $"{this.Head.ToString()} :- {bodyString} .\n"
    member this.ToString (manager) =
        let bodyString = this.Body
                            |> List.map (fun el -> el.ToString(manager))
                            |> String.concat ","
        $"{this.Head.ToString(manager)} :- {bodyString} .\n"
type Substitution = 
    Map<string, Ingress.GraphElementId>
type PartialRule = 
    {Rule: Rule; Match : QuadPattern}
type PartialRuleMatch = 
    {Match: PartialRule; Substitution: Substitution}


module Datalog =
    let emptySubstitution : Substitution = Map.empty
    let isFact (rule) = rule.Body |> List.isEmpty
    
    let ConstantQuadPattern (quad: Ingress.Quad) : QuadPattern = 
        {QuadPattern.Graph = Term.Resource quad.tripleId
         Subject = Term.Resource quad.subject
         Predicate = Term.Resource quad.predicate; Object = Term.Resource quad.obj}
    
    /// Generate all 8 possible triple patterns with wildcards for a given triple pattern
    /// Duplicate patterns are ok since these are used as a key in a dictionary
    let WildcardQuadPattern (quad: QuadPattern) = 
        let resourceList = [quad.Graph; quad.Subject; quad.Predicate; quad.Object]
        let rec generatePatterns (quad: Term list) : ResourceOrWildcard list list = 
              match quad with
              | [] -> [[]]
              | head :: tail -> 
                let rest = generatePatterns tail
                match head with
                | Variable _ -> 
                     rest |> List.map (fun quadPart -> Wildcard :: quadPart)
                | Term.Resource r -> 
                    rest |> List.collect (fun quadPart -> [Resource r :: quadPart; Wildcard :: quadPart])
        generatePatterns resourceList |> List.map (fun quadPart ->
            {QuadWildcard.Graph = List.item 0 quadPart
             Subject = List.item 1 quadPart
             Predicate = List.item 2 quadPart
             Object = List.item 3 quadPart})
        
    (* Safe rules are those where the head only has variable that are in the body *)
    let GetUnsafeHeadVariables (rule) =
        let variablesInBody = rule.Body
                                |> Seq.collect (fun atom -> atom.GetVariables()
                                )
        let variablesInHead = rule.Head.GetVariables()
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
    let ApplySubstitutionQuad sub (quad: QuadPattern) : Quad =
        {
         Quad.tripleId = ApplySubstitutionResource sub quad.Graph
         subject = ApplySubstitutionResource sub quad.Subject
         predicate = ApplySubstitutionResource sub quad.Predicate
         obj = ApplySubstitutionResource sub quad.Object 
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
    let GetSubstitutions (subs) (fact : Quad) (factPattern : QuadPattern)  : Substitution option =
        let resourceList = [
                            (fact.tripleId, factPattern.Graph)
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
                                | PositivePattern t -> Some t
                                | NotPattern t -> None
                                | NotEqualsAtom (t1, t2) -> None
                    )
        |> Seq.map (fun r -> r, GetSubstitutions (Map.empty) fact r) 
        |> Seq.choose (fun (r, s) -> Option.map (fun s -> {Match = rule; Substitution = s}) s)
        
    let GetPartialMatch (quad : QuadPattern)  =
        WildcardQuadPattern quad
        
    let GetPartialMatches (rule : Rule) : Map<QuadWildcard, PartialRule list> =
       Map.ofSeq (rule.Body
       |> Seq.choose (fun atom -> match atom with
                                    | RuleAtom.PositivePattern t -> Some t
                                    | NotPattern t -> None
                                    // TODO: This might need a match
                                    | NotEqualsAtom (t1, t2) -> None
                    )
       |> Seq.collect (fun pat ->
           WildcardQuadPattern pat
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

    let evaluatePattern (rdf : QuadTable) (triplePattern : QuadPattern) (sub : Substitution)  =
        let mappedQuad : QuadPattern = {
                            QuadPattern.Graph = GetMappedResource sub triplePattern.Graph
                            QuadPattern.Subject = GetMappedResource sub triplePattern.Subject
                            QuadPattern.Predicate = GetMappedResource sub triplePattern.Predicate
                            QuadPattern.Object = GetMappedResource sub triplePattern.Object
                            }
        let matchedQuads = (
            match mappedQuad.Graph, mappedQuad.Subject, mappedQuad.Predicate, mappedQuad.Object with
            | Term.Resource g, Term.Resource s, Variable _p, Variable _o -> 
                    rdf.GetQuadsWithIdSubject(g, s)
            | Term.Resource g, Variable _s, Term.Resource p, Variable _o -> 
                    rdf.GetQuadsWithIdPredicate(g, p)
            | Term.Resource g, Variable _s, Variable _p, Term.Resource o -> 
                    rdf.GetQuadsWithIdObject(g, o)
            | Term.Resource g, Term.Resource s, Term.Resource p, Variable _o -> 
                    rdf.GetQuadsWithIdSubjectPredicate(g, s, p)
            | Term.Resource g, Variable _s, Term.Resource p, Term.Resource o -> 
                    rdf.GetQuadsWithIdObjectPredicate(g, o, p)
            | Term.Resource g, Term.Resource s, Term.Resource p, Term.Resource o -> 
                    let quad = {Quad.tripleId = g; Quad.subject = s; predicate = p; obj = o}
                    match rdf.Contains quad with
                    | false -> []
                    | true -> [ quad ]                    
            | Term.Resource g, Term.Resource s, Variable p, Term.Resource o ->
                rdf.GetQuadsWithIdSubjectObject (g, s, o)
            | Term.Resource g, Variable s, Variable p, Variable o -> rdf.GetQuadsWithId(g)
            | Variable _g, Term.Resource s, Variable _p, Variable _o -> 
                    rdf.GetQuadsWithSubject(s)
            | Variable _g, Variable _s, Term.Resource p, Variable _o -> 
                    rdf.GetQuadsWithPredicate(p)
            | Variable _g, Variable _s, Variable _p, Term.Resource o -> 
                    rdf.GetQuadsWithObject(o)
            | Variable _g, Term.Resource s, Term.Resource p, Variable _o -> 
                    rdf.GetQuadsWithSubjectPredicate(s, p)
            | Variable _g, Variable _s, Term.Resource p, Term.Resource o -> 
                    rdf.GetQuadsWithObjectPredicate(o, p)
            | Variable _g, Term.Resource s, Term.Resource p, Term.Resource o -> 
                    rdf.GetQuadsWithTriple(s, p, o)       
            | Variable _g, Term.Resource s, Variable p, Term.Resource o ->
                rdf.GetQuadsWithSubjectObject (s, o)
            | Variable _g, Variable s, Variable p, Variable o -> rdf.GetQuads) 
        matchedQuads |> Seq.choose (fun t -> GetSubstitutions sub t mappedQuad)
                            
    let evaluatePositive (rdf : QuadTable) (ruleMatch : PartialRuleMatch) : Substitution seq =
         ruleMatch.Match.Rule.Body
        |> Seq.choose (fun atom -> match atom with
                                    | PositivePattern t -> Some t
                                    | NotPattern t -> None
                                    | NotEqualsAtom (t1, t2) -> None
                    )
        |> Seq.fold
            ( fun subs tr ->
                    subs |> Seq.collect (evaluatePattern rdf tr) )
            [ruleMatch.Substitution]  
    
    let evaluate (rdf : QuadTable) (ruleMatch : PartialRuleMatch)  : Substitution seq =
        ruleMatch.Match.Rule.Body
        |> Seq.choose (fun atom -> match atom with
                                    | PositivePattern _ -> None
                                    | NotPattern t -> Some t
                                    | NotEqualsAtom (t1, t2) -> None
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
    
    