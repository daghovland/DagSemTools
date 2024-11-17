namespace DagSemTools.RdfOwlTranslator

open System.Resources
open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
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
    let createDeclaration (resource : ResourceManager) (declarationType) (resourceId : Ingress.ResourceId)  : Entity=
        let resourceIri = getResourceIri resource resourceId
        declarationType (Axioms.Iri.FullIri resourceIri.Value)
    
    (* This is the set RIND in Table 8 of https://www.w3.org/TR/owl2-mapping-to-rdf/ *)
    let getReificationBlankNodes (tripleTable : TripleTable) (resources : ResourceManager) =
        let rdfTypeId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfType)))
        [
            Namespaces.OwlAxiom
            Namespaces.OwlAnnotation
            Namespaces.OwlAllDisjointClasses
            Namespaces.OwlAllDisjointProperties
            Namespaces.OwlAllDifferent
            Namespaces.OwlNegativePropertyAssertion
        ]
            |> Seq.map (fun iri -> resources.AddResource(Resource.Iri (new IriReference(iri))))
            |> Seq.collect (fun typeId -> tripleTable.GetTriplesWithObjectPredicate(typeId, rdfTypeId)
                                          |> Seq.map _.subject
                                          |> Seq.choose (fun res -> match resources.GetResource(res) with
                                                                         | AnonymousBlankNode _ -> Some res
                                                                         | NamedBlankNode _ -> Some res
                                                                         | _ -> None))
   
    
    (* This is for the set Decl from  the OWL2 specs, first part of table 7 in https://www.w3.org/TR/owl2-mapping-to-rdf/ *)
    let getBasicDeclarations (tripleTable : TripleTable) (resources : ResourceManager) (typeDeclaration : Iri -> Entity) (entityTypeIri)=
        let rdfTypeId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfType)))
        let owlClassId = resources.AddResource(Resource.Iri (entityTypeIri))
        tripleTable.GetTriplesWithObjectPredicate(owlClassId, rdfTypeId)
            |> Seq.map (_.subject)
            |> Seq.map (createDeclaration resources typeDeclaration)
    
    
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
    
    let getTypeDeclarator entityTypeIri =
        match entityTypeIri.ToString() with
        | Namespaces.OwlDatatypeProperty -> DataPropertyDeclaration
        | Namespaces.OwlObjectProperty -> ObjectPropertyDeclaration
        | Namespaces.RdfsDatatype -> DatatypeDeclaration
        | Namespaces.OwlClass -> ClassDeclaration
        | Namespaces.OwlAnnotationProperty -> AnnotationPropertyDeclaration
        | Namespaces.OwlNamedIndividual -> (fun indIri -> NamedIndividualDeclaration (NamedIndividual indIri))
        | _ -> failwith $"BUG: no declaration for iri {entityTypeIri}"
    
    (* This is for the set Decl from  the OWL2 specs, second part of table 7 in https://www.w3.org/TR/owl2-mapping-to-rdf/ *)
    let getAxiomDeclarations (tripleTable : TripleTable) (resources : ResourceManager) : Axiom seq =
        let rdfTypeId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfType)))
        let owlAxiomId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlAxiom)))
        let owlAnnPropId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlAnnotatedProperty)))
        let owlAnnSourceId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlAnnotatedSource)))
        let owlAnnTargetId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlAnnotatedTarget)))
        
        tripleTable.GetTriplesWithObjectPredicate(owlAxiomId, rdfTypeId)
                                    |> Seq.map (_.subject)
                                    |> Seq.filter (fun ax -> tripleTable.Contains({subject= ax; predicate = owlAnnPropId; obj = rdfTypeId}))
                                    |> Seq.collect (fun ax -> tripleTable.GetTriplesWithSubjectPredicate(ax, owlAnnSourceId)
                                                                |> Seq.map _.obj
                                                                |> Seq.map (fun source -> (ax, source) )
                                                                |> Seq.collect (fun (ax, source) ->
                                                                    tripleTable.GetTriplesWithSubjectPredicate(ax, owlAnnTargetId)
                                                                    |> Seq.map (_.obj)
                                                                    |> Seq.choose (getResourceIri resources)
                                                                    |> Seq.map getTypeDeclarator
                                                                    |> Seq.map (fun decl -> (source, decl))))
                                    |> Seq.map (fun (source, decl) -> AxiomDeclaration ([], createDeclaration resources decl source ))
    
    let getDeclarations (tripleTable : TripleTable) (resources) =
        [
         Namespaces.OwlClass
         Namespaces.OwlDatatypeProperty
         Namespaces.OwlObjectProperty
         Namespaces.RdfsDatatype
         Namespaces.OwlAnnotationProperty
         Namespaces.OwlNamedIndividual
         ]
        |> Seq.map (fun typeIri -> (getTypeDeclarator typeIri, typeIri))
        |> Seq.collect (fun (typeInfo, typeIri) -> getBasicDeclarations tripleTable resources typeInfo (new IriReference(typeIri)))
        |> Seq.map (fun ent -> Axioms.AxiomDeclaration ([], ent))
        
    let extractOntology (tripleTable : TripleTable) (resources : ResourceManager) =
        let (oName, imports) = extractOntologyName tripleTable resources
        let declarations = getDeclarations tripleTable resources
        let axiomDeclarations = getAxiomDeclarations tripleTable resources
        let tripleAxioms = tripleTable.GetTriples() |> Seq.choose (extractAxiom resources)
        let axioms = [declarations ; tripleAxioms ; axiomDeclarations] |> Seq.concat |> Seq.toList
        Ontology.Ontology (imports, oName, [], axioms)