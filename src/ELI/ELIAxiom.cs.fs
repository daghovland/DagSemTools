(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

(* Implementation of the translation in section 4.3 of https://www.w3.org/TR/owl2-profiles/#OWL_2_RL *)

namespace DagSemTools.ELI

open DagSemTools.OwlOntology

module Axioms =
    
    type ELIClass =
        | Top 
        | ClassName of Class
        | Intersection of ELIClass list
        | SomeValuesFrom of ObjectPropertyExpression * ELIClass
    type ELIAxiom =
        SubClassAxiom of ELIClass list * Class list 
        
        
    
