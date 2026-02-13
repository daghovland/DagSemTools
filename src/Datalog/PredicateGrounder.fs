(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Datalog

open DagSemTools.Rdf.Query


(* 
    This module makes it possible with variables in the predicate position in datalog rules
    It first finds all predicates in use in the data or in the head of rules, and then creates 
    a new rule for each such predicate whenever a variable is used in the predicate position
 *)
module PredicateGrounder =
    open DagSemTools.Rdf
    
    let getQuadPredicate (quad: QuadPattern)  =
        match quad.Predicate with
        | Variable _ -> None
        | Term.Resource r -> Some r

    let getRuleHeadPredicate (head : RuleHead) =
        match head with
        | Contradiction -> None
        | NormalHead tp -> getQuadPredicate tp
    let getAtomPredicate (atom: RuleAtom)  =
        match atom with
        | PositivePattern triple -> getQuadPredicate triple
        | NotPattern triple -> getQuadPredicate triple
        | NotEqualsAtom (t1, t2) -> None
    let getPredicatesInUse (rules: Rule seq, triplestore: Datastore) =
        let headPredicates = rules |> Seq.choose (fun rule -> getRuleHeadPredicate rule.Head)
        let dataPredicates = triplestore.Resources.GetIriResourceIds()
        Seq. concat [headPredicates; dataPredicates] |> Seq.distinct

    let instantiateResourceWithVariableMapping variableName predicate res =
            match res with
                    | _variableName when _variableName = variableName -> Term.Resource predicate
                    | _ -> res
    
    let instantiateTripleWithVariableMapping (quad) (variableName : Term) predicate =
        let tripleList =
            [ quad.Graph;  quad.Subject; quad.Predicate; quad.Object]
            |> List.map (instantiateResourceWithVariableMapping variableName predicate)
        {Graph = tripleList[0]; Subject = tripleList.[1]; Predicate = tripleList.[2]; Object = tripleList.[3]}
        
    let instantiateRuleWithVariableMapping (predicate, rule: Rule, variableName) =
        let newHead = match rule.Head with
                        | Contradiction -> Contradiction
                        | NormalHead triplePattern -> NormalHead {triplePattern with Predicate = Term.Resource predicate}
        let newBody = rule.Body |> List.map (
            fun atom ->
                match atom with
                | PositivePattern triple -> PositivePattern (instantiateTripleWithVariableMapping triple variableName predicate)
                | RuleAtom.NotPattern triple -> NotPattern (instantiateTripleWithVariableMapping triple variableName predicate)
                | NotEqualsAtom (t1, t2) -> NotEqualsAtom (instantiateResourceWithVariableMapping variableName predicate t1,
                                                           instantiateResourceWithVariableMapping variableName predicate t2)
                )
        {rule with Head = newHead; Body = newBody}
    
    let multiplyRuleHeadWithPredicates predicates (rule: Rule) : Rule seq=
        match rule.Head with
        | Contradiction -> [rule]
        | NormalHead ruleHead ->
                        match ruleHead.Predicate with
                        | Variable s -> predicates
                                        |> Seq.map (fun p -> instantiateRuleWithVariableMapping(p, rule, Variable s))
                        | Term.Resource _ -> [rule]

    let getTripleRelationVariable (triple : QuadPattern) =
        match triple.Predicate with
        | Variable s -> Some s
        | Term.Resource _ -> None
        
    let getBodyRelationVariables (rule: Rule) =
        rule.Body |> List.choose (fun atom ->
            match atom with
            | PositivePattern triple -> getTripleRelationVariable triple
            | NotPattern triple -> getTripleRelationVariable triple
            | NotEqualsAtom (t1, t2) -> None
            )
    
    let getGroundPredicateRules (rule: Rule) predicates (relationVariableName: string) : Rule seq =
        predicates |> Seq.map (fun pred -> instantiateRuleWithVariableMapping(pred, rule, Variable relationVariableName))
    
    (* 
        This assumes that the head has already a ground relation
        Currently not used, since attempting to make the reasoner handle variables in the body 
        in the stratification properly.
     *)
    let multiplyRuleBodyWithPredicates predicates (rule: Rule) : Rule seq =
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
