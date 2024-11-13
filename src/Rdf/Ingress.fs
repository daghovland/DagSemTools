(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Rdf

open IriTools
open DagSemTools.Resource

module Ingress =
    type ResourceId = uint32
    type TripleListIndex = uint
    type QuadListIndex = uint
        
    [<Struct>]
    type Triple = {
            subject: ResourceId
            predicate: ResourceId
            obj: ResourceId
        }
        
    [<Struct>]
    type Quad = {
            tripleId: ResourceId
            subject: ResourceId
            predicate: ResourceId
            obj: ResourceId
        }
    
    [<Struct>]
    type TripleResource = {
            subject: Resource
            predicate: Resource
            obj: Resource
    }
    
      
    let doubleArraySize (originalArray: 'T array) : 'T array =
        let newSize = originalArray.Length * 2
        let newArray = Array.zeroCreate<'T> newSize
        Array.blit originalArray 0 newArray 0 originalArray.Length
        newArray    
    
    
  
    
