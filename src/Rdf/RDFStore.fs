(*
    Copyright (C) 2024 Dag Hovland
    
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    
    Contact: hovlanddag@gmail.com
*)
namespace Rdf

open System
open System.Resources
open IriTools

module RDFStore =
    type ResourceId = uint32
    [<StructuralComparison>]
    [<StructuralEquality>]
    [<Struct>]
    type Resource =
        Iri of iri:  IriReference
        | Literal of  literal: string
    
    [<Struct>]
    type TripleLookup =
        | ArrayIndex of index: int
        | End
    type Triple =
        struct
            val subject: ResourceId
            val predicate: ResourceId
            val object: ResourceId
        end
    type TripleListEntry =
        struct
            val triple: Triple
            val next_subject_predicate_list: TripleLookup
            val next_predicate_list: TripleLookup
            val next_object_predicate_list: TripleLookup
        end

      
    
    
  
    
