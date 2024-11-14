namespace DagSemTools.RdfOwlTranslator

open System.Resources
open DagSemTools.Rdf
open DagSemTools.Resource
open IriTools
open OwlOntology

module Rdf2Owl =
    let getResourceIri (resource : ResourceManager) (resourceId : Ingress.ResourceId) : IriReference option =
        match resource.GetResource(resourceId) with 
            | Resource.Iri iri -> Some iri
            | _ -> None
    
    let getResourceClass (resource : ResourceManager) (resourceId : Ingress.ResourceId) : OwlOntology.Axioms.Class option =
        getResourceIri resource resourceId |> Option.map OwlOntology.Axioms.Class.FullIri 
    
    let createSubClassAxiom subclass superclass = 
        OwlOntology.Axioms.ClassAxiom.SubClassOf ([], (Axioms.ClassName subclass), (Axioms.ClassName superclass))
    
    let extractAxiom (resource : ResourceManager)  (triple : Ingress.Triple) : OwlOntology.Axioms.Axiom option =
        getResourceIri resource triple.predicate
        |> Option.map (_.ToString())
        |> Option.bind (fun (predicateIri) -> 
                        match predicateIri with
                            | Namespaces.RdfsSubClassOf -> Option.map2 createSubClassAxiom  (getResourceClass resource (triple.subject)) (getResourceClass resource (triple.obj))
                                                            |> Option.map Axioms.AxiomClassAxiom
                            | _ -> None)
                                                   
    
    let extractOntology (tripleTable : TripleTable) (resources : ResourceManager) =
        let axioms = tripleTable.GetTriples() |> Seq.choose (extractAxiom resources)  |> Seq.toList
        OwlOntology.Ontology.Ontology ([], Ontology.UnNamedOntology, [], axioms)