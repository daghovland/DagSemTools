namespace DagSemTools.RdfOwlTranslator

open System.Resources
open DagSemTools.Rdf
open DagSemTools.Resource
open IriTools
open OwlOntology
open OwlOntology.Axioms
open OwlOntology.Ontology

module Rdf2Owl =
    let getResourceIri (resource : ResourceManager) (resourceId : Ingress.ResourceId) : IriReference option =
        match resource.GetResource(resourceId) with 
            | Resource.Iri iri -> Some iri
            | _ -> None
    
    let getResourceClass (resource : ResourceManager) (resourceId : Ingress.ResourceId) : OwlOntology.Axioms.Class option =
        getResourceIri resource resourceId |> Option.map OwlOntology.Axioms.Class.FullIri 
    
    let createSubClassAxiom subclass superclass = 
        OwlOntology.Axioms.ClassAxiom.SubClassOf ([], (Axioms.ClassName subclass), (Axioms.ClassName superclass))
    
    let createClassDeclaration (resource : ResourceManager) (tripleTable : TripleTable) (classId : Ingress.ResourceId) =
        let classTriples = tripleTable.GetTriplesWithSubject(classId)
        Axioms.ClassDeclaration
    
    let extractAxiom (resource : ResourceManager)  (triple : Ingress.Triple) : OwlOntology.Axioms.Axiom option =
        getResourceIri resource triple.predicate
        |> Option.map (_.ToString())
        |> Option.bind (fun (predicateIri) -> 
                        match predicateIri with
                            | Namespaces.RdfsSubClassOf -> Option.map2 createSubClassAxiom  (getResourceClass resource (triple.subject)) (getResourceClass resource (triple.obj))
                                                            |> Option.map Axioms.AxiomClassAxiom
                            | _ -> None)
        
    let extractOntologyIri (tripleTable : TripleTable) (resources : ResourceManager) =
        let rdfTypeId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfType)))
        let owlOntologyId = resources.AddResource(Resource.Iri(new IriReference(Namespaces.OwlOntology)))
        let ontologyTypeTriples = tripleTable.GetTriplesWithObjectPredicate(owlOntologyId, rdfTypeId)
        if (ontologyTypeTriples |> Seq.length > 1) then
            failwith "Multiple ontology IRIs provided in file!"
        else
            ontologyTypeTriples |> Seq.tryHead |> Option.bind (fun tr -> (getResourceIri resources tr.subject)) 
    
    let extractOntologyVersionIri (tripleTable : TripleTable) (resources : ResourceManager) (ontologyIri : IriReference) =
        let ontologyIriId = resources.ResourceMap.[Resource.Iri ontologyIri]
        let versionPropId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlVersionIri)))
        let ontologyVersionTriples = tripleTable.GetTriplesWithSubjectPredicate(ontologyIriId, versionPropId)
        if (ontologyVersionTriples |> Seq.length > 1) then
            failwith "Multiple ontology version IRIs provided in file!"
        else
            ontologyVersionTriples |> Seq.tryHead |> Option.bind (fun tr -> (getResourceIri resources tr.subject)) 
     
    let extractOntologyImports (tripleTable : TripleTable) (resources : ResourceManager) (ontologyIri : IriReference) =
        let ontologyIriId = resources.ResourceMap.[Resource.Iri ontologyIri]
        let importsPropId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlImport)))
        let ontologyImportTriples = tripleTable.GetTriplesWithSubjectPredicate(ontologyIriId, importsPropId)
        ontologyImportTriples |> Seq.choose (fun tr -> (getResourceIri resources tr.obj)) 
     
    let extractOntologyName (tripleTable : TripleTable) (resources : ResourceManager)  =
        match extractOntologyIri tripleTable resources with
        | None -> (UnNamedOntology, [])
        | Some iri ->
                        let imports = extractOntologyImports tripleTable resources iri |> Seq.toList
                        let version = match extractOntologyVersionIri tripleTable resources iri with
                                        | None -> NamedOntology iri
                                        | Some versionIri -> VersionedOntology (iri, versionIri)
                        (version, imports)
       
    let extractOntology (tripleTable : TripleTable) (resources : ResourceManager) =
        let (oName, imports) = extractOntologyName tripleTable resources
        let axioms = tripleTable.GetTriples() |> Seq.choose (extractAxiom resources)  |> Seq.toList
        Ontology.Ontology (imports, oName, [], axioms)