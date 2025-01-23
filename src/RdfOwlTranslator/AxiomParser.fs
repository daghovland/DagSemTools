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
              resourceManager : GraphElementManager,
              classExpressions : GraphElementId -> ClassExpression,
              dataRanges : GraphElementId -> DataRange,
              objectProps : GraphElementId -> ObjectPropertyExpression,
              dataProps: GraphElementId -> DataProperty,
              annProps : Map<GraphElementId, AnnotationProperty>,
              anns : GraphElementId -> Annotation list,
              TryGetAnyPropertyAxiom: Annotation list -> GraphElementId -> GraphElementId -> (Annotation list -> GraphElementId -> ObjectPropertyExpression -> Axiom) -> (Annotation list -> GraphElementId -> DataProperty -> Axiom) ->(Annotation list -> GraphElementId -> AnnotationProperty -> Axiom) -> Axiom,
              TryGetDataOrObjectPropertyAxiom: GraphElementId -> (ObjectPropertyExpression -> Axiom) -> (DataProperty -> Axiom) -> Axiom option,
              TryGetClassAxiom: GraphElementId -> (ClassExpression -> Axiom) -> (Datatype -> Axiom) -> Axiom option,
              TryGetDataRangeAxiom: GraphElementId -> (ClassExpression -> Axiom) -> (Datatype -> Axiom) -> Axiom option
              ) =
    
    let tripleTable = triples
    let resources = resourceManager
    let ClassExpressions = classExpressions
    let DataRanges = dataRanges
    let ObjectPropertyExpressions = objectProps
    let DataPropertyExpressions = dataProps
    let AnnotationProperties = annProps
    let Annotations = anns
    
    
    let rdfTypeId = resources.AddNodeResource(RdfResource.Iri (new IriReference(RdfType)))
    let owlOntologyId = resources.AddNodeResource(RdfResource.Iri(new IriReference(OwlOntology)))
    let versionPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlVersionIri)))
    let importsPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlImport)))
    let owlClassId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlClass)))
    let owlRestrictionId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlRestriction)))
    let owlOnPropertyId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlOnProperty)))
    let owlOnClassId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlOnClass)))
    let owlQualifiedCardinalityId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlQualifiedCardinality)))
    let owlAxiomId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlAxiom)))
    let owlMembersId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlMembers)))
    let owlAnnPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlAnnotatedProperty)))
    let owlAnnSourceId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlAnnotatedSource)))
    let owlAnnTargetId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlAnnotatedTarget)))
    let owlInvObjPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlObjectInverseOf)))
    let subClassPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(RdfsSubClassOf)))
        
    let RequireDataOrObjectProperty resourceId objectPropDecl dataPropDecl =
        match TryGetDataOrObjectPropertyAxiom resourceId objectPropDecl dataPropDecl with
        | Some ax -> ax
        | None -> failwith $"Invalid Owl Ontology {resources.GetGraphElement resourceId} not correctly declared as exactly one data or object property: {GetResourceInfoForErrorMessage tripleTable resources resourceId}"
    
    let getResourceClass (resourceId : GraphElementId) : Class option =
        resources.GetNamedResource resourceId |> Option.map Class.FullIri

    let RequireResourceIri resourceId =
        match resources.GetNamedResource resourceId with
        | Some c -> c
        | None -> failwith $"Invalid resource {resourceId} used compulsory IRI position"
       
    let GetResourceInfoForErrorMessage(subject: GraphElementId) : string =
           tripleTable.GetTriplesMentioning subject
            |> Seq.map resources.GetResourceTriple
            |> Seq.map _.ToString()
            |> String.concat ". \n"
 
    
    let RequireResourceClass resourceId =
        match getResourceClass resourceId with
        | Some c -> c
        | None -> failwith $"Invalid resource {resourceId} used on class position"

    
    let RequireDeclaration (declarationMap : Map<GraphElementId, 'T>) resourceId =
        let resource = resources.GetGraphElement(resourceId)
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
         SubAnnotationPropertyOf (annotations, objPropExpr, FullIri ( RequireResourceIri superPropertyId) )
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
         AnnotationPropertyDomain (annotations, objPropExpr, FullIri ( RequireResourceIri range) )
        |> AxiomAnnotationAxiom
    
    let ObjectPropertyRangeAxiom  annotations range objPropExpr =
         ObjectPropertyRange (objPropExpr, ClassExpressions range )
        |> AxiomObjectPropertyAxiom
    let DataPropertyRangeAxiom  annotations range objPropExpr =
         DataPropertyRange (annotations, objPropExpr, DataRanges range )
        |> AxiomDataPropertyAxiom
    let AnnotationPropertyRangeAxiom  annotations range objPropExpr =
         AnnotationPropertyRange ( annotations, objPropExpr, FullIri ( RequireResourceIri range) )
        |> AxiomAnnotationAxiom
    
    let ObjectPropertyAssertionAxiom  subjectId objId predicate =
         ObjectPropertyAssertion ([], predicate, tryGetIndividual (resources.GetGraphElement subjectId), tryGetIndividual (resources.GetGraphElement objId) )
        |> AxiomAssertion
    let DataPropertyAssertionAxiom   subjectId objId predicate =
         DataPropertyAssertion ([], predicate,
                                subjectId |> resources.GetGraphElement |> tryGetIndividual,
                                objId |> resources.GetGraphElement |> tryGetLiteral |> GraphElement.GraphLiteral )
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
    member internal this.extractAxiom   (triple : Triple) : Axiom option =
        let axiomAnns = GetAxiomAnnotations triple
        resources.GetNamedResource triple.predicate
        |> Option.map (_.ToString())
        |> Option.bind (fun (predicateIri) -> 
                        match predicateIri with
                            | RdfType -> resources.GetNamedResource(triple.obj)
                                                    |> Option.map (_.ToString())
                                                    |> Option.bind (fun classIri -> match classIri with
                                                                                    | OwlClass -> resources.GetNamedResource (triple.subject)
                                                                                                                |> Option.map FullIri
                                                                                                                |> Option.map ClassDeclaration
                                                                                                                |> Option.map (fun e -> Declaration ([],e))
                                                                                                                |> Option.map AxiomDeclaration
                                                                                    | RdfsDatatype -> resources.GetNamedResource (triple.subject)
                                                                                                                    |> Option.map FullIri
                                                                                                                    |> Option.map DatatypeDeclaration
                                                                                                                    |> Option.map (fun e -> Declaration ([],e))
                                                                                                                    |> Option.map AxiomDeclaration
                                                                                    | OwlObjectProperty -> resources.GetNamedResource (triple.subject)
                                                                                                                    |> Option.map FullIri
                                                                                                                    |> Option.map ObjectPropertyDeclaration
                                                                                                                    |> Option.map (fun e -> Declaration ([],e))
                                                                                                                    |> Option.map AxiomDeclaration
                                                                                    | OwlDatatypeProperty -> resources.GetNamedResource (triple.subject)
                                                                                                                        |> Option.map FullIri
                                                                                                                        |> Option.map DataPropertyDeclaration
                                                                                                                        |> Option.map (fun e -> Declaration ([],e))
                                                                                                                        |> Option.map AxiomDeclaration
                                                                                    | OwlAnnotationProperty -> resources.GetNamedResource (triple.subject)
                                                                                                                        |> Option.map FullIri
                                                                                                                        |> Option.map AnnotationPropertyDeclaration
                                                                                                                        |> Option.map (fun e -> Declaration ([],e))
                                                                                                                        |> Option.map AxiomDeclaration
                                                                                    | OwlNamedIndividual -> (match resources.GetResource(triple.subject) with
                                                                                                                                | Some (Iri i) -> NamedIndividual (FullIri i)
                                                                                                                                | Some (AnonymousBlankNode bn) -> AnonymousIndividual bn
                                                                                                                                | None -> failwith "Invalid individual ")
                                                                                                                        |> NamedIndividualDeclaration |> (fun e -> Declaration ([],e)) |> AxiomDeclaration |> Some
                                                                                    | OwlAllDisjointClasses -> (match tripleTable.GetTriplesWithSubjectPredicate(triple.subject, owlMembersId) |> Seq.toList with
                                                                                                                           | [] -> None
                                                                                                                           | [disjointList] ->  ClassAxiom.DisjointClasses (axiomAnns,
                                                                                                                                                                            disjointList.obj
                                                                                                                                                                                |> GetRdfListElements tripleTable resources
                                                                                                                                                                                |> List.map (ClassExpressions)
                                                                                                                                                                            )
                                                                                                                                                    |> AxiomClassAxiom
                                                                                                                                                    |> Some
                                                                                                                           | _ -> failwith "Several owl:members triples detected on a owl:AllDisjointClasses axiom. This is not valid int owl")
                                                                                    | OwlAllDisjointProperties -> (match tripleTable.GetTriplesWithSubjectPredicate(triple.subject, owlMembersId) |> Seq.toList with
                                                                                                                               | [] -> None
                                                                                                                               | [disjointList] ->  DisjointObjectProperties (axiomAnns, disjointList.obj
                                                                                                                                                                              |> GetRdfListElements tripleTable resources
                                                                                                                                                                              |> List.map (ObjectPropertyExpressions))
                                                                                                                                                        |> AxiomObjectPropertyAxiom
                                                                                                                                                        |> Some
                                                                                                                               | _ -> failwith "Several owl:members triples detected on a owl:AllDisjointClasses axiom. This is not valid int owl")
                                                                                    | OwlFunctionalProperty -> RequireDataOrObjectProperty triple.subject (FunctionalObjectPropertyAxiom (Annotations triple.subject))  (FunctionalDataPropertyAxiom (Annotations triple.subject) )
                                                                                                                                                |> Some
                                                                                    | OwlInverseFunctionalProperty -> InverseFunctionalObjectProperty ([], ObjectPropertyExpressions triple.subject)
                                                                                                                                |> AxiomObjectPropertyAxiom
                                                                                                                                |> Some
                                                                                    | OwlReflexiveProperty -> ReflexiveObjectProperty (axiomAnns, ObjectPropertyExpressions triple.subject)
                                                                                                                                |> AxiomObjectPropertyAxiom
                                                                                                                                |> Some
                                                                                    | OwlIrreflexiveProperty -> IrreflexiveObjectProperty (axiomAnns, ObjectPropertyExpressions triple.subject)
                                                                                                                                |> AxiomObjectPropertyAxiom
                                                                                                                                |> Some
                                                                                    | OwlSymmetricProperty -> SymmetricObjectProperty (axiomAnns, ObjectPropertyExpressions triple.subject)
                                                                                                                                |> AxiomObjectPropertyAxiom
                                                                                                                                |> Some
                                                                                    | OwlAsymmetricProperty -> AsymmetricObjectProperty (axiomAnns, ObjectPropertyExpressions triple.subject)
                                                                                                                                |> AxiomObjectPropertyAxiom
                                                                                                                                |> Some
                                                                                    | OwlTransitiveProperty -> TransitiveObjectProperty (axiomAnns, ObjectPropertyExpressions triple.subject)
                                                                                                                                |> AxiomObjectPropertyAxiom
                                                                                                                                |> Some
                                                                                    | _ -> ClassAssertion (axiomAnns,
                                                                                                           ClassExpressions triple.obj,
                                                                                                           (triple.subject |> resources.GetGraphElement |> tryGetIndividual))
                                                                                                |> AxiomAssertion |> Some
                                                                                )
                            | RdfsSubClassOf -> ClassAxiom.SubClassOf (axiomAnns,
                                                                                  ClassExpressions triple.subject,
                                                                                  ClassExpressions triple.obj)
                                                           |> AxiomClassAxiom |> Some
                            | OwlEquivalentClass -> match (TryGetDataRangeAxiom triple.obj (EquivalentClassAxiom triple.subject) (EquivalentDataRangeAxiom triple.subject)) with
                                                                | Some cl -> Some cl
                                                                | None -> TryGetClassAxiom triple.obj (EquivalentClassAxiom triple.subject) (EquivalentDataRangeAxiom triple.subject)
                                                               
                            | OwlDisjointWith -> ClassAxiom.DisjointClasses (axiomAnns, [ClassExpressions triple.subject; ClassExpressions triple.obj])
                                                                |> AxiomClassAxiom |> Some
                            | OwlDisjointUnionOf -> ClassAxiom.DisjointUnion (axiomAnns, RequireResourceClass triple.subject, triple.obj
                                                                                                                                 |> GetRdfListElements tripleTable resources
                                                                                                                                 |> List.map (ClassExpressions))
                                                               |> AxiomClassAxiom |> Some
                            | RdfsSubPropertyOf -> TryGetAnyPropertyAxiom axiomAnns triple.subject triple.obj SubObjectPropertyAxiom SubDataPropertyAxiom SubAnnotationPropertyAxiom
                                                                 |> Some
                            | OwlPropertyChainAxiom -> SubObjectPropertyOf(axiomAnns,
                                                                                      triple.obj
                                                                                          |> GetRdfListElements tripleTable resources
                                                                                          |> List.map (ObjectPropertyExpressions)
                                                                                          |> subPropertyExpression.PropertyExpressionChain,
                                                                                      ObjectPropertyExpressions triple.subject)
                                                                |> AxiomObjectPropertyAxiom |> Some
                            | OwlEquivalentProperty -> TryGetAnyPropertyAxiom axiomAnns triple.subject triple.obj EquivalentObjectPropertyAxiom EquivalentDataPropertyAxiom EquivalentAnnotationPropertyAxiom
                                                                  |> Some
                            | OwlPropertyDisjointWith -> DisjointObjectProperties(axiomAnns, [ObjectPropertyExpressions triple.subject
                                                                                              ObjectPropertyExpressions triple.obj])
                                                                    |> AxiomObjectPropertyAxiom |> Some
                            | RdfsDomain -> TryGetAnyPropertyAxiom axiomAnns triple.subject triple.obj ObjectPropertyDomainAxiom DataPropertyDomainAxiom AnnotationPropertyDomainAxiom
                                                       |> Some
                            | RdfsRange -> TryGetAnyPropertyAxiom axiomAnns triple.subject triple.obj ObjectPropertyRangeAxiom DataPropertyRangeAxiom AnnotationPropertyRangeAxiom
                                                       |> Some
                            | OwlObjectInverseOf -> InverseObjectProperties (axiomAnns,
                                                                                        ObjectPropertyExpressions triple.subject,
                                                                                        ObjectPropertyExpressions triple.obj)
                                                                |> AxiomObjectPropertyAxiom |> Some
                            | _ -> TryGetDataOrObjectPropertyAxiom triple.predicate
                                       (ObjectPropertyAssertionAxiom triple.subject triple.obj)
                                       (DataPropertyAssertionAxiom triple.subject triple.obj))
        
    