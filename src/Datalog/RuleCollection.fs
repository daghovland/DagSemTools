(*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)


namespace DagSemTools.Datalog

open System.Collections.Generic
open DagSemTools.Rdf.Ingress
open Serilog
open Datalog

(* 
    The RuleCollection maintains a collection of rules and implements methods for 
    getting a rule where a body atom matches, or where a head matches. The methods are made to support the stratification algorithm
    The method names are taken from https://ojs.aaai.org/index.php/AAAI/article/view/9409
 *)
module internal RuleCollection =
    let internal MatchBody (rules : Rule array) (triplePatter : TriplePattern) =
        rules |> Array.map (fun rule ->
            rule.Body |> List.map (fun bodyAtom ->
                Datalog.triplePatternsUnifiable triplePatter bodyAtom))

