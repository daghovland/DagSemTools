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
    
    let getResourceClass (resource : ResourceManager) (resourceId : Ingress.ResourceId) : Class option =
        getResourceIri resource resourceId |> Option.map Class.FullIri 
    
    let createSubClassAxiom subclass superclass = 
        ClassAxiom.SubClassOf ([], (ClassName subclass), (ClassName superclass))
    
    (* Creates entities for use in declaration axioms  *)
    let createClassDeclaration (resource : ResourceManager) (tripleTable : TripleTable) (classId : Ingress.ResourceId) =
        let classIri = getResourceIri resource classId
        ClassDeclaration (Class.FullIri classIri.Value)
    let createDatatypeDeclaration (resource : ResourceManager) (tripleTable : TripleTable) (typeId : Ingress.ResourceId)  =
        let typeIri = getResourceIri resource typeId
        DatatypeDeclaration (Axioms.Iri.FullIri typeIri.Value)
    let createDatatypePropertyDeclaration (resource : ResourceManager) (tripleTable : TripleTable) (propertyId : Ingress.ResourceId)  =
        let propertyIri = getResourceIri resource propertyId
        DataPropertyDeclaration (Axioms.Iri.FullIri propertyIri.Value)
    let createObjectPropertyDeclaration (resource : ResourceManager) (tripleTable : TripleTable) (propertyId : Ingress.ResourceId)  =
        let propertyIri = getResourceIri resource propertyId
        ObjectPropertyDeclaration (Axioms.Iri.FullIri propertyIri.Value)
    let extractAxiom (resource : ResourceManager)  (triple : Ingress.Triple) : Axiom option =
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
    
    let getBasicClassDeclarations (tripleTable : TripleTable) (resources : ResourceManager) =
        let rdfTypeId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfType)))
        let owlClassId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlClass)))
        tripleTable.GetTriplesWithObjectPredicate(owlClassId, rdfTypeId)
            |> Seq.map (_.subject)
            |> Seq.map (createClassDeclaration resources tripleTable)
    
    let getBasicDatatypeDeclarations (tripleTable : TripleTable) (resources : ResourceManager) =
        let rdfTypeId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfType)))
        let owlClassId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfsDatatype)))
        tripleTable.GetTriplesWithObjectPredicate(owlClassId, rdfTypeId)
            |> Seq.map (_.subject)
            |> Seq.map (createDatatypeDeclaration resources tripleTable)
    let getBasicDatatypePropertyDeclarations (tripleTable : TripleTable) (resources : ResourceManager) =
        let rdfTypeId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfType)))
        let owlClassId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlDatatypeProperty)))
        tripleTable.GetTriplesWithObjectPredicate(owlClassId, rdfTypeId)
            |> Seq.map (_.subject)
            |> Seq.map (createDatatypePropertyDeclaration resources tripleTable)
    let getBasicObjectPropertyDeclarations (tripleTable : TripleTable) (resources : ResourceManager) =
        let rdfTypeId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfType)))
        let owlClassId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlObjectProperty)))
        tripleTable.GetTriplesWithObjectPredicate(owlClassId, rdfTypeId)
            |> Seq.map (_.subject)
            |> Seq.map (createObjectPropertyDeclaration resources tripleTable)
    
    
    (* This is the set Decl from  the OWL2 specs, table 7 in https://www.w3.org/TR/owl2-mapping-to-rdf/ *)
    let getDeclarations (tripleTable : TripleTable) (resources) =
        Seq.concat [
          getBasicClassDeclarations tripleTable resources
          getBasicDatatypeDeclarations tripleTable resources
          getBasicObjectPropertyDeclarations tripleTable resources
          getBasicDatatypePropertyDeclarations tripleTable resources
        ] |>
        Seq.map (fun ent -> Axioms.AxiomDeclaration ([], ent))
        
    let extractOntology (tripleTable : TripleTable) (resources : ResourceManager) =
        let (oName, imports) = extractOntologyName tripleTable resources
        let declarations = getDeclarations tripleTable resources
        let tripleAxioms = tripleTable.GetTriples() |> Seq.choose (extractAxiom resources)
        let axioms = [declarations ; tripleAxioms] |> Seq.concat |> Seq.toList
        Ontology.Ontology (imports, oName, [], axioms)