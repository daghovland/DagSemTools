(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Datalog


(* 
    This module makes it possible with variables in the predicate position in datalog rules
    It first finds all predicates in use in the data or in the head of rules, and then creates 
    a new rule for each such predicate whenever a variable is used in the predicate position
 *)
module PredicateGrounder =
    open DagSemTools.Rdf
    
    let getTriplePredicate (triple: TriplePattern)  =
        match triple.Predicate with
        | Variable _ -> None
        | ResourceOrVariable.Resource r -> Some r

    let getAtomPredicate (atom: RuleAtom)  =
        match atom with
        | PositiveTriple triple -> getTriplePredicate triple
        | NotTriple triple -> getTriplePredicate triple    
    let getPredicatesInUse (rules: Rule seq, triplestore: Datastore) =
        let headPredicates = rules |> Seq.choose (fun rule -> getTriplePredicate rule.Head)
        let dataPredicates = triplestore.Triples.GetPredicates()
        Seq. concat [headPredicates; dataPredicates] |> Seq.distinct

    let instantiateTripleWithPredicate (triple : TriplePattern) (variableName : ResourceOrVariable) (predicate : Ingress.ResourceId) =
        match triple.Predicate with
        | _variableName when _variableName = variableName -> {triple with Predicate = ResourceOrVariable.Resource predicate}
        | _ -> triple
        
    let instantiateRuleWithPredicate (predicate: Ingress.ResourceId, rule: Rule, variableName) =
        let newHead = {rule.Head with Predicate = ResourceOrVariable.Resource predicate}
        let newBody = rule.Body |> List.map (
            fun atom ->
                match atom with
                | PositiveTriple triple -> PositiveTriple (instantiateTripleWithPredicate triple variableName predicate)
                | RuleAtom.NotTriple triple -> NotTriple (instantiateTripleWithPredicate triple variableName predicate)
                )
        {rule with Head = newHead; Body = newBody}
    
    
    let multiplyRuleHeadWithPredicates (predicates: Ingress.ResourceId seq) (rule: Rule) =
        match rule.Head.Predicate with
        | Variable s -> predicates |> Seq.map (fun p -> instantiateRuleWithPredicate(p, rule, Variable s))
        | ResourceOrVariable.Resource _ -> [rule]

    let getTripleRelationVariable (triple : TriplePattern) =
        match triple.Predicate with
        | Variable s -> Some s
        | ResourceOrVariable.Resource _ -> None
        
    let getBodyRelationVariables (rule: Rule) =
        rule.Body |> List.choose (fun atom ->
            match atom with
            | PositiveTriple triple -> getTripleRelationVariable triple
            | NotTriple triple -> getTripleRelationVariable triple
            )
    
    let getGroundPredicateRules (rule: Rule) (predicates: Ingress.ResourceId seq) (relationVariableName: string) : Rule seq =
        predicates |> Seq.map (fun pred -> instantiateRuleWithPredicate(pred, rule, Variable relationVariableName))
    
    (* This assumes that the head has already a ground relation *)
    let multiplyRuleBodyWithPredicates (predicates: Ingress.ResourceId seq) (rule: Rule) : Rule seq =
        let relationVariables = getBodyRelationVariables rule
        if relationVariables |> Seq.isEmpty then
            [rule]
        else
            relationVariables
                |> Seq.distinct
                |> Seq.collect (getGroundPredicateRules rule predicates)
        
    let groundRulePredicates (rules: Rule list, triplestore: Datastore) =
        let predicatesInUse = getPredicatesInUse(rules, triplestore)
        rules |> Seq.collect (multiplyRuleHeadWithPredicates predicatesInUse)
              |> Seq.collect (multiplyRuleBodyWithPredicates predicatesInUse)
