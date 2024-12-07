(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
module DagSemTools.Manchester.Printer

open DagSemTools.AlcTableau.ALC

let printRole (role: Role) = 
    role.ToString()
let rec toString (concept: Concept) =
    match concept with
    | ConceptName iri -> $"%s{iri.ToString()}"
    | Disjunction (left, right) -> 
        $"{toString left} or {toString right}"
    | Conjunction (left, right) -> 
        $"{toString left} and {toString right}"
    | Negation concept -> 
        $"not {toString concept}"
    | Existential (role, concept) ->
        $"{printRole role} some {toString concept}"
    | Universal (role, concept) -> 
        $"{printRole role} only {toString concept}"
    | Top -> "owl:Thing"
    | Bottom -> "owl:Nothing"

and ontologyToString (tbox: TBoxAxiom list) : string =
        List.map (fun ax -> axiomToString ax) tbox  
        |>  List.fold (fun acc elem -> acc + elem) ""

and axiomToString (axiom: TBoxAxiom) : string = 
    match axiom with
    | Inclusion (sup, sub) -> $"%s{toString sub} <= {toString sup}"
    | Equivalence (left, right) -> $"%s{toString left} >= {toString right}"
    
