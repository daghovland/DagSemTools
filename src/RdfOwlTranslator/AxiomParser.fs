(*
 Copyright (C) 2024 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.RdfOwlTranslator

open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.Ingress
open DagSemTools.OwlOntology
open IriTools

type AxiomParser (triples : TripleTable,
              resourceManager : ResourceManager,
              classExpressions : ResourceId -> ClassExpression,
              dataRanges : ResourceId -> DataRange,
              objectProps : ResourceId -> ObjectPropertyExpression,
              dataProps: ResourceId -> DataProperty,
              annProps : Map<ResourceId, AnnotationProperty>,
              anns : ResourceId -> Annotation list,
              TryGetAnyPropertyAxiom: Annotation list -> ResourceId -> ResourceId -> (Annotation list -> ResourceId -> ObjectPropertyExpression -> Axiom) -> (Annotation list -> ResourceId -> DataProperty -> Axiom) ->(Annotation list -> ResourceId -> AnnotationProperty -> Axiom) -> Axiom
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
    
        
    
    (* Extracts an RDF list. See Table 3 in https://www.w3.org/TR/owl2-mapping-to-rdf/
        Assumes head is the head of some rdf list in the triple-table
        The requirements in the specs includes non-circular lists, so blindly assumes this is true
     *)
    let rec _GetRdfListElements listId acc : ResourceId list =
        if (listId = rdfNilId) then
            acc
        else
            let head = match tripleTable.GetTriplesWithSubjectPredicate(listId, rdfFirstId) |> Seq.toList with
                        | [] -> failwith $"Invalid list defined at {resources.GetResource(listId)}: {GetResourceInfoForErrorMessage listId}"
                        | [headElement] -> headElement.obj
                        | _ -> failwith $"Invalid list defined at {resources.GetResource(listId)}: {GetResourceInfoForErrorMessage listId}"
            if (Seq.contains head acc) then
                failwith $"Invalid cyclic list defined at {resources.GetResource(listId)}: {GetResourceInfoForErrorMessage listId}"
            else
                let rest = match tripleTable.GetTriplesWithSubjectPredicate(listId, rdfRestId) |> Seq.toList with
                            | [] -> failwith $"Invalid list defined at {resources.GetResource(listId)}"
                            | [headElement] -> headElement.obj
                            | _ -> failwith $"Invalid list defined at {resources.GetResource(listId)}"
                _GetRdfListElements rest (head :: acc)     
    let GetRdfListElements listId=
        _GetRdfListElements listId []    
    
    let SubObjectPropertyAxiom annotations superPropertyId objPropExpr   =
         SubObjectPropertyOf (annotations, (SubObjectPropertyExpression objPropExpr), ObjectPropertyExpressions superPropertyId )
        |> AxiomObjectPropertyAxiom
    let SubDataPropertyAxiom  annotations superPropertyId objPropExpr =
         SubDataPropertyOf (annotations, objPropExpr, DataPropertyExpressions superPropertyId )
        |> AxiomDataPropertyAxiom
    let SubAnnotationPropertyAxiom  annotations superPropertyId objPropExpr =
         SubAnnotationPropertyOf (annotations, objPropExpr, FullIri ( tryGetResourceIri superPropertyId) )
        |> AxiomAnnotationAxiom
    
    
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
                                                                                                                           | [disjointList] ->  ClassAxiom.DisjointClasses (Annotations triple.subject,
                                                                                                                                                                            disjointList.obj
                                                                                                                                                                                |> GetRdfListElements
                                                                                                                                                                                |> List.map (ClassExpressions)
                                                                                                                                                                            )
                                                                                                                                                    |> AxiomClassAxiom
                                                                                                                                                    |> Some
                                                                                                                           | _ -> failwith "Several owl:members triples detected on a owl:AllDisjointClasses axiom. This is not valid int owl")
                                                                                    | Namespaces.OwlAllDisjointProperties -> (match tripleTable.GetTriplesWithSubjectPredicate(triple.subject, owlMembersId) |> Seq.toList with
                                                                                                                               | [] -> None
                                                                                                                               | [disjointList] ->  DisjointObjectProperties (Annotations triple.subject, disjointList.obj |> GetRdfListElements |> List.map (ObjectPropertyExpressions))
                                                                                                                                                        |> AxiomObjectPropertyAxiom
                                                                                                                                                        |> Some
                                                                                                                               | _ -> failwith "Several owl:members triples detected on a owl:AllDisjointClasses axiom. This is not valid int owl")
                                                                                    | _ -> None) 
                            | Namespaces.RdfsSubClassOf -> ClassAxiom.SubClassOf ([],
                                                                                  ClassExpressions triple.subject,
                                                                                  ClassExpressions triple.obj)
                                                           |> AxiomClassAxiom |> Some
                            | Namespaces.OwlEquivalentClass -> ClassAxiom.EquivalentClasses ([], [ClassExpressions triple.subject
                                                                                                  ClassExpressions triple.obj])
                                                               |> AxiomClassAxiom |> Some
                            | Namespaces.OwlDisjointWith -> ClassAxiom.DisjointClasses ([], [ClassExpressions triple.subject; ClassExpressions triple.obj])
                                                                |> AxiomClassAxiom |> Some
                            | Namespaces.OwlDisjointUnionOf -> ClassAxiom.DisjointUnion ([], tryGetResourceClass triple.subject, triple.obj |> GetRdfListElements |> List.map (ClassExpressions))
                                                               |> AxiomClassAxiom |> Some
                            | Namespaces.RdfsSubPropertyOf -> TryGetAnyPropertyAxiom (Annotations triple.subject) triple.subject triple.obj SubObjectPropertyAxiom SubDataPropertyAxiom SubAnnotationPropertyAxiom
                                                                 |> Some
                            | Namespaces.OwlPropertyChainAxiom -> SubObjectPropertyOf([],
                                                                                      triple.obj
                                                                                          |> GetRdfListElements
                                                                                          |> List.map (ObjectPropertyExpressions)
                                                                                          |> subPropertyExpression.PropertyExpressionChain,
                                                                                      ObjectPropertyExpressions triple.subject)
                                                                |> AxiomObjectPropertyAxiom |> Some
                            | Namespaces.OwlEquivalentProperty -> EquivalentObjectProperties([], [ObjectPropertyExpressions triple.subject; ObjectPropertyExpressions triple.obj])
                                                                |> AxiomObjectPropertyAxiom |> Some
                            | Namespaces.OwlPropertyDisjointWith -> DisjointObjectProperties([], [ObjectPropertyExpressions triple.subject; ObjectPropertyExpressions triple.obj])
                                                                    |> AxiomObjectPropertyAxiom |> Some
                            | Namespaces.RdfsDomain -> TryGetAnyPropertyAxiom (Annotations triple.subject) triple.subject triple.obj ObjectPropertyDomainAxiom DataPropertyDomainAxiom AnnotationPropertyDomainAxiom
                                                       |> Some
                            | Namespaces.RdfsRange -> TryGetAnyPropertyAxiom (Annotations triple.subject) triple.subject triple.obj ObjectPropertyRangeAxiom DataPropertyRangeAxiom AnnotationPropertyRangeAxiom
                                                       |> Some
                            | _ -> None)
    