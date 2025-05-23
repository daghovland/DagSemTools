﻿(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Ingress

open System
open System.Numerics
open IriTools

    
    [<StructuralComparison>]
    [<StructuralEquality>]
    [<Struct>]
    type public RdfResource =
        Iri of iri:  IriReference
        | AnonymousBlankNode of anon_blankNode: uint32
        override this.ToString() =
                match this with
                | Iri iri -> if iri = (IriReference Namespaces.RdfType) then "a" else $"<%A{iri}>"
                | AnonymousBlankNode anon_blankNode -> $"_:(%u{anon_blankNode})"
            
    [<StructuralComparison>]
    [<StructuralEquality>]
    [<Struct>]
    type public RdfLiteral =
        | LiteralString of literal: string
        | BooleanLiteral of literalBool: bool
        | DecimalLiteral of literalDec: decimal
        | FloatLiteral of literalFloat: float
        | DoubleLiteral of literalDouble: double
        | DurationLiteral of literalDuration: TimeSpan
        | IntegerLiteral of literalInt: BigInteger
        | DateTimeLiteral of literalDateTime: DateTime
        | TimeLiteral of literalTime: TimeOnly
        | DateLiteral of literalDate: DateOnly
        | LangLiteral of lang: string * langliteral: string
        | TypedLiteral of typeIri: IriReference * typedLiteral: string
        override this.ToString() =
                match this with
                | LiteralString literal -> $"(%s{literal})"
                | BooleanLiteral literalBool -> match literalBool with
                                                    | true -> $"(true)"
                                                    | false -> $"(false)"
                | DecimalLiteral literalDec -> $"DecimalLiteral(%M{literalDec})"
                | FloatLiteral literalFloat -> $"FloatLiteral(%f{literalFloat})"
                | DoubleLiteral literalDouble -> $"DoubleLiteral(%f{literalDouble})"
                | DurationLiteral literalDuration -> $"DurationLiteral(%A{literalDuration})"
                | IntegerLiteral literalInt -> $"IntegerLiteral({literalInt})"
                | DateTimeLiteral literalDateTime -> $"DateTimeLiteral(%A{literalDateTime})"
                | TimeLiteral literalTime -> $"TimeLiteral(%A{literalTime})"
                | DateLiteral literalDate -> $"DateLiteral(%A{literalDate})"
                | LangLiteral (lang, langliteral) -> $"%s{lang}@%s{langliteral})"
                | TypedLiteral (typeIri, typedLiteral) -> $"%s{typedLiteral}^^%A{typeIri}"
                
            
    [<StructuralComparison>]
    [<StructuralEquality>]
    [<Struct>]
    type public GraphElement =
        | NodeOrEdge of resource: RdfResource
        | GraphLiteral of literal: RdfLiteral
            override this.ToString() =
                match this with
                | NodeOrEdge r -> r.ToString()
                | GraphLiteral l -> l.ToString()
            
                
    type prefixDeclaration =
        | PrefixDefinition of PrefixName: string * PrefixIri: IriReference
    type prefixDeclaration with
        member x.TryGetPrefixName() =
            match x with
            | PrefixDefinition (name, iri) -> (name, iri)
    
    
    type ontologyVersion =
        | UnNamedOntology
        | NamedOntology of OntologyIri: IriReference
        | VersionedOntology of OntologyIri: IriReference * OntologyVersionIri: IriReference
    type ontologyVersion with
        member x.TryGetOntologyVersionIri() =
            match x with
            | NamedOntology iri -> null
            | VersionedOntology (_, iri) -> iri
            | _ -> null
        member x.TryGetOntologyIri() =
            match x with
            | NamedOntology iri -> iri
            | VersionedOntology (iri, _) -> iri
            | _ -> null
    