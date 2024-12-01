namespace DagSemTools.RdfOwlTranslator

open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.Resource
open DagSemTools.OwlOntology
open DagSemTools.Resource.Namespaces
open IriTools

type AxiomParser (triples : TripleTable,
              resourceManager : ResourceManager,
              classExpressions,
              dataRanges,
              objectProps,
              dataProps: Map<ResourceId, DataProperty>,
              annProps : Map<ResourceId, AnnotationProperty>,
              anns : Map<ResourceId, Annotation>
              ) =
    
    let tripleTable = triples
    let resources = resourceManager
    let ClassExpressions = classExpressions
    let DataRanges = dataRanges
    let ObjectPropertyExpressions = objectProps
    let DataPropertyExpressions = dataProps
    let AnnotationProperties = annProps
    let Annotations = anns
    
    
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

    
    let tryGetDeclaration (declarationMap : Map<ResourceId, 'T>) resourceId =
        let resource = resources.GetResource(resourceId)
        match declarationMap.TryGetValue(resourceId) with
        | false, _ -> failwith $"Invalid OWL ontology. The resource {resource} used as a {declarationMap.Values.GetType()} without declaration."
        | true, decl -> decl
    
        
    
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
    
    
    (* This is an implementation of section 3.2.5 in https://www.w3.org/TR/owl2-mapping-to-rdf/#Analyzing_Declarations *)
    member internal this.extractAxiom   (triple : Ingress.Triple) : Axiom option =
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
                            | Namespaces.RdfsSubClassOf -> ClassAxiom.SubClassOf ([],
                                                                                  tryGetDeclaration ClassExpressions triple.subject,
                                                                                  tryGetDeclaration ClassExpressions triple.obj)
                                                           |> AxiomClassAxiom |> Some
                            | Namespaces.OwlEquivalentClass -> ClassAxiom.EquivalentClasses ([], [tryGetDeclaration ClassExpressions triple.subject; tryGetDeclaration ClassExpressions triple.obj])
                                                               |> AxiomClassAxiom |> Some
                            | Namespaces.OwlDisjointWith -> ClassAxiom.DisjointClasses ([], [tryGetDeclaration ClassExpressions triple.subject; tryGetDeclaration ClassExpressions triple.obj])
                                                                |> AxiomClassAxiom |> Some
                            | Namespaces.OwlDisjointUnionOf -> ClassAxiom.DisjointUnion ([], tryGetResourceClass triple.subject, triple.obj |> GetRdfListElements |> List.map (tryGetDeclaration ClassExpressions))
                                                               |> AxiomClassAxiom |> Some
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
    