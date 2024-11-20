namespace DagSemTools.RdfOwlTranslator

open System.Resources
open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.Resource
open IriTools
open OwlOntology
open OwlOntology.Axioms
open OwlOntology.Ontology

type Rdf2Owl (triples : TripleTable,
              resourceManager : ResourceManager) =
    
    let tripleTable = triples
    let resources = resourceManager
    
    let rdfTypeId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfType)))
    let owlOntologyId = resources.AddResource(Resource.Iri(new IriReference(Namespaces.OwlOntology)))
    let versionPropId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlVersionIri)))
    let importsPropId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlImport)))
    let owlAxiomId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlAxiom)))
    let owlAnnPropId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlAnnotatedProperty)))
    let owlAnnSourceId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlAnnotatedSource)))
    let owlAnnTargetId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.OwlAnnotatedTarget)))
        
    (* Section 3.2.1 of https://www.w3.org/TR/owl2-mapping-to-rdf/#Mapping_from_RDF_Graphs_to_the_Structural_Specification*)
    
    (* This is for the set Decl from  the OWL2 specs, second part of table 7 in https://www.w3.org/TR/owl2-mapping-to-rdf/ *)
    let getAxiomDeclarations (axiomTypeIri : string) : ResourceId seq  =
        let axiomTypeId = resources.AddResource(Resource.Iri (new IriReference(axiomTypeIri)))
        let decl = Ingress.getTypeDeclarator axiomTypeIri
        tripleTable.GetTriplesWithObjectPredicate(axiomTypeId, owlAnnTargetId) 
                                    |> Seq.map (_.subject)
                                    |> Seq.filter (fun ax -> tripleTable.Contains({subject = ax; predicate = rdfTypeId; obj = owlAxiomId }) &&
                                                                 tripleTable.Contains({subject= ax; predicate = owlAnnPropId; obj = rdfTypeId}))
                                    |> Seq.collect (fun ax -> tripleTable.GetTriplesWithSubjectPredicate(ax, owlAnnSourceId)
                                                                |> Seq.map (_.obj))
                                    
    
    (* This is for the set Decl from  the OWL2 specs, first part of table 7 in https://www.w3.org/TR/owl2-mapping-to-rdf/ *)
    let getBasicDeclarations (typeDeclaration : Iri -> Entity) (entityTypeIri)=
        let owlClassId = resources.AddResource(Resource.Iri (entityTypeIri))
        tripleTable.GetTriplesWithObjectPredicate(owlClassId, rdfTypeId)
            |> Seq.map (_.subject)
   
    let getSimpleDeclarations (typeIri : string) : ResourceId seq=
        let typeInfo = Ingress.getTypeDeclarator typeIri
        (getBasicDeclarations typeInfo (new IriReference(typeIri)))
        
    let getResourceIri (resourceId : Ingress.ResourceId) : IriReference option =
        match resources.GetResource(resourceId) with 
            | Resource.Iri iri -> Some iri
            | _ -> None
    
    (* This is run to initialize Decl(G), and then CE, OPE, DPE, DR and AP as in Tables 7 and section 3.2.1 in the specification
       Note that the silent ignoring of blank nodes is per specifiation *)
    let getInitialDeclarations (typeIri : string) : Iri seq =
        Seq.concat [getSimpleDeclarations typeIri; getAxiomDeclarations typeIri]
        |> Seq.choose getResourceIri
        |> Seq.map Iri.FullIri
        
    (* CE *)
    let mutable ClassExpressions : Map<ResourceId, ClassExpression> =
        getInitialDeclarations Namespaces.OwlClass
        |> Seq.map (fun res -> (res, ClassName res))
        |> Map.ofSeq
                    
    (* DR *)
    let mutable DataRanges = Map<ResourceId, DataRange>
    (* OPE *)
    let mutable ObjectPropertyExpressions = Map<ResourceId, ObjectPropertyExpression>
    (* DPE *)
    let mutable DstaPropertyExpressions = Map<ResourceId, DataProperty>
    (* AP *)
    let mutable AnnotationProperties = Map<ResourceId, AnnotationProperty>
    
    let getResourceClass (resourceId : Ingress.ResourceId) : Class option =
        getResourceIri resourceId |> Option.map Class.FullIri 
    (* Creates entities for use in declaration axioms  *)
    let createDeclaration (declarationType) (resourceId : Ingress.ResourceId)  : Entity =
        let resourceIri = getResourceIri resourceId
        declarationType (Axioms.Iri.FullIri resourceIri.Value)
    
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
                                                                         | NamedBlankNode _ -> Some res
                                                                         | _ -> None))
   
    
    
    
    let extractAxiom   (triple : Ingress.Triple) : Axiom option =
        getResourceIri triple.predicate
        |> Option.map (_.ToString())
        |> Option.bind (fun (predicateIri) -> 
                        match predicateIri with
                            | Namespaces.RdfsSubClassOf -> Option.map2 Ingress.createSubClassAxiom  (getResourceClass (triple.subject)) (getResourceClass (triple.obj))
                                                            |> Option.map Axioms.AxiomClassAxiom
                            | _ -> None)
        
    
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
        es |> Seq.map (fun ent -> Axioms.AxiomDeclaration ([], ent))
    member this.extractOntology  =
        let (oName, imports) = extractOntologyName 
        let declarations = getDeclarations tripleTable resources
        let tripleAxioms = tripleTable.GetTriples() |> Seq.choose extractAxiom
        let RIND = getReificationBlankNodes 
        let axioms = [tripleAxioms ; (declarations |> Map.values |> Seq.concat ) ] |> Seq.concat |> Seq.toList
        Ontology.Ontology (imports, oName, [], axioms)
    
