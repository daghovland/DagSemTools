(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

(* Implementation of the translation in section 4.3 of https://www.w3.org/TR/owl2-profiles/#OWL_2_RL *)
(* See also https://www.emse.fr/~zimmermann/Teaching/KRR/el.html *)
namespace DagSemTools.ELI

open DagSemTools.OwlOntology

module Axioms =

    type ComplexConcept =
        | Top
        | AtomicConcept of Class
        | Intersection of ComplexConcept list
        | SomeValuesFrom of ObjectPropertyExpression * ComplexConcept


    type NormalizedConcept =
        | Bottom
        | AtomicNamedConcept of Class
        | AtomicAnonymousConcept
        | ObjectHasValue of ObjectPropertyExpression * Individual
        | AllValuesFrom of ObjectPropertyExpression * Class
        | AtMostOneValueFromQualified of ObjectPropertyExpression * Class
        | AtMostOneValueFrom of ObjectPropertyExpression


    type Formula =
        (* of the form U C_i <= /\ A_i , where U C_i is a disjunction of ELI-concepts and /\ A_i is a conjunction of atomic concept *)
        | DirectlyTranslatableConceptInclusion of subclassDisjunction: ComplexConcept list * superclassConjunction: Class list
        (* /\ A_i <= C, where /\ A_i is a conjunction of atomic concepts and C is a concept *)
        | NormalizedConceptInclusion of subclassConjunction: Class list * superclass: NormalizedConcept
