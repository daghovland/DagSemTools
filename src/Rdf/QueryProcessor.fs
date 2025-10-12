(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.Rdf

module QueryProcessor =
    let Answer (datastore : Datastore) (query : Query.SelectQuery) : (string * string) list seq =
        let variableNames = 
            query.BGPs
            |> List.collect (fun pattern -> 
                [ pattern.Subject; pattern.Predicate; pattern.Object ])
            |> List.choose (function | Variable vName -> Some vName | Resource _ -> None)
            |> List.distinct
        // For simplicity, we will return empty results for now
        Seq.empty

