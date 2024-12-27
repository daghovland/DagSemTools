(*
 Copyright (C) 2024 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.RdfOwlTranslator

open DagSemTools.Ingress.Namespaces
open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.Ingress
open DagSemTools.OwlOntology
open DagSemTools.RdfOwlTranslator.Ingress
open IriTools

type AxiomParser (triples : TripleTable,
              resourceManager : ResourceManager,
              classExpressions : ResourceId -> ClassExpression,
              dataRanges : ResourceId -> DataRange,
              objectProps : ResourceId -> ObjectPropertyExpression,
              dataProps: ResourceId -> DataProperty,
              annProps : Map<ResourceId, AnnotationProperty>,
              anns : ResourceId -> Annotation list,
              TryGetAnyPropertyAxiom: Annotation list -> ResourceId -> ResourceId -> (Annotation list -> ResourceId -> ObjectPropertyExpression -> Axiom) -> (Annotation list -> ResourceId -> DataProperty -> Axiom) ->(Annotation list -> ResourceId -> AnnotationProperty -> Axiom) -> Axiom,
              TryGetDataOrObjectPropertyAxiom: ResourceId -> (ObjectPropertyExpression -> Axiom) -> (DataProperty -> Axiom) -> Axiom option,
              TryGetClassOrDataRangeAxiom: ResourceId -> (ClassExpression -> Axiom) -> (Datatype -> Axiom) -> Axiom
              ) =
    
    let tripleTable = triples
    let resources = resourceManager
    let ClassExpressions = classExpressions
    let DataRanges = dataRanges
    let ObjectPropertyExpressions = objectProps
    let DataPropertyExpressions = dataProps
    let AnnotationProperties = annProps
    let Annotations = anns
    
    
    let rdfTypeId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.RdfType)))
    let owlOntologyId = resources.AddResource(GraphElement.Iri(new IriReference(Namespaces.OwlOntology)))
    let versionPropId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.OwlVersionIri)))
    let importsPropId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.OwlImport)))
    let owlClassId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.OwlClass)))
    let owlRestrictionId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.OwlRestriction)))
    let owlOnPropertyId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.OwlOnProperty)))
    let owlOnClassId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.OwlOnClass)))
    let owlQualifiedCardinalityId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.OwlQualifiedCardinality)))
    let owlAxiomId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.OwlAxiom)))
    let owlMembersId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.OwlMembers)))
    let owlAnnPropId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.OwlAnnotatedProperty)))
    let owlAnnSourceId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.OwlAnnotatedSource)))
    let owlAnnTargetId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.OwlAnnotatedTarget)))
    let owlInvObjPropId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.OwlObjectInverseOf)))
    let subClassPropId = resources.AddResource(GraphElement.Iri (new IriReference(Namespaces.RdfsSubClassOf)))
    let getResourceIri (resourceId : Ingress.ResourceId) : IriReference option =
        match resources.GetResource(resourceId) with 
            | GraphElement.Iri iri -> Some iri
            | _ -> None
        
    let RequireDataOrObjectProperty resourceId objectPropDecl dataPropDecl =
        match TryGetDataOrObjectPropertyAxiom resourceId objectPropDecl dataPropDecl with
        | Some ax -> ax
        | None -> failwith $"Invalid Owl Ontology {resources.GetResource resourceId} not correctly declared as exactly one data or object property: {GetResourceInfoForErrorMessage tripleTable resources resourceId}"
    
    let getResourceClass (resourceId : Ingress.ResourceId) : Class option =
        getResourceIri resourceId |> Option.map Class.FullIri

    let tryGetResourceIri resourceId =
        match getResourceIri resourceId with
        | Some c -> c
        | None -> failwith $"Invalid resource {resourceId} used compulsory IRI position"
       
    let GetResourceInfoForErrorMessage(subject: ResourceId) : string =
           tripleTable.GetTriplesMentioning subject
            |> Seq.map resources.GetResourceTriple
            |> Seq.map _.ToString()
            |> String.concat ". \n"
 
    
    let tryGetResourceClass resourceId =
        match getResourceClass resourceId with
        | Some c -> c
        | None -> failwith $"Invalid resource {resourceId} used on class position"

    
    let tryGetDeclaration (declarationMap : Map<ResourceId, 'T>) resourceId =
        let resource = resources.GetResource(resourceId)
        match declarationMap.TryGetValue(resourceId) with
        | false, _ -> failwith $"Invalid OWL ontology. The resource {resource} used as a {declarationMap.Values.GetType()} without declaration."
        | true, decl -> decl
    
        
    let FunctionalObjectPropertyAxiom annotations property =
        FunctionalObjectProperty (annotations, property) |> AxiomObjectPropertyAxiom
    let FunctionalDataPropertyAxiom annotations property =
        FunctionalDataProperty (annotations, property) |> AxiomDataPropertyAxiom
    let SubObjectPropertyAxiom annotations superPropertyId objPropExpr   =
         SubObjectPropertyOf (annotations, (SubObjectPropertyExpression objPropExpr), ObjectPropertyExpressions superPropertyId )
        |> AxiomObjectPropertyAxiom
    let SubDataPropertyAxiom  annotations superPropertyId objPropExpr =
         SubDataPropertyOf (annotations, objPropExpr, DataPropertyExpressions superPropertyId )
        |> AxiomDataPropertyAxiom
    let SubAnnotationPropertyAxiom  annotations superPropertyId objPropExpr =
         SubAnnotationPropertyOf (annotations, objPropExpr, FullIri ( tryGetResourceIri superPropertyId) )
        |> AxiomAnnotationAxiom
    
    let EquivalentClassAxiom objectExpression subjectExpression   =
         EquivalentClasses ([], [subjectExpression; ClassExpressions objectExpression ])
        |> AxiomClassAxiom
    let rec EquivalentDataRangeAxiom  objectRange subjectRange =
         AxiomDatatypeDefinition ([], subjectRange, DataRanges objectRange )
    
    
    let EquivalentObjectPropertyAxiom annotations superPropertyId objPropExpr   =
         EquivalentObjectProperties (annotations, [objPropExpr; ObjectPropertyExpressions superPropertyId ])
        |> AxiomObjectPropertyAxiom
    let EquivalentDataPropertyAxiom  annotations superPropertyId objPropExpr =
         EquivalentDataProperties (annotations, [objPropExpr; DataPropertyExpressions superPropertyId] )
        |> AxiomDataPropertyAxiom
    let EquivalentAnnotationPropertyAxiom  annotations superPropertyId objPropExpr =
         failwith "Invalid OWL Ontology: owl:equivalentProperty cannot be used on annotation properties"
    
    let ObjectPropertyDomainAxiom  annotations range objPropExpr =
         ObjectPropertyDomain (objPropExpr, ClassExpressions range )
        |> AxiomObjectPropertyAxiom
    let DataPropertyDomainAxiom  annotations range objPropExpr =
         DataPropertyDomain (annotations, objPropExpr, ClassExpressions range )
        |> AxiomDataPropertyAxiom
    let AnnotationPropertyDomainAxiom  annotations range objPropExpr =
         AnnotationPropertyDomain (annotations, objPropExpr, FullIri ( tryGetResourceIri range) )
        |> AxiomAnnotationAxiom
    
    let ObjectPropertyRangeAxiom  annotations range objPropExpr =
         ObjectPropertyRange (objPropExpr, ClassExpressions range )
        |> AxiomObjectPropertyAxiom
    let DataPropertyRangeAxiom  annotations range objPropExpr =
         DataPropertyRange (annotations, objPropExpr, DataRanges range )
        |> AxiomDataPropertyAxiom
    let AnnotationPropertyRangeAxiom  annotations range objPropExpr =
         AnnotationPropertyRange ( annotations, objPropExpr, FullIri ( tryGetResourceIri range) )
        |> AxiomAnnotationAxiom
    
    let ObjectPropertyAssertionAxiom  subjectId objId predicate =
         ObjectPropertyAssertion ([], predicate, Ingress.tryGetIndividual (resources.GetResource subjectId), Ingress.tryGetIndividual (resources.GetResource objId) )
        |> AxiomAssertion
    let DataPropertyAssertionAxiom   subjectId objId predicate =
         DataPropertyAssertion ([], predicate, subjectId |> resources.GetResource |> tryGetIndividual, objId |> resources.GetResource |> tryGetLiteral )
        |> AxiomAssertion

    (* This is Table 17 in https://www.w3.org/TR/owl2-mapping-to-rdf/#ref-owl-2-specification
    
    If G contains this pattern... 	                                                    ...then the following axiom is added to OG.
        s *:p xlt .
        _:x rdf:type owl:Axiom .
        _:x owl:annotatedSource s .
        _:x owl:annotatedProperty *:p .
        _:x owl:annotatedTarget xlt .
        { s *:p xlt .
          is the main triple of an axiom according to Table 16 and
          G contains possible necessary side triples for the axiom } 	        The result is the axiom corresponding to s *:p xlt . (and possible side triples)
                                                                                            that additionally contains the annotations ANN(_:x).
    *)
    let GetAxiomAnnotations(triple: Triple) =
        tripleTable.GetTriplesWithObjectPredicate(triple.subject, owlAnnSourceId)
                    |> Seq.filter (fun sourceAnnTr -> tripleTable.Contains({subject = sourceAnnTr.subject; predicate = rdfTypeId; obj = owlAxiomId})
                                                    && tripleTable.Contains({subject = sourceAnnTr.subject; predicate = owlAnnPropId; obj = triple.predicate})
                                                    && tripleTable.Contains({subject = sourceAnnTr.subject; predicate = owlAnnTargetId; obj = triple.obj}))
                    |> Seq.map (_.subject)
                    |> Seq.map Annotations
                    |> Seq.concat
                    |> Seq.toList
                    
        
    (* This is an implementation of section 3.2.5 in https://www.w3.org/TR/owl2-mapping-to-rdf/#Analyzing_Declarations *)
    member internal this.extractAxiom   (triple : Ingress.Triple) : Axiom option =
        let axiomAnns = GetAxiomAnnotations triple
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
                                                                                                                           | [disjointList] ->  ClassAxiom.DisjointClasses (axiomAnns,
                                                                                                                                                                            disjointList.obj
                                                                                                                                                                                |> Ingress.GetRdfListElements tripleTable resources
                                                                                                                                                                                |> List.map (ClassExpressions)
                                                                                                                                                                            )
                                                                                                                                                    |> AxiomClassAxiom
                                                                                                                                                    |> Some
                                                                                                                           | _ -> failwith "Several owl:members triples detected on a owl:AllDisjointClasses axiom. This is not valid int owl")
                                                                                    | Namespaces.OwlAllDisjointProperties -> (match tripleTable.GetTriplesWithSubjectPredicate(triple.subject, owlMembersId) |> Seq.toList with
                                                                                                                               | [] -> None
                                                                                                                               | [disjointList] ->  DisjointObjectProperties (axiomAnns, disjointList.obj
                                                                                                                                                                              |> Ingress.GetRdfListElements tripleTable resources
                                                                                                                                                                              |> List.map (ObjectPropertyExpressions))
                                                                                                                                                        |> AxiomObjectPropertyAxiom
                                                                                                                                                        |> Some
                                                                                                                               | _ -> failwith "Several owl:members triples detected on a owl:AllDisjointClasses axiom. This is not valid int owl")
                                                                                    | Namespaces.OwlFunctionalProperty -> RequireDataOrObjectProperty triple.subject (FunctionalObjectPropertyAxiom (Annotations triple.subject))  (FunctionalDataPropertyAxiom (Annotations triple.subject) )
                                                                                                                                                |> Some
                                                                                    | Namespaces.OwlInverseFunctionalProperty -> InverseFunctionalObjectProperty ([], ObjectPropertyExpressions triple.subject)
                                                                                                                                |> AxiomObjectPropertyAxiom
                                                                                                                                |> Some
                                                                                    | Namespaces.OwlReflexiveProperty -> ReflexiveObjectProperty (axiomAnns, ObjectPropertyExpressions triple.subject)
                                                                                                                                |> AxiomObjectPropertyAxiom
                                                                                                                                |> Some
                                                                                    | Namespaces.OwlIrreflexiveProperty -> IrreflexiveObjectProperty (axiomAnns, ObjectPropertyExpressions triple.subject)
                                                                                                                                |> AxiomObjectPropertyAxiom
                                                                                                                                |> Some
                                                                                    | Namespaces.OwlSymmetricProperty -> SymmetricObjectProperty (axiomAnns, ObjectPropertyExpressions triple.subject)
                                                                                                                                |> AxiomObjectPropertyAxiom
                                                                                                                                |> Some
                                                                                    | Namespaces.OwlAsymmetricProperty -> AsymmetricObjectProperty (axiomAnns, ObjectPropertyExpressions triple.subject)
                                                                                                                                |> AxiomObjectPropertyAxiom
                                                                                                                                |> Some
                                                                                    | Namespaces.OwlTransitiveProperty -> TransitiveObjectProperty (axiomAnns, ObjectPropertyExpressions triple.subject)
                                                                                                                                |> AxiomObjectPropertyAxiom
                                                                                                                                |> Some
                                                                                    | _ -> ClassAssertion (axiomAnns,
                                                                                                           ClassExpressions triple.obj,
                                                                                                           (triple.subject |> resources.GetResource |> tryGetIndividual))
                                                                                                |> AxiomAssertion |> Some
                                                                                )
                            | Namespaces.RdfsSubClassOf -> ClassAxiom.SubClassOf (axiomAnns,
                                                                                  ClassExpressions triple.subject,
                                                                                  ClassExpressions triple.obj)
                                                           |> AxiomClassAxiom |> Some
                            | Namespaces.OwlEquivalentClass -> TryGetClassOrDataRangeAxiom triple.obj (EquivalentClassAxiom triple.subject) (EquivalentDataRangeAxiom triple.subject)
                                                                |> Some
                            | Namespaces.OwlDisjointWith -> ClassAxiom.DisjointClasses (axiomAnns, [ClassExpressions triple.subject; ClassExpressions triple.obj])
                                                                |> AxiomClassAxiom |> Some
                            | Namespaces.OwlDisjointUnionOf -> ClassAxiom.DisjointUnion (axiomAnns, tryGetResourceClass triple.subject, triple.obj
                                                                                                                                 |> Ingress.GetRdfListElements tripleTable resources
                                                                                                                                 |> List.map (ClassExpressions))
                                                               |> AxiomClassAxiom |> Some
                            | Namespaces.RdfsSubPropertyOf -> TryGetAnyPropertyAxiom axiomAnns triple.subject triple.obj SubObjectPropertyAxiom SubDataPropertyAxiom SubAnnotationPropertyAxiom
                                                                 |> Some
                            | Namespaces.OwlPropertyChainAxiom -> SubObjectPropertyOf(axiomAnns,
                                                                                      triple.obj
                                                                                          |> Ingress.GetRdfListElements tripleTable resources
                                                                                          |> List.map (ObjectPropertyExpressions)
                                                                                          |> subPropertyExpression.PropertyExpressionChain,
                                                                                      ObjectPropertyExpressions triple.subject)
                                                                |> AxiomObjectPropertyAxiom |> Some
                            | Namespaces.OwlEquivalentProperty -> TryGetAnyPropertyAxiom axiomAnns triple.subject triple.obj EquivalentObjectPropertyAxiom EquivalentDataPropertyAxiom EquivalentAnnotationPropertyAxiom
                                                                  |> Some
                            | Namespaces.OwlPropertyDisjointWith -> DisjointObjectProperties(axiomAnns, [ObjectPropertyExpressions triple.subject
                                                                                                         ObjectPropertyExpressions triple.obj])
                                                                    |> AxiomObjectPropertyAxiom |> Some
                            | Namespaces.RdfsDomain -> TryGetAnyPropertyAxiom axiomAnns triple.subject triple.obj ObjectPropertyDomainAxiom DataPropertyDomainAxiom AnnotationPropertyDomainAxiom
                                                       |> Some
                            | Namespaces.RdfsRange -> TryGetAnyPropertyAxiom axiomAnns triple.subject triple.obj ObjectPropertyRangeAxiom DataPropertyRangeAxiom AnnotationPropertyRangeAxiom
                                                       |> Some
                            | Namespaces.OwlObjectInverseOf -> InverseObjectProperties (axiomAnns,
                                                                                        ObjectPropertyExpressions triple.subject,
                                                                                        ObjectPropertyExpressions triple.obj)
                                                                |> AxiomObjectPropertyAxiom |> Some
                            | _ -> TryGetDataOrObjectPropertyAxiom triple.predicate (ObjectPropertyAssertionAxiom triple.subject triple.obj) (DataPropertyAssertionAxiom triple.subject triple.obj))
        
    