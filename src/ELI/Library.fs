(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

(* Translation from ELI to RL, inspired by https://arxiv.org/abs/2008.02232 *)

namespace DagSemTools.ELI

open DagSemTools.ELI.ELIExtractor
open DagSemTools.OwlOntology
open DagSemTools.Rdf
open Serilog

module Library =

    let Owl2Datalog (logger : ILogger) (resources: ResourceManager) (axiom: ClassAxiom) =
        axiom |> ELIAxiomExtractor logger |> Option.map (ELI2RL.GenerateTBoxRL logger resources)
