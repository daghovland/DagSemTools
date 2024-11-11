(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Datalog

open DagSemTools.Rdf
open Datalog
open Stratifier
    
module Reasoner =

    type DatalogProgram (Rules: Rule list, tripleStore : Datastore) =
        
        let GetUnsafeRules (rules : Rule seq) =
            rules |> Seq.filter (not << isSafeRule)
        let mutable Rules =
            let unsafeRules = GetUnsafeRules Rules
            if unsafeRules |> Seq.isEmpty then
                Rules
            else
                let unsafeRuleStrings = unsafeRules
                                                |> Seq.map (fun rule -> rule.ToString())
                                                
                raise (new System.ArgumentException("These rules are not safe: " + String.concat "" unsafeRuleStrings))
                
        let mutable RuleMap : Map<TripleWildcard, PartialRule list>  =
                            Rules
                                |> List.map GetPartialMatches
                                |> mergeMaps
                                   
        member this.AddRule(rule: Rule)  =
            if not (isSafeRule rule) then
                raise (new System.ArgumentException("Rule is not safe"))
            Rules <- rule :: Rules
            RuleMap <- mergeMaps [RuleMap; GetPartialMatches rule]
                
            
        member this.GetRulesForFact(fact: Ingress.Triple) : PartialRuleMatch seq = 
            ConstantTriplePattern fact
                |> WildcardTriplePattern
                |> Seq.map (fun wildcardFact ->
                    match RuleMap.TryGetValue(wildcardFact) with
                    | true, rules -> rules
                    | false, _ -> [])
                |> Seq.distinct
                |> Seq.collect (Seq.collect (GetMatchesForRule fact))
        
        
        member this.GetFacts() =
            Rules
                |> Seq.filter isFact
                |> Seq.map (fun rule -> rule.Head)
                |> Seq.map (ApplySubstitutionTriple emptySubstitution)
        
        (* 
            The semi-naive materialisation algorithm. Assumes a non-cyclic ruleset
            Usually called from the evaluate function, which will stratify the ruleset
        *)
        member internal this.materialiseNaive() =
                this.GetFacts() |> Seq.iter tripleStore.AddTriple
                for triple in tripleStore.Triples.GetTriples() do
                    for rules in this.GetRulesForFact triple do
                        for subs in evaluate tripleStore.Triples rules  do
                            let newTriple = ApplySubstitutionTriple subs rules.Match.Rule.Head
                            tripleStore.AddTriple newTriple

    let evaluate (rules: Rule list, triplestore: Datastore) =
            // let rules_with_iri_predicates = PredicateGrounder.groundRulePredicates(rules, triplestore) |> Seq.toList
            let stratifier = RulePartitioner rules
            let stratification = stratifier.orderRules
            for partition in stratification do
                let program = DatalogProgram(Rules = Seq.toList partition, tripleStore = triplestore)
                program.materialiseNaive()