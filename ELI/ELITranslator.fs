(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

(* Implementation of the translation in section 4.3 of https://www.w3.org/TR/owl2-profiles/#OWL_2_RL *)

namespace DagSemTools.OWL2RL2Datalog
open System.IO
open DagSemTools.Rdf
open DagSemTools.Datalog
open DagSemTools.Ingress
open DagSemTools.OwlOntology
open IriTools

module ELITranslator =
    
    let ELIClassExtractor classExpression =
        match classExpression with
        | ClassName className -> 
    
    (*
        Separates the axioms into ELI-axioms and non-ELI-axioms
        ELI-Axioms are subclass axioms  
    *)
    
    
    let ELIEAxiomxtractor (axiom : Axiom) =
        match axiom with
        | AxiomClassAxiom clAxiom -> match clAxiom with
                                        | SubClassOf (_, sub, super) -> match super with
                                                                        | ClassName superClass -> Some 
        | _ -> None