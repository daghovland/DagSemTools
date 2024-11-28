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
        
    (* Section 3.2.1 of https://www.w3.org/TR/owl2-mapping-to-rdf/#Mapping_from_RDF_Graphs_to_the_Structural_Specification*)
   
    (* This is for the set Decl from  the OWL2 specs, first part of table 7 in https://www.w3.org/TR/owl2-mapping-to-rdf/ *)
    let getBasicDeclarations owlClassId=
        tripleTable.GetTriplesWithObjectPredicate(owlClassId, rdfTypeId)
            |> Seq.map (_.subject)
   
   (* This is not per specification, but as a user it seems weird not to assume that only classes are combined with subClassOf *)
    let getSubClassClasses=
        tripleTable.GetTriplesWithPredicate(subClassPropId)
            |> Seq.map (fun tr -> [(tr.subject);(tr.obj)])
            |> Seq.concat
            |> Seq.choose (fun classId -> getResourceIri(classId) |> Option.map (fun classIri -> (classId, classIri)))
    
    
   (* This is for the set Decl from  the OWL2 specs, second part of table 7 in https://www.w3.org/TR/owl2-mapping-to-rdf/ *)
    let getAxiomDeclarations owlClassId   =
        tripleTable.GetTriplesWithObjectPredicate(owlClassId, owlAnnTargetId) 
                                    |> Seq.map (_.subject)
                                    |> Seq.filter (fun ax -> tripleTable.Contains({subject = ax; predicate = rdfTypeId; obj = owlAxiomId }) &&
                                                                 tripleTable.Contains({subject= ax; predicate = owlAnnPropId; obj = rdfTypeId}))
                                    |> Seq.collect (fun ax -> tripleTable.GetTriplesWithSubjectPredicate(ax, owlAnnSourceId)
                                                                |> Seq.map (_.obj))
    
    (* This is run to initialize Decl(G), and then CE, OPE, DPE, DR and AP as in Tables 7 and section 3.2.1 in the specification
       Note that the silent ignoring of blank nodes is per specifiation *)
    let getInitialDeclarations (typeIri : string)  =
        let typeId = resources.AddResource(Resource.Iri (new IriReference(typeIri)))
        Seq.concat [getBasicDeclarations typeId; getAxiomDeclarations typeId]
        |> Seq.choose (fun id -> getResourceIri id |> Option.map Iri.FullIri 
                                                |> Option.map (fun iri -> (id, iri)))
                                                
    (* CE *)
    let mutable ClassExpressions : Map<ResourceId, ClassExpression> =
        let mutable specDeclarations = (getInitialDeclarations Namespaces.OwlClass) |> Seq.map (fun (id, iri) ->  (id, ClassName iri)) |> Map.ofSeq
        getSubClassClasses
        |> Seq.fold (fun specDecls (classId, classIri) ->
            if not (specDecls.ContainsKey classId) then
                specDecls.Add(classId, ClassName (Iri.FullIri classIri))
            else
                specDecls
            ) specDeclarations
    
    (* DR *)
    let mutable DataRanges : Map<ResourceId, DataRange> =
        getInitialDeclarations Namespaces.RdfsDatatype
        |> Seq.map (fun (id, iri) ->  (id, NamedDataRange iri))
        |> Map.ofSeq
    (* OPE *)
    let mutable ObjectPropertyExpressions : Map<ResourceId, ObjectPropertyExpression> =
        let mutable firstMap = getInitialDeclarations Namespaces.OwlObjectProperty
                            |> Seq.map (fun (id, iri) ->  (id, NamedObjectProperty iri))
        let secondMap = tripleTable.GetTriplesWithPredicate(owlInvObjPropId)
                            |> Seq.choose (fun invTR -> getResourceIri(invTR.obj) |> Option.map (fun obj -> (invTR.subject, obj)))
                            |> Seq.map (fun (subj, obj) -> (subj, ObjectPropertyExpression.InverseObjectProperty (NamedObjectProperty (FullIri obj)) ))
        [firstMap ; secondMap] |> Seq.concat |> Map.ofSeq
    (* DPE *)
    let mutable DataPropertyExpressions : Map<ResourceId, DataProperty> =
        getInitialDeclarations Namespaces.OwlDatatypeProperty
        |> Seq.map (fun (id, iri) ->  (id, iri))
        |> Map.ofSeq
        
    (* AP *)
    let mutable AnnotationProperties : Map<ResourceId, AnnotationProperty> =
        getInitialDeclarations Namespaces.OwlAnnotationProperty
        |> Seq.map (fun (id, iri) ->  (id, iri))
        |> Map.ofSeq
    let mutable Individuals : Map<ResourceId, Individual> =
        getInitialDeclarations Namespaces.OwlNamedIndividual
        |> Seq.map (fun (id, iri) -> (id, NamedIndividual iri))
        |> Map.ofSeq
    
    (* ANN *)
    let mutable Annotations : Map<ResourceId, Annotation> =
        AnnotationProperties
        |> Map.toSeq
        |> Seq.map (fun (annPropId, annProp) -> tripleTable.GetTriplesWithPredicate(annPropId)
                                                |> Seq.map (fun annTr -> (annTr.subject, Ingress.createAnnotationValue Individuals annTr.obj (resources.GetResource(annTr.obj))))
                                                |> Seq.map (fun (annotatedObj, annVal) -> (annotatedObj, (Annotation (annProp, annVal)) )))
        |> Seq.concat
        |> Map.ofSeq
    
    let getResourceClass (resourceId : Ingress.ResourceId) : Class option =
        getResourceIri resourceId |> Option.map Class.FullIri

    
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
                                                                         | _ -> None))
   
    let tryGetDeclaration (declarationMap : Map<ResourceId, 'T>) resourceId =
        let resource = resources.GetResource(resourceId)
        match declarationMap.TryGetValue(resourceId) with
        | false, _ -> failwith $"Invalid OWL ontology. The resource {resource} used as a {declarationMap.Values.GetType()} without declaration."
        | true, decl -> decl
    
    (* This is an implementation of section 3.2.5 in https://www.w3.org/TR/owl2-mapping-to-rdf/#Analyzing_Declarations *)
    let extractAxiom   (triple : Ingress.Triple) : Axiom option =
        getResourceIri triple.predicate
        |> Option.map (_.ToString())
        |> Option.bind (fun (predicateIri) -> 
                        match predicateIri with
                            | Namespaces.RdfType -> getResourceIri(triple.obj)
                                                    |> Option.map (_.ToString())
                                                    |> Option.bind (fun classIri -> match classIri with
                                                                                    | Namespaces.OwlClass -> getResourceIri (triple.subject)
                                                                                                                |> Option.map FullIri
                                                                                                                |> Option.map ClassDeclaration
                                                                                                                |> Option.map (fun e -> Declaration ([],e))
                                                                                                                |> Option.map AxiomDeclaration
                                                                                    | Namespaces.RdfsDatatype -> getResourceIri (triple.subject)
                                                                                                                    |> Option.map FullIri
                                                                                                                    |> Option.map DatatypeDeclaration
                                                                                                                    |> Option.map (fun e -> Declaration ([],e))
                                                                                                                    |> Option.map AxiomDeclaration
                                                                                    | Namespaces.OwlObjectProperty -> getResourceIri (triple.subject)
                                                                                                                    |> Option.map FullIri
                                                                                                                    |> Option.map ObjectPropertyDeclaration
                                                                                                                    |> Option.map (fun e -> Declaration ([],e))
                                                                                                                    |> Option.map AxiomDeclaration
                                                                                    | Namespaces.OwlDatatypeProperty -> getResourceIri (triple.subject)
                                                                                                                        |> Option.map FullIri
                                                                                                                        |> Option.map DataPropertyDeclaration
                                                                                                                        |> Option.map (fun e -> Declaration ([],e))
                                                                                                                        |> Option.map AxiomDeclaration
                                                                                    | Namespaces.OwlAnnotationProperty -> getResourceIri (triple.subject)
                                                                                                                        |> Option.map FullIri
                                                                                                                        |> Option.map AnnotationPropertyDeclaration
                                                                                                                        |> Option.map (fun e -> Declaration ([],e))
                                                                                                                        |> Option.map AxiomDeclaration
                                                                                    | Namespaces.OwlNamedIndividual -> (match resources.GetResource(triple.subject) with
                                                                                                                                | Iri i -> NamedIndividual (FullIri i)
                                                                                                                                | AnonymousBlankNode bn -> AnonymousIndividual bn
                                                                                                                                | _ -> failwith "Invalid individual ")
                                                                                                                        |> NamedIndividualDeclaration |> (fun e -> Declaration ([],e)) |> AxiomDeclaration |> Some
                                                                                    | Namespaces.OwlAllDisjointClasses -> (match tripleTable.GetTriplesWithSubjectPredicate(triple.subject, owlMembersId) |> Seq.toList with
                                                                                                                           | [] -> None
                                                                                                                           | [disjointList] ->  ClassAxiom.DisjointClasses ([Annotations.[triple.subject]], disjointList.obj |> GetRdfListElements |> List.map (tryGetDeclaration ClassExpressions))
                                                                                                                                                    |> AxiomClassAxiom
                                                                                                                                                    |> Some
                                                                                                                           | _ -> failwith "Several owl:members triples detected on a owl:AllDisjointClasses axiom. This is not valid int owl")
                                                                                    | Namespaces.OwlAllDisjointProperties -> (match tripleTable.GetTriplesWithSubjectPredicate(triple.subject, owlMembersId) |> Seq.toList with
                                                                                                                               | [] -> None
                                                                                                                               | [disjointList] ->  DisjointObjectProperties ([Annotations.[triple.subject]], disjointList.obj |> GetRdfListElements |> List.map (tryGetDeclaration ObjectPropertyExpressions))
                                                                                                                                                        |> AxiomObjectPropertyAxiom
                                                                                                                                                        |> Some
                                                                                                                               | _ -> failwith "Several owl:members triples detected on a owl:AllDisjointClasses axiom. This is not valid int owl")
                                                                                    | _ -> None) 
                            | Namespaces.RdfsSubClassOf -> ClassAxiom.SubClassOf ([], tryGetDeclaration ClassExpressions triple.subject, tryGetDeclaration ClassExpressions triple.obj)
                                                           |> AxiomClassAxiom |> Some
                            | Namespaces.OwlEquivalentClass -> ClassAxiom.EquivalentClasses ([], [tryGetDeclaration ClassExpressions triple.subject; tryGetDeclaration ClassExpressions triple.obj])
                                                               |> AxiomClassAxiom |> Some
                            | Namespaces.OwlDisjointWith -> ClassAxiom.DisjointClasses ([], [tryGetDeclaration ClassExpressions triple.subject; tryGetDeclaration ClassExpressions triple.obj])
                                                                |> AxiomClassAxiom |> Some
                            | Namespaces.OwlDisjointUnionOf -> ClassAxiom.DisjointUnion ([], tryGetResourceClass triple.subject, triple.obj |> GetRdfListElements |> List.map (tryGetDeclaration ClassExpressions))
                                                               |> Axioms.AxiomClassAxiom |> Some
                            | Namespaces.RdfsSubPropertyOf -> SubObjectPropertyOf ([],
                                                                                   triple.subject |> tryGetDeclaration ObjectPropertyExpressions |> SubObjectPropertyExpression,
                                                                                   tryGetDeclaration ObjectPropertyExpressions triple.obj )
                                                                |> AxiomObjectPropertyAxiom |> Some
                            | Namespaces.OwlPropertyChainAxiom -> SubObjectPropertyOf([], triple.subject |> GetRdfListElements |> List.map (tryGetDeclaration ObjectPropertyExpressions) |> subPropertyExpression.PropertyExpressionChain, tryGetDeclaration ObjectPropertyExpressions triple.obj)
                                                                |> AxiomObjectPropertyAxiom |> Some
                            | Namespaces.OwlEquivalentProperty -> EquivalentObjectProperties([], [tryGetDeclaration ObjectPropertyExpressions triple.subject; tryGetDeclaration ObjectPropertyExpressions triple.obj])
                                                                |> AxiomObjectPropertyAxiom |> Some
                            | Namespaces.OwlPropertyDisjointWith -> DisjointObjectProperties([], [tryGetDeclaration ObjectPropertyExpressions triple.subject; tryGetDeclaration ObjectPropertyExpressions triple.obj])
                                                                    |> AxiomObjectPropertyAxiom |> Some
                            | Namespaces.RdfsDomain -> ObjectPropertyDomain (tryGetDeclaration ObjectPropertyExpressions triple.subject, tryGetDeclaration ClassExpressions triple.obj )
                                                        |> AxiomObjectPropertyAxiom |> Some
                            | Namespaces.RdfsRange -> ObjectPropertyRange (tryGetDeclaration ObjectPropertyExpressions triple.subject, tryGetDeclaration ClassExpressions triple.obj )
                                                        |> AxiomObjectPropertyAxiom |> Some
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
        let tripleAxioms = tripleTable.GetTriples() |> Seq.choose extractAxiom
        let RIND = getReificationBlankNodes 
        let axioms = [tripleAxioms  ] |> Seq.concat |> Seq.toList
        Ontology.Ontology (imports, oName, [], axioms)
    
