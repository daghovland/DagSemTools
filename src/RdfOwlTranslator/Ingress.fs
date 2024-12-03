namespace DagSemTools.RdfOwlTranslator

open DagSemTools.Rdf.Ingress
open DagSemTools.Resource
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