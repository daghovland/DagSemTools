(*
    Copyright (C) 2024 Dag Hovland
    
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Rdf

open System
open DagSemTools.AlcTableau
open DagSemTools.AlcTableau.ALC
open IriTools

module Ingress =
    type ResourceId = uint32
    
    [<StructuralComparison>]
    [<StructuralEquality>]
    [<Struct>]
    type public Resource =
        public Iri of iri:  IriReference
        | NamedBlankNode of blankNode: string
        | AnonymousBlankNode of anon_blankNode: uint32
        | LiteralString of literal: string
        | BooleanLiteral of literalBool: bool
        | DecimalLiteral of literalDec: decimal
        | FloatLiteral of literalFloat: float
        | DoubleLiteral of literalDouble: double
        | DurationLiteral of literalDuration: TimeSpan
        | IntegerLiteral of literalInt: int
        | DateTimeLiteral of literalDateTime: DateTime
        | TimeLiteral of literalTime: TimeOnly
        | DateLiteral of literalDate: DateOnly
        | LangLiteral of lang: string * langliteral: string
        | TypedLiteral of typeIri: IriReference * typedLiteral: string
        | DLTranslatedConceptName of concept: IriReference
        | DLTranslatedExistential of role: IriReference * filler: IriReference
            override this.ToString() =
                match this with
                | Iri iri -> $"<(%A{iri})>"
                | NamedBlankNode blankNode -> $"_:(%s{blankNode})"
                | AnonymousBlankNode anon_blankNode -> $"_:(%u{anon_blankNode})"
                | LiteralString literal -> $"(%s{literal})"
                | BooleanLiteral literalBool -> match literalBool with
                                                    | true -> $"(true)"
                                                    | false -> $"(false)"
                | DecimalLiteral literalDec -> $"DecimalLiteral(%M{literalDec})"
                | FloatLiteral literalFloat -> $"FloatLiteral(%f{literalFloat})"
                | DoubleLiteral literalDouble -> $"DoubleLiteral(%f{literalDouble})"
                | DurationLiteral literalDuration -> $"DurationLiteral(%A{literalDuration})"
                | IntegerLiteral literalInt -> $"IntegerLiteral(%d{literalInt})"
                | DateTimeLiteral literalDateTime -> $"DateTimeLiteral(%A{literalDateTime})"
                | TimeLiteral literalTime -> $"TimeLiteral(%A{literalTime})"
                | DateLiteral literalDate -> $"DateLiteral(%A{literalDate})"
                | LangLiteral (lang, langliteral) -> $"%s{lang}@%s{langliteral})"
                | TypedLiteral (typeIri, typedLiteral) -> $"%s{typedLiteral}^^%A{typeIri}"
                | DLTranslatedConceptName concept -> $"Internal Representation for DL reasoning of (%A{concept})"
                | DLTranslatedExistential (role, filler) -> $"Internal Representation for DL reasoning of (%A{role} some %A{filler})"
                
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
    
    type prefixDeclaration =
        | PrefixDefinition of PrefixName: string * PrefixIri: IriReference
    type prefixDeclaration with
        member x.TryGetPrefixName() =
            match x with
            | PrefixDefinition (name, iri) -> (name, iri)
      
    let doubleArraySize (originalArray: 'T array) : 'T array =
        let newSize = originalArray.Length * 2
        let newArray = Array.zeroCreate<'T> newSize
        Array.blit originalArray 0 newArray 0 originalArray.Length
        newArray    
    
    
  
    
