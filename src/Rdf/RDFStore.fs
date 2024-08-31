(*
    Copyright (C) 2024 Dag Hovland
    
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    
    Contact: hovlanddag@gmail.com
*)
namespace Rdf

open System
open IriTools

module RDFStore =
    type ResourceId = int
    type Resource =
        | Iri of IriReference
        | Literal of string
    type TripleLookup =
        | ArrayIndex of int
        | End
    type Triple = {
        subject: ResourceId
        predicate: ResourceId
        object: ResourceId
    }
    type TripleListEntry = {
        triple: Triple
        next_subject_predicate_list: TripleLookup
        next_predicate_list: TripleLookup
        next_object_predicate_list: TripleLookup
    }
    
    type TripleTable = {
        ResourceMap : Map<Resource, ResourceId>
        ResourceList : array<Resource>
        TripleList : array<TripleListEntry>
        ThreeKeysIndex: Map<Triple, TripleLookup>
        subject_index: array<TripleLookup>
        predicate_index: array<TripleLookup>
        object_index: array<TripleLookup>
        subject_predicate_index: Map<Tuple<ResourceId, ResourceId>, TripleLookup>
        object_predicate_index: Map<Tuple<ResourceId, ResourceId>, TripleLookup>        
    }
      
    
    
  
    
