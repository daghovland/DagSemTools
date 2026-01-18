(*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.Rdf
open Ingress

open System
open System.Collections.Generic

               
type NamedTripleTable(quads: QuadTable, graphName: GraphElementId) =
        
    interface ITripleTable with
        member this.TripleCount()  =
            (uint32) quads.TripleIdIndex[graphName].Length 
            
        member this.GetTriples() : Triple seq =
            quads.GetTriplesWithId graphName
        member this.Contains (triple : Triple) : bool =
            quads.Contains (tripleToQuad triple graphName)
        member this.GetTriplesWithSubject subject : Triple seq =
            quads.GetQuadsWithIdSubject (graphName, subject)
        member this.GetTriplesWithPredicate (predicate: GraphElementId) : Triple seq =
            quads.GetTriplesWithIdPredicate (graphName, predicate)
        member this.GetTriplesWithObject (obj: GraphElementId) : Triple seq =
            quads.GetTriplesWithIdObject (graphName, obj)
        member this.GetPredicates() : GraphElementId seq =
            quads.PredicateIndex.Keys 
        member this.GetTriplesWithSubjectPredicate (subject: GraphElementId, predicate: GraphElementId) =
            quads.GetTriplesWithIdSubjectPredicate (graphName, subject, predicate)
        member this.GetTriplesWithObjectPredicate (obj: GraphElementId, predicate: GraphElementId) =
            quads.GetTriplesWithIdObjectPredicate (graphName, obj, predicate)
        member this.GetTriplesWithSubjectObject (subject: GraphElementId, object: GraphElementId) : Triple seq =
            quads.GetTriplesWithIdSubjectObject (graphName, subject, object)
        member this.GetTriplesMentioning resource =
               Seq.concat [quads.GetQuadsWithIdSubject (graphName, resource)
                           quads.GetTriplesWithIdPredicate (graphName, resource)
                           quads.GetTriplesWithIdObject (graphName, resource)]