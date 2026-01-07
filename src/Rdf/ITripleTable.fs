(*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.Rdf
open Ingress


type ITripleTable =
    abstract member TripleCount : unit -> uint32
    abstract member GetTriples : unit -> Triple seq
    abstract member Contains : Triple -> bool
    abstract member GetTriplesWithSubject : GraphElementId -> Triple seq
    abstract member GetTriplesWithObject : GraphElementId -> Triple seq
    abstract member GetTriplesWithPredicate : GraphElementId -> Triple seq
    abstract member GetPredicates : unit -> GraphElementId seq
    abstract member GetTriplesWithSubjectPredicate : GraphElementId * GraphElementId -> Triple seq
    abstract member GetTriplesWithObjectPredicate : GraphElementId * GraphElementId -> Triple seq
    abstract member GetTriplesWithSubjectObject : GraphElementId * GraphElementId -> Triple seq
    abstract member GetTriplesMentioning : GraphElementId -> Triple seq