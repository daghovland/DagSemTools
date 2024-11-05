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
        let mutable Rules = Rules
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
        
        (* 
            The semi-naive materialisation algorithm. Assumes a non-cyclic ruleset
            Usually called from the evaluate function, which will stratify the ruleset
        *)
        member this.materialiseNaive() =
                for triple in tripleStore.Triples.GetTriples() do
                    for rules in this.GetRulesForFact triple do
                        for subs in evaluate tripleStore.Triples rules  do
                            let newTriple = ApplySubstitutionTriple subs rules.Match.Rule.Head
                            tripleStore.AddTriple newTriple

    let evaluate (rules: Rule list, triplestore: Datastore) =
            let rules_with_iri_predicates = PredicateGrounder.groundRulePredicates(rules, triplestore) |> Seq.toList
            let stratifier = RulePartitioner rules_with_iri_predicates
            let stratification = stratifier.orderRules
            for partition in stratification do
                let program = DatalogProgram(Rules = Seq.toList partition, tripleStore = triplestore)
                program.materialiseNaive()