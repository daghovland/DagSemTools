(*
 Copyright (C) 2024 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.RdfOwlTranslator

open DagSemTools.Rdf.Ingress
open DagSemTools.Ingress
open DagSemTools.OwlOntology

module Ingress =
    
    let createSubClassAxiom subclass superclass = 
        ClassAxiom.SubClassOf ([], (ClassName subclass), (ClassName superclass))
    
    let createAnnotationValue (individuals : Map<ResourceId, Individual>) (resId : ResourceId) (res: Resource) =
        match individuals.TryGetValue resId with
        | true, individual -> match res with 
                                | Iri i -> AnnotationValue.IndividualAnnotation (NamedIndividual (FullIri i))
                                | AnonymousBlankNode bn -> AnnotationValue.IndividualAnnotation (Individual.AnonymousIndividual bn)
                                | _ -> failwith "Only IRIs and blank nodes are valid individuals. This is a bug"
        | false, _ -> match res with
                                | Iri i -> AnnotationValue.IriAnnotation (FullIri i)
                                | AnonymousBlankNode bn -> failwith "Annotations with blank nodes that are not individuals is not allowed"
                                | _ -> AnnotationValue.LiteralAnnotation res 
                                
    let tryGetIndividual res = 
        match res with
        | Resource.Iri iri -> NamedIndividual (FullIri iri)
        | Resource.AnonymousBlankNode bn -> AnonymousIndividual bn
        | x -> failwith $"Invalid OWL Ontology: {x} attempted used as an individual. Only IRIs and blank nodes can be individuals"