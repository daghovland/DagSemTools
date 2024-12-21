(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Ingress
(* Helper function(s) related to the list and option monads in F#  *)

module Monad =

    let flattenOptionList (inputList) =
        List.fold
            (fun agg el ->
                match (agg, el) with
                | (Some ls, Some e) -> Some(e :: ls)
                | _ -> None)
            (Some [])
            inputList
