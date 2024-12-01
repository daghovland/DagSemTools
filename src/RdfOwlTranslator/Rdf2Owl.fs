namespace DagSemTools.RdfOwlTranslator

open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.Resource
open DagSemTools.OwlOntology
open DagSemTools.Resource.Namespaces
open IriTools

type Rdf2Owl (triples : TripleTable,
              resourceManager : ResourceManager) =
    
    let tripleTable = triples
    let resources = resourceManager
    
    let rdfTypeId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfType)))
    let owlOntologyId = resources.AddResource(Resource.Iri(new IriReference(Namespaces.OwlOntology)))
    let versionPropId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlVersionIri)))
    let importsPropId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlImport)))
    let owlClassId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlClass)))
    let owlRestrictionId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlRestriction)))
    let owlOnPropertyId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlOnProperty)))
    let owlOnClassId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlOnClass)))
    let owlQualifiedCardinalityId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlQualifiedCardinality)))
    let owlAxiomId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlAxiom)))
    let owlMembersId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlMembers)))
    let owlAnnPropId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlAnnotatedProperty)))
    let owlAnnSourceId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlAnnotatedSource)))
    let owlAnnTargetId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlAnnotatedTarget)))
    let owlInvObjPropId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlObjectInverseOf)))
    let subClassPropId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfsSubClassOf)))
    let rdfNilId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfNil)))
    let rdfFirstId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfFirst)))
    let rdfRestId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfRest)))
    let getResourceIri (resourceId : Ingress.ResourceId) : IriReference option =
        match resources.GetResource(resourceId) with 
            | Resource.Iri iri -> Some iri
            | _ -> None
        
    let getResourceClass (resourceId : Ingress.ResourceId) : Class option =
        getResourceIri resourceId |> Option.map Class.FullIri

    let tryGetResourceIri resourceId =
        match getResourceIri resourceId with
        | Some c -> c
        | None -> failwith $"Invalid resource {resourceId} used compulsory IRI position"

    let tryGetResourceClass resourceId =
        match getResourceClass resourceId with
        | Some c -> c
        | None -> failwith $"Invalid resource {resourceId} used on class position"

    (* Extracts an RDF list. See Table 3 in https://www.w3.org/TR/owl2-mapping-to-rdf/
        Assumes head is the head of some rdf list in the triple-table
        The requirements in the specs includes non-circular lists, so blindly assumes this is true
     *)
    let rec _GetRdfListElements listId acc : ResourceId list =
        if (listId = rdfNilId) then
            acc
        else
            let head = match tripleTable.GetTriplesWithSubjectPredicate(listId, rdfFirstId) |> Seq.toList with
                        | [] -> failwith $"Invalid list defined at {resources.GetResource(listId)}"
                        | [headElement] -> headElement.obj
                        | _ -> failwith $"Invalid list defined at {resources.GetResource(listId)}"
            if (Seq.contains head acc) then
                failwith $"Invalid list defined at {resources.GetResource(listId)}"
            else
                let rest = match tripleTable.GetTriplesWithSubjectPredicate(listId, rdfFirstId) |> Seq.toList with
                            | [] -> failwith $"Invalid list defined at {resources.GetResource(listId)}"
                            | [headElement] -> headElement.obj
                            | _ -> failwith $"Invalid list defined at {resources.GetResource(listId)}"
                _GetRdfListElements rest (head :: acc)     
    let GetRdfListElements listId=
        _GetRdfListElements listId []    
    (* Creates entities for use in declaration axioms  *)
    let createDeclaration (declarationType) (resourceId : Ingress.ResourceId)  : Entity =
        let resourceIri = getResourceIri resourceId
        declarationType (Iri.FullIri resourceIri.Value)
    
    (* This is the set RIND in Table 8 of https://www.w3.org/TR/owl2-mapping-to-rdf/ *)
    let getReificationBlankNodes =
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
                                                                         | _ -> None))
   
    
    let extractOntologyVersionIri (ontologyIri : IriReference) =
        let ontologyIriId = resources.ResourceMap.[Resource.Iri ontologyIri]
        let ontologyVersionTriples = tripleTable.GetTriplesWithSubjectPredicate(ontologyIriId, versionPropId)
        if (ontologyVersionTriples |> Seq.length > 1) then
            failwith "Multiple ontology version IRIs provided in file!"
        else
            ontologyVersionTriples |> Seq.tryHead |> Option.bind (fun tr -> (getResourceIri tr.subject)) 
     
    let extractOntologyImports  (ontologyIri : IriReference) =
        let ontologyIriId = resources.ResourceMap.[Resource.Iri ontologyIri]
        let ontologyImportTriples = tripleTable.GetTriplesWithSubjectPredicate(ontologyIriId, importsPropId)
        ontologyImportTriples |> Seq.choose (fun tr -> (getResourceIri tr.obj)) 
    
    let extractOntologyIri =
        let ontologyTypeTriples = tripleTable.GetTriplesWithObjectPredicate(owlOntologyId, rdfTypeId)
        if (ontologyTypeTriples |> Seq.length > 1) then
            failwith "Multiple ontology IRIs provided in file!"
        else
            ontologyTypeTriples |> Seq.tryHead |> Option.bind (fun tr -> (getResourceIri tr.subject)) 
    
     
    let extractOntologyName  =
        match extractOntologyIri with
        | None -> (UnNamedOntology, [])
        | Some iri ->
                        let imports = extractOntologyImports iri |> Seq.toList
                        let version = match extractOntologyVersionIri  iri with
                                        | None -> NamedOntology iri
                                        | Some versionIri -> VersionedOntology (iri, versionIri)
                        (version, imports)
   
    let getEntityDeclarations (es : Entity seq) : Axiom seq =
        es |> Seq.map (fun ent -> AxiomDeclaration ([], ent))
    
    member this.extractOntology  =
        let (oName, imports) = extractOntologyName 
        let RIND = getReificationBlankNodes
        let classExpressionParser = new ClassExpressionParser(tripleTable, resources)
        let axiomParser = classExpressionParser.getAxiomParser()
        let tripleAxioms = tripleTable.GetTriples() |> Seq.choose axiomParser.extractAxiom
        let axioms = [tripleAxioms  ] |> Seq.concat |> Seq.toList
        Ontology (imports, oName, [], axioms)
    
