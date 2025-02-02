(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.    
    Contact: hovlanddag@gmail.com
*)


namespace DagSemTools.AlcTableau

open ALC

module NNF = 
    let rec internal nnf_concept concept =
        match concept with
        | Negation concept1 -> push_negation concept1
        | Conjunction (c1, c2) -> Conjunction(nnf_concept c1, nnf_concept c2)
        | Disjunction (c1, c2) -> Disjunction(nnf_concept c1, nnf_concept c2)
        | Existential (r1, c1) -> Existential(r1, nnf_concept c1)
        | Universal (r1, c1) -> Universal(r1, nnf_concept c1)
        | _ -> concept
    and internal push_negation c =
        match c with
        | Negation concept1 -> nnf_concept concept1
        | ConceptName iri1 -> Negation c
        | Conjunction (c1, c2) -> Disjunction(push_negation c1, push_negation c2)
        | Disjunction (c1, c2) -> Conjunction(push_negation c1, push_negation c2)
        | Existential (r1, c1) -> Universal(r1, push_negation c1)
        | Universal (r1, c1) -> Existential(r1, push_negation c1)
        | Top -> Bottom
        | Bottom -> Top
    
    let internal nnf_assertion assertion =
        match assertion with
            | ConceptAssertion (individual, concept) -> ConceptAssertion(individual, nnf_concept concept)
            | NegativeAssertion (RoleAssertion (individual, object, role)) -> NegativeRoleAssertion (individual, object, role)
            | NegativeAssertion (NegativeRoleAssertion (individual, object, role)) -> RoleAssertion (individual, object, role)
            | NegativeAssertion (ConceptAssertion (individual, concept)) -> ConceptAssertion(individual, nnf_concept (ALC.Negation concept))
            | NegativeAssertion (LiteralAssertion (individual, data, prop)) -> NegativeLiteralAssertion (individual, data, prop)
            | NegativeAssertion (NegativeAssertion assertion) -> assertion
            | NegativeAssertion (NegativeLiteralAssertion (individual, data, prop)) -> LiteralAssertion(individual, data, prop)
            
            | NegativeAssertion _ -> failwith "Arbitrary negative assertions are not yet supported"
            | r -> r
            
            
            
    let internal nnf_kb (tbox, abox) =
        (tbox, List.map nnf_assertion abox)
        