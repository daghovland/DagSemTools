(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

module DagSemTools.AlcTableau.ConjunctiveQuery

open ALC
open IriTools

type ConceptSignature =
    | Variable of string
    | Concept of ALC.Concept
    
type RoleSignature =
    | Role of ALC.Role
    | Variable of string
    
type IndividualSignature =
    | Individual of IriReference
    | Variable of string
open System
type QueryAtom =
    | ConceptQuery of ConceptSignature * IndividualSignature
    | RoleQuery of role: RoleSignature * left: IndividualSignature * right: IndividualSignature

type ConjunctiveQuery = QueryAtom list

