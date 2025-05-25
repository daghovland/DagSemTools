(*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)


namespace DagSemTools.Datalog

open DagSemTools.Rdf.Ingress

(* 
    The RuleCollection maintains a collection of rules and implements methods for 
    getting a rule where a body atom matches, or where a head matches. The methods are made to support the stratification algorithm
    The method names are taken from https://ojs.aaai.org/index.php/AAAI/article/view/9409
 *)
module internal Unification =
    
    let VariableConstantUnifiable v res (constantMap : Map<string, GraphElementId>)  (variableMap : Map<string, string>)=
            let varName = variableMap.TryFind v |> Option.defaultValue v
            match constantMap.TryFind varName with
            | None -> Some (Map.add v res constantMap, variableMap)
            | Some res2 ->  if res = res2 then Some (constantMap, variableMap) else None
        
    (* Two terms are unifiable if they can be mapped to the same constant *)
    let internal TermsUnifiable (term1 : Term) (term2 : Term) (constantMap : Map<string, GraphElementId>, variableMap : Map<string, string>)=
        match term1, term2 with
        | Term.Variable v, Term.Resource res1 ->
           VariableConstantUnifiable v res1 constantMap variableMap
        | Term.Resource res, Term.Variable v ->
           VariableConstantUnifiable v res constantMap variableMap
        | Term.Resource res1, Term.Resource res2 -> if res1 = res2 then (Some (constantMap, variableMap)) else None
        | Term.Variable v1, Term.Variable v2 -> (Some (constantMap, (Map.add v1 (v2) variableMap)))
    
    (* Two triple patterns are unifiable if there exists a mapping of the variables such that they are equal *)
    let internal triplePatternsUnifiable (triple1 : TriplePattern) (triple2 : TriplePattern)  =
        TermsUnifiable triple1.Subject triple2.Subject (Map.empty, Map.empty)
        |> Option.bind (TermsUnifiable triple1.Predicate triple2.Predicate)
        |> Option.bind (TermsUnifiable triple1.Object triple2.Object)
        |> Option.isSome
    
      (*
        These are the edges in the dependency graph of triple patterns
        There is an edge from the pattern in the head of a rule to every atom in the body
        The edge is negative if the body atom is negative
        The stratification checks for cycles with negative edges
        See the Alice book for more info
    *)
    [<Struct>]
    [<StructuralEquality>]
    [<StructuralComparison>]
    type PatternEdge =
        | PositivePatternEdge of pedge: Rule
        | NegativePatternEdge of nedge: Rule
    with member this.GetRule() =
            match this with
                | PositivePatternEdge pedge -> pedge
                | NegativePatternEdge nedge -> nedge
    
    type UnificationResult =
        | PositiveRelation
        | NegativeRelation
        
    let internal triplePatternAtomUnifiable (triple1 : TriplePattern) (atom : RuleAtom) =
        match atom with
        | NotTriple pattern -> if triplePatternsUnifiable triple1 pattern then Some NegativeRelation else None
        | PositiveTriple pattern -> if triplePatternsUnifiable triple1 pattern then Some PositiveRelation else None
        | NotEqualsAtom (t1, t2) -> None
    
    (* Returns all rules where there is a body atom that is unifiable with the triple pattern
      TODO: This should probably be cached or indexed in a better way
     *)
    let internal DependingRules rules triplePattern =
        rules |> List.choose (fun rule ->
            let edges = rule.Body
                        |> Seq.choose (triplePatternAtomUnifiable triplePattern)
            if edges |> Seq.isEmpty then
                None
            elif edges |> Seq.contains NegativeRelation then
                Some (NegativePatternEdge rule)
            else
                Some (PositivePatternEdge rule))
                

    (* Returns all rules where the Head is unifiable with the given pattern *)
    let internal IntentionalRules rules triplePattern =
        rules |> Seq.where (fun rule ->
            match rule.Head with
            | Contradiction -> false
            | NormalHead headPattern -> triplePatternsUnifiable triplePattern headPattern)
