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
        | LiteralString of literal: string
        | IntegerLiteral of literalInt: int
        | DecimalLiteral of literalDec: decimal
        | DoubleLiteral of literalDouble: double
        | BooleanLiteral of literalBool: bool
        | DateTimeLiteral of literalDateTime: DateTime
        | LangLiteral of lang: string * langliteral: string
        | TypedLiteral of typeIri: IriReference * typedLiteral: string
    type TripleListIndex = int
    [<Struct>]
    type TripleListLink =
        | ArrayIndex of index: TripleListIndex
        | End
    [<Struct>]
    type Triple = {
            subject: ResourceId
            predicate: ResourceId
            object: ResourceId
        }
    [<Struct>]
    type TripleListEntry = {
            triple: Triple
            next_subject_predicate_list: TripleListLink
            next_predicate_list: TripleListLink
            next_object_predicate_list: TripleListLink
        }

      
    
    
  
    
