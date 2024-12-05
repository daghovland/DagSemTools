(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Rdf

open DagSemTools.Ingress

module Ingress =
    type ResourceId = uint32
    type TripleListIndex = uint
    type QuadListIndex = uint
        
    [<Struct>]
    [<StructuralEquality>]
    [<NoComparison>]
    type Triple = {
            subject: ResourceId
            predicate: ResourceId
            obj: ResourceId
        }
        
    [<Struct>]
    [<StructuralEquality>]
    [<NoComparison>]
    type Quad = {
            tripleId: ResourceId
            subject: ResourceId
            predicate: ResourceId
            obj: ResourceId
        } with
        override this.ToString() =
            sprintf "%A: (%A, %A, %A)"
                this.tripleId
                this.subject 
                this.predicate 
                this.obj
    
    
    [<Struct>]
    [<StructuralEquality>]
    [<NoComparison>]
    type TripleResource = {
            subject: Resource
            predicate: Resource
            obj: Resource
    } with
        override this.ToString() =
            sprintf "(%A, %A, %A)" 
                this.subject 
                this.predicate 
                this.obj
    
    
      
    let doubleArraySize (originalArray: 'T array) : 'T array =
        let newSize = originalArray.Length * 2
        let newArray = Array.zeroCreate<'T> newSize
        Array.blit originalArray 0 newArray 0 originalArray.Length
        newArray    
    
    (* Assumes the resource is some integer literal, and extracts it if that is the cases *)
    let tryGetNonNegativeIntegerLiteral res =
        match res with
                    | Resource.IntegerLiteral nn -> Some nn
                    | Resource.TypedLiteral (tp, nn) when (List.contains (tp.ToString()) [Namespaces.XsdInt ; Namespaces.XsdInteger; Namespaces.XsdNonNegativeInteger] ) -> nn |> int |> Some                              
                    | x -> None
    
    (* Assumes the resource is some integer literal, and extracts it if that is the cases *)
    let tryGetBoolLiteral res =
        match res with
                    | Resource.BooleanLiteral nn -> Some nn
                    | Resource.TypedLiteral (tp, nn) when (tp.ToString() = Namespaces.XsdBoolean) -> Some (match nn with
                                                                                                           | "true" -> true
                                                                                                           | "false" -> false
                                                                                                           | x -> failwith $"Invalid use of xsd:boolean on value {x}")
                    | _ -> None
    
  
    
