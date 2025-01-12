(*
 Copyright (C) 2024 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.RdfOwlTranslator

open DagSemTools.AlcTableau.DataRange
open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.Ingress
open DagSemTools.Ingress.Namespaces
open DagSemTools.OwlOntology
open IriTools


(* These functions correspond to table 13 in https://www.w3.org/TR/owl2-mapping-to-rdf/#Parsing_of_Axioms 
    Assumes the IRI-based declarations CE, DR, etc. are already made
    This table / function only handles anonymous nodes/blank IRIs
*)
type ClassExpressionParser (triples : TripleTable,
              resourceManager : GraphElementManager) =

    let tripleTable = triples
    let resources = resourceManager

    let rdfTypeId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.RdfType)))
    let owlOntologyId = resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.OwlOntology)))
    let versionPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlVersionIri)))
    let importsPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlImport)))
    let owlClassId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlClass)))
    let owlRestrictionId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlRestriction)))
    let owlOnPropertyId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlOnProperty)))
    let owlOnPropertiesId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlOnProperties)))
    let owlSomeValueFromId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlSomeValuesFrom)))
    let owlAllValuesFromId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlAllValuesFrom)))
    let owlIntersectionOfId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlIntersectionOf)))
    let owlUnionOfId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlUnionOf)))
    let owlComplementOfId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlComplementOf)))
    let owlOneOfId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlOneOf)))
    let owlHasValueId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlHasValue)))
    let owlHasSelfId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlHasSelf)))
    let owlOnClassId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlOnClass)))
    
    let owlOnDataRangeId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlOnDataRange)))
    let owlQualifiedCardinalityId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlQualifiedCardinality)))
    let owlMaxQualifiedCardinalityId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlMaxQualifiedCardinality)))
    let owlMinQualifiedCardinalityId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlMinQualifiedCardinality)))
    let owlMaxCardinalityId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlMaxCardinality)))
    let owlMinCardinalityId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlMinCardinality)))
    let owlCardinalityId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlCardinality)))
    let owlAxiomId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlAxiom)))
    let owlMembersId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlMembers)))
    let owlAnnPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlAnnotatedProperty)))
    let owlAnnSourceId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlAnnotatedSource)))
    let owlAnnTargetId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlAnnotatedTarget)))
    let owlInvObjPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.OwlObjectInverseOf)))
    let subClassPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.RdfsSubClassOf)))
    let rdfNilId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.RdfNil)))
    let rdfFirstId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.RdfFirst)))
    let rdfRestId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.RdfRest)))
        
    let GetResourceInfoForErrorMessage subject : string =
           tripleTable.GetTriplesMentioning subject
            |> Seq.map resources.GetResourceTriple
            |> Seq.map _.ToString()
            |> String.concat ". \n"
    let getResourceClass resourceId  : Class option =
        resources.GetNamedResource resourceId |> Option.map Class.FullIri

    let RequireResourceIri resourceId =
        match resources.GetNamedResource resourceId with
        | Some c -> c
        | None -> failwith $"Invalid resource {resourceId} used compulsory IRI position"

    let RequireResourceClass resourceId =
        match getResourceClass resourceId with
        | Some c -> c
        | None -> failwith $"Invalid resource {resourceId} used on class position"

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
            |> Seq.choose (fun classId -> resources.GetNamedResource(classId) |> Option.map (fun classIri -> (classId, classIri)))
    
    
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
        let typeId = resources.AddNodeResource(RdfResource.Iri (new IriReference(typeIri)))
        Seq.concat [getBasicDeclarations typeId; getAxiomDeclarations typeId]
        |> Seq.choose (fun id -> resources.GetNamedResource id |> Option.map Iri.FullIri 
                                                |> Option.map (fun iri -> (id, iri)))
                                                
    (* CE *)
    let mutable ClassExpressions : Map<GraphElementId, ClassExpression> =
        let mutable specDeclarations = (getInitialDeclarations Namespaces.OwlClass) |> Seq.map (fun (id, iri) ->  (id, ClassName iri)) |> Map.ofSeq
        getSubClassClasses
        |> Seq.fold (fun specDecls (classId, classIri) ->
            if not (specDecls.ContainsKey classId) then
                specDecls.Add(classId, ClassName (Iri.FullIri classIri))
            else
                specDecls
            ) specDeclarations
    
    (* DR *)
    let mutable DataRanges : Map<GraphElementId, DataRange> =
        getInitialDeclarations Namespaces.RdfsDatatype
        |> Seq.map (fun (id, iri) ->  (id, NamedDataRange iri))
        |> Map.ofSeq
    (* OPE *)
    let mutable ObjectPropertyExpressions : Map<GraphElementId, ObjectPropertyExpression> =
        let mutable firstMap = getInitialDeclarations Namespaces.OwlObjectProperty
                            |> Seq.map (fun (id, iri) ->  (id, NamedObjectProperty iri))
        let secondMap = tripleTable.GetTriplesWithPredicate(owlInvObjPropId)
                            |> Seq.choose (fun invTR -> resources.GetNamedResource(invTR.obj) |> Option.map (fun obj -> (invTR.subject, obj)))
                            |> Seq.map (fun (subj, obj) -> (subj, ObjectPropertyExpression.InverseObjectProperty (NamedObjectProperty (FullIri obj)) ))
        [firstMap ; secondMap] |> Seq.concat |> Map.ofSeq
    (* DPE *)
    let mutable DataPropertyExpressions : Map<GraphElementId, DataProperty> =
        getInitialDeclarations Namespaces.OwlDatatypeProperty
        |> Seq.map (fun (id, iri) ->  (id, iri))
        |> Map.ofSeq
        
        
    (* AP *)
    let mutable AnnotationProperties : Map<GraphElementId, AnnotationProperty> =
        getInitialDeclarations Namespaces.OwlAnnotationProperty
        |> Seq.map (fun (id, iri) ->  (id, iri))
        |> Map.ofSeq
    let mutable Individuals : Map<GraphElementId, Individual> =
        getInitialDeclarations Namespaces.OwlNamedIndividual
        |> Seq.map (fun (id, iri) -> (id, NamedIndividual iri))
        |> Map.ofSeq
    
    (* ANN: Section 3.2.2 and Table 10 in https://www.w3.org/TR/owl2-mapping-to-rdf/#ref-owl-2-specification *)
    let mutable Annotations : Map<GraphElementId, Annotation list> =
        AnnotationProperties
        |> Map.toSeq
        |> Seq.map (fun (annPropId, annProp) -> tripleTable.GetTriplesWithPredicate(annPropId)
                                                |> Seq.choose (fun annTr ->
                                                                // TODO: Is it ok to just ignore literals here?
                                                                resources.GetResource(annTr.obj)
                                                                |> Option.map (fun resource ->
                                                                    (annTr.subject, Ingress.createAnnotationValue Individuals annTr.obj resource)))
                                                |> Seq.map (fun (annotatedObj, annVal) ->
                                                    (annotatedObj,
                                                     (Annotation (annProp, annVal)) )))
                                                |> Seq.concat
                                                |> Seq.fold (fun acc (key, annotation) ->
                                                    match Map.tryFind key acc with
                                                    | Some values -> Map.add key (annotation :: values) acc
                                                    | None -> Map.add key [annotation] acc
                                                ) Map.empty
        
    let getAnnotations resourceId =
        match Annotations.TryFind resourceId with
        | Some annotationList -> annotationList
        | None -> []
    
    (* This is called  when propGraphElementId can only be an object property
       The function returns either the declared object property, or if it is not already a data or annotaiton property,
        a simple named declaration
        This is not completely per spec, but many ontologies around do not specify all properties *)
    let tryGetObjectPropertyExpressions propGraphElementId =
        match ObjectPropertyExpressions.TryGetValue propGraphElementId with
        | true, prop -> prop
        | false, _ -> if ((DataPropertyExpressions.ContainsKey propGraphElementId) || (AnnotationProperties.ContainsKey propGraphElementId)) then 
                            failwith $"Invalid OWL ontology: Property {resources.GetGraphElement propGraphElementId} used both as object property and either data or annotation property: {GetResourceInfoForErrorMessage propGraphElementId}"
                      else
                          match (resources.GetNamedResource propGraphElementId) with
                          | Some iri -> NamedObjectProperty (FullIri iri)
                          // | Resource.AnonymousBlankNode bn -> AnonymousObjectProperty bn
                          | x -> failwith $"Invalid OWL Ontology: {x} used as an object property"
    
    
    (* This is called  when propGraphElementId can only be aa data property
       The function returns either the declared object property, or if it is not already a object or annotaiton property,
        a simple named declaration
        This is not completely per spec, but many ontologies around do not specify all properties *)
    let tryGetDataPropertyExpressions propGraphElementId =
        match DataPropertyExpressions.TryGetValue propGraphElementId with
        | true, prop -> prop
        | false, _ -> if ((ObjectPropertyExpressions.ContainsKey propGraphElementId) || (AnnotationProperties.ContainsKey propGraphElementId)) then 
                            failwith $"Invalid OWL ontology: Property {resources.GetGraphElement propGraphElementId} used both as data property and either object or annotation property"
                      else
                          match (resources.GetNamedResource propGraphElementId) with
                          | Some iri -> FullIri iri
                          // | Resource.AnonymousBlankNode bn -> AnonymousObjectProperty bn
                          | x -> failwith $"Invalid OWL Ontology: {x} used as an object property"
   
    
    (* This is called  when propGraphElementId can only be an object or data property
        and a delaration or axioms is needed
        *)
    let tryGetPropertyDeclaration propResourceId (objectDeclarer) (dataDeclarer) =
        match (ObjectPropertyExpressions.TryGetValue propResourceId, DataPropertyExpressions.TryGetValue propResourceId) with
        | ((true, _), (true, _)) -> None 
        | ((true, expr), (false,_)) -> Some (objectDeclarer expr) 
        | ((false, _), (true,expr)) -> Some (dataDeclarer expr)
        | ((false, _), (false, _)) -> None 
    
    let RequireObjectOrDataPropDeclaration resourceId objectDeclarer dataDeclarer =
        match tryGetPropertyDeclaration resourceId objectDeclarer dataDeclarer with
        | Some ax -> ax
        | None -> failwith $"Owl Invalid ontology. Property {resources.GetGraphElement resourceId} must be declared as an object property or datatype property: {GetResourceInfoForErrorMessage resourceId}"
    
    (* This is called  when propResourceId can be an object-, data- or annotation-property
        and a declaration axiom is needed
        *)
    let tryGetAnyPropertyAxiom annotationList propResourceId rangeResourceId (objectDeclarer: Annotation list -> GraphElementId -> ObjectPropertyExpression -> Axiom) (dataDeclarer: Annotation list -> GraphElementId -> DataProperty -> Axiom) (annotationDeclarer : Annotation list -> GraphElementId -> AnnotationProperty -> Axiom) =
        match (ObjectPropertyExpressions.TryGetValue propResourceId, DataPropertyExpressions.TryGetValue propResourceId, AnnotationProperties.TryGetValue propResourceId) with
        | ((true, _), (true, _), _) -> failwith $"Invalid Owl Ontology {resources.GetGraphElement propResourceId} used both as data and object property: {GetResourceInfoForErrorMessage propResourceId}"
        | ((true, _), _, (true, _)) -> failwith $"Invalid Owl Ontology {resources.GetGraphElement propResourceId} used both as annotation and object property: {GetResourceInfoForErrorMessage propResourceId}"
        | (_, (true, _), (true, _)) -> failwith $"Invalid Owl Ontology {resources.GetGraphElement propResourceId} used both as data and annotation property: {GetResourceInfoForErrorMessage propResourceId}"
        | ((true, expr), (false,_), (false, _)) -> objectDeclarer annotationList rangeResourceId expr 
        | ((false, _), (true,expr), (false, _)) -> dataDeclarer annotationList rangeResourceId expr
        | ((false, _), (false,_), (true, expr)) -> annotationDeclarer annotationList rangeResourceId expr
        | ((false, _), (false, _), (false,_)) -> failwith $"Owl Invalid ontology. Property {resources.GetGraphElement propResourceId} must be declared as an annotation property, object property or datatype property: {GetResourceInfoForErrorMessage propResourceId}"
    
    
    (* This is called  when propResourceId can only be a class
       The function returns either the declared class, or if it is not already a datarange,
        a simple named declaration
        This is not completely per spec, but many ontologies around do not specify all properties *)
    let tryGetClassExpressions propResourceId =
        match ClassExpressions.TryGetValue propResourceId with
        | true, prop -> prop
        | false, _ -> if ((DataRanges.ContainsKey propResourceId)) then 
                            failwith $"Invalid OWL ontology: Property {resources.GetGraphElement propResourceId} used both as a data range and a class"
                      else
                          match (resources.GetResource propResourceId) with
                          | Some (RdfResource.Iri iri) -> ClassName (FullIri iri)
                          | Some (AnonymousBlankNode bn) -> failwith $"BUG: {resources.GetGraphElement propResourceId} used as a class without being defined" // AnonymousClass bn
                          | None -> failwith $"Invalid OWL Ontology: {resources.GetGraphElement propResourceId} used as a class"
                          
    (* This is called  when propResourceId can only be a data range
        *)
    let tryGetDataRange propResourceId =
        match DataRanges.TryGetValue propResourceId with
        | true, prop -> prop
        | false, _ -> failwith $"Invalid OWL ontology: {resources.GetGraphElement propResourceId} used  as a data range but not declared"
        
    (* This is aalled when resourceId can be a class or data range *)
    let tryGetClassOrDataRangeAxiom  resourceId classDeclarer datatypeDeclarer =
        match (ClassExpressions.TryGetValue resourceId, DataRanges.TryGetValue resourceId) with
        | ((true, expr), (false, _)) -> classDeclarer expr
        | ((false, _), (true, expr)) -> match expr with
                                            | DataRange.NamedDataRange dt -> datatypeDeclarer dt
                                            | _ -> failwith $"Invalid OWL Ontology. Only datatypes can be defined. {resources.GetGraphElement resourceId} is not a datatype"
        | _ -> failwith $"Invalid OWL Ontology. {resources.GetGraphElement resourceId} must be declared as either data range or class expression {GetResourceInfoForErrorMessage resourceId}"
                
    (* These are called whenever setting CE, OPE, DPE, DR and AP
        These cannot be redefined, as this is an error *)
    let trySetClassExpression x expression =
        if ClassExpressions.ContainsKey x then
            failwith $"Invalid ontology: class {resources.GetGraphElement x} defined twice"
        else
            ClassExpressions <- ClassExpressions.Add(x, expression)
    
    let trySetObjectPropertyExpression x expression =
        if ObjectPropertyExpressions.ContainsKey x then
            failwith $"Invalid ontology: object property {resources.GetGraphElement x} defined twice"
        else
            ObjectPropertyExpressions <- ObjectPropertyExpressions.Add(x, expression)
    let trySetDataPropertyExpression x expression =
        if DataPropertyExpressions.ContainsKey x then
            failwith $"Invalid ontology: data property {resources.GetGraphElement x} defined twice"
        else
            DataPropertyExpressions <- DataPropertyExpressions.Add(x, expression)
    
    let trySetDataRanges x expression =
        if DataRanges.ContainsKey x then
            failwith $"Invalid ontology: data range {resources.GetGraphElement x} defined twice"
        else
            DataRanges <- DataRanges.Add(x, expression)
        
    
    let trySetAnnotationProperties x expression =
        if AnnotationProperties.ContainsKey x then
            failwith $"Invalid ontology: annotation property {resources.GetGraphElement x} defined twice"
        else
            AnnotationProperties <- AnnotationProperties.Add(x, expression)

    let containsOwlAxiom (triple : Triple) =
        Seq.contains triple.predicate  [ owlIntersectionOfId; owlUnionOfId; owlComplementOfId; owlOneOfId]
    
    (* This is called to prepare the class-expressions for topological sorting when propResourceId can only be a class
       The function returns true if the resource is not yet defined and is not an iri *)
    let GetPredecessorClassExpressions propResourceId =
        match ClassExpressions.TryGetValue propResourceId with
        | true, prop -> false
        | false, _ -> if ((DataRanges.ContainsKey propResourceId)) then 
                            failwith $"Invalid OWL ontology: Property {resources.GetGraphElement propResourceId} used both as a data range and a class"
                      else
                          match (resources.GetResource propResourceId) with
                          | Some (RdfResource.Iri iri) -> false
                          | Some (AnonymousBlankNode bn) -> true
                          | None -> failwith $"Invalid OWL Ontology: {resources.GetGraphElement propResourceId} used as a class"
    
    
    (* First four rows of Table 13, Class Expressions in https://www.w3.org/TR/owl2-mapping-to-rdf *)        
    member internal  this.parseAnonymousClassExpressions() =
        let anonymousClassTriples = tripleTable.GetTriplesWithObjectPredicate(owlClassId, rdfTypeId)
                                            |> Seq.choose (fun tr -> match resources.GetResource(tr.subject) with
                                                                                    | Some (AnonymousBlankNode x) -> Some (tr.subject)
                                                                                    | _ -> None)
                                            |> Seq.map tripleTable.GetTriplesWithSubject
                                            |> Seq.choose (fun trs -> trs |> Seq.filter containsOwlAxiom
                                                                        |> fun t -> match t |> Seq.toList with
                                                                                    | [tr] -> Some tr
                                                                                    | [] ->  failwith $"Probably invalid or pointless construct in ontology: Anonymous class without expression : {GetResourceInfoForErrorMessage ((trs |> Seq.head).subject)} "
                                                                                    | _ -> failwith $"Invalid owl ontology: Anonymous class expression defined multiple times: {GetResourceInfoForErrorMessage ((trs |> Seq.head).subject)} "
                                                                                    )                                
        anonymousClassTriples
        |> Seq.map (fun classTriple -> 
            let x = classTriple.subject
            let owlAxiomName = (RequireResourceIri classTriple.predicate).ToString()
            let ys = match owlAxiomName with
                        | OwlComplementOf -> [classTriple.obj]
                        | _ -> Ingress.GetRdfListElements tripleTable resources classTriple.obj 
            (x, ys,
             fun () ->
                    let classExpression =
                        match owlAxiomName  with 
                        | OwlIntersectionOf ->
                            let yClassExpressions = ys |> List.map tryGetClassExpressions
                            ObjectIntersectionOf yClassExpressions
                        | OwlUnionOf -> 
                            let yClassExpressions = ys |> List.map tryGetClassExpressions
                            ObjectUnionOf yClassExpressions
                        | OwlComplementOf -> 
                            let y = classTriple.obj
                            let yClassExpression = tryGetClassExpressions y
                            ObjectComplementOf yClassExpression  
                        | OwlOneOf -> 
                            let ystars = ys
                                         |> List.map resources.GetGraphElement
                                         |> List.map Ingress.tryGetIndividual
                            ObjectOneOf ystars
                        | _x -> failwith $"Invalid OWL ontology: Unknown predicate {_x} used in class expression."
                    trySetClassExpression x classExpression
                    )
            )
    
    (* Assumes the restrictionTriples is a set of triples describing an OWL cardinality constraint
        Returns the integer in the constraint
     *)
    member internal this.RequireQualificationCardinality (restrictionTriples : Triple seq) cardinalityId =
        let n_opt = restrictionTriples
                            |> Seq.find (fun tr -> tr.predicate = cardinalityId)
                            |> (_.obj)
                            |> resources.GetGraphElement
                            |> Ingress.tryGetNonNegativeIntegerLiteral
        match n_opt with 
                | Some nn -> nn                              
                | None -> failwith $"Invalid OWL cardinality integer constraint"
  
    (* 
        Assumes the set of triples is 
         _:x rdf:type owl:Restriction .
        _:x owl:onProperty y .
        _:x owl:someValuesFrom z .
        { *PE(y) ≠ ε and CE(z) ≠ ε }  
            
        Sets CE(_:x) to *SomeValuesFrom( *PE(y) CE(z) )
        
        Where * is O(bject) or D(ata)   
    *)
    member internal this.parseSomeValueFrom (restrictionTriples : Triple seq) =
        let x = restrictionTriples |> Seq.head |> (_.subject)
        let y = restrictionTriples |> Seq.find (fun tr -> tr.predicate = owlOnPropertyId) |> (_.obj)
        let z = restrictionTriples |> Seq.find (fun tr -> tr.predicate = owlSomeValueFromId) |> (_.obj)
        (x, [y; z], fun () ->
        let objectSomeValuesFromCreator yExpr = ObjectSomeValuesFrom(yExpr, tryGetClassExpressions z)
        let dataSomeValuesFromCreator yExpr = DataSomeValuesFrom([yExpr], tryGetDataRange z)
        let restriction = RequireObjectOrDataPropDeclaration y objectSomeValuesFromCreator dataSomeValuesFromCreator
        trySetClassExpression x restriction)

    (* 
        Assumes the set of triples is 
        _:x rdf:type owl:Restriction .
        _:x owl:onProperty y .
        _:x owl:allValuesFrom z .
        { *PE(y) ≠ ε and CE/DR(z) ≠ ε } 
            
        Sets CE(_:x) to  *AllValuesFrom( *PE(y) CE(z) )    
    *)
    member internal this.parseAllValuesFrom (restrictionTriples : Triple seq) =
        let x = restrictionTriples |> Seq.head |> (_.subject)
        let y = restrictionTriples |> Seq.find (fun tr -> tr.predicate = owlOnPropertyId) |> (_.obj)
        let z = restrictionTriples |> Seq.find (fun tr -> tr.predicate = owlAllValuesFromId) |> (_.obj)
        (x, [y; z], fun () ->
        let objectSomeValuesFromCreator yExpr = ObjectAllValuesFrom(yExpr, tryGetClassExpressions z)
        let dataSomeValuesFromCreator yExpr = DataAllValuesFrom([yExpr], tryGetDataRange z)
        let restriction = RequireObjectOrDataPropDeclaration y objectSomeValuesFromCreator dataSomeValuesFromCreator
        trySetClassExpression x restriction)
    
    
    (* 
        Assumes the set of triples is 
        _:x rdf:type owl:Restriction .
        _:x owl:onProperties T(SEQ y1 ... yn) .
        _:x owl:XValuesFrom z .
        { n ≥ 1, DPE(yi) ≠ ε for each 1 ≤ i ≤ n, and DR(z) ≠ ε }
            
        Sets CE(_:x) to   DataXValuesFrom( DPE(y1) ... DPE(yn) DR(z) ))
        
        WHre X is all or xome    
    *)
    member internal this.parseValuesFromProperties (dataValuesFromConstructor : DataProperty list * DataRange -> ClassExpression) (restrictionTriples : Triple seq) =
        let x = restrictionTriples |> Seq.head |> (_.subject)
        let predecessors = restrictionTriples |> Seq.find (fun tr -> tr.predicate = owlOnPropertiesId)
                           |> (_.obj)
                           |> Ingress.GetRdfListElements tripleTable resources
        let unhandledPredecessors = predecessors |> Seq.filter GetPredecessorClassExpressions
        (x, predecessors, fun () ->
        let ys = predecessors |> List.map tryGetDataPropertyExpressions
        let z = restrictionTriples |> Seq.find (fun tr -> tr.predicate = owlAllValuesFromId) |> (_.obj)
        let restriction = dataValuesFromConstructor(ys, tryGetDataRange z)
        trySetClassExpression x restriction)
    
    
    
    (* 
        Assumes the set of triples is 
        _:x rdf:type owl:Restriction .
        _:x owl:onProperties T(SEQ y1 ... yn) .
        _:x owl:allValuesFrom z .
        { n ≥ 1, DPE(yi) ≠ ε for each 1 ≤ i ≤ n, and DR(z) ≠ ε }
            
        Sets CE(_:x) to   DataAllValuesFrom( DPE(y1) ... DPE(yn) DR(z) ))    
    *)
    member internal this.parseAllValuesFromProperties = this.parseValuesFromProperties DataAllValuesFrom
    
    
    (* 
        Assumes the set of triples is 
        _:x rdf:type owl:Restriction .
        _:x owl:onProperties T(SEQ y1 ... yn) .
        _:x owl:someValueFrom z .
        { n ≥ 1, DPE(yi) ≠ ε for each 1 ≤ i ≤ n, and DR(z) ≠ ε }
            
        Sets CE(_:x) to   DataAllValuesFrom( DPE(y1) ... DPE(yn) DR(z) ))    
    *)
    member internal this. parseSomeValueFromProperties = this.parseValuesFromProperties DataSomeValuesFrom
    
    (* 
        Assumes the set of triples is 
        _:x rdf:type owl:Restriction .
        _:x owl:onProperty y .
        _:x owl:hasValue *:z .
        { *PE(y) ≠ ε }
            
        Sets CE(_:x) to  *HasValue( *PE(y) *:z )  
        where *PE is either OPE or DPE and *HasValue is ObjectHasValue or DataHasValue   
    *)
    member internal this.parseHasValue (restrictionTriples : Triple seq) =
        let x = restrictionTriples |> Seq.head |> (_.subject)
        let y = restrictionTriples |> Seq.find (fun tr -> tr.predicate = owlOnPropertyId) |> (_.obj)
        let z = restrictionTriples
                |> Seq.find (fun tr -> tr.predicate = owlHasValueId)
                |> (_.obj)
                |> resources.GetGraphElement
        let objectHasValueCreator yExpr = ObjectHasValue(yExpr, Ingress.tryGetIndividual z)
        let dataHasValueCreator yExpr = DataHasValue(yExpr, z)
        let restriction = RequireObjectOrDataPropDeclaration y objectHasValueCreator dataHasValueCreator
        (x, [], fun () ->
        trySetClassExpression x restriction)
        
    (* 
        Assumes the set of triples is 
       _:x rdf:type owl:Restriction .
        _:x owl:onProperty y .
        _:x owl:hasSelf "true"^^xsd:boolean .
        { OPE(y) ≠ ε } 
            
        Sets CE(_:x) to  ObjectHasSelf( OPE(y) )      
    *)
    member internal this.parseObjectHasSelf (restrictionTriples : Triple seq) =
        let x = restrictionTriples |> Seq.head |> (_.subject)
        let y = restrictionTriples |> Seq.find (fun tr -> tr.predicate = owlOnPropertyId) |> (_.obj)
        (x, [y], fun () ->
        let has_self = restrictionTriples
                       |> Seq.find (fun tr -> tr.predicate = owlHasSelfId)
                       |> (_.obj)
                       |> resources.GetGraphElement
                       |> Ingress.tryGetBoolLiteral
                       |> Option.get
        if has_self then
            let OPE_y = tryGetObjectPropertyExpressions y
            let restriction = ObjectHasSelf(OPE_y)
            trySetClassExpression x restriction)


        (* 
        Assumes the set of triples is an rdf representation of OwlMinQualifiedCardinality  on an object property like this:
     _:x rdf:type owl:Restriction .
    _:x <some cardinality iri> NN_INT(n) .
    _:x owl:onProperty y .
    _:x owl:onClass z .
    { OPE(y) ≠ ε and CE(z) ≠ ε } 
        
        Sets CE(_:x) to SeomCardinality( n OPE(y) CE(z) )  
    *)
    member internal this.parseQualifiedCardinality
        tryGetProperty tryGetObjectType qualifierPropertyId
        (restrictionTriples : Triple seq) (cardinalityQualifierResourceId : GraphElementId)  (cardinalityExpressionConstructor : (int * 'a *'b) -> ClassExpression) =
        let x = restrictionTriples |> Seq.head |> (_.subject)
        let y = restrictionTriples |> Seq.find (fun tr -> tr.predicate = owlOnPropertyId) |> (_.obj)
        let z = restrictionTriples |> Seq.find (fun tr -> tr.predicate = qualifierPropertyId) |> (_.obj)
        (x, [y; z], fun () ->
        let n = this.RequireQualificationCardinality restrictionTriples cardinalityQualifierResourceId
        let OPE_y = tryGetProperty y
        let CE_z = tryGetObjectType z
        let restriction = cardinalityExpressionConstructor(n, OPE_y, CE_z)
        trySetClassExpression x restriction)

    member internal this.parseObjectQualifiedCardinality (restrictionTriples : Triple seq) cardinalityQualifierResourceId (cardinalityExpressionConstructor)  =
        this.parseQualifiedCardinality tryGetObjectPropertyExpressions tryGetClassExpressions owlOnClassId restrictionTriples cardinalityQualifierResourceId cardinalityExpressionConstructor
    member internal this.parseDataQualifiedCardinality (restrictionTriples : Triple seq) cardinalityQualifierResourceId  (cardinalityExpressionConstructor) =
        this.parseQualifiedCardinality tryGetDataPropertyExpressions tryGetDataRange owlOnDataRangeId restrictionTriples cardinalityQualifierResourceId cardinalityExpressionConstructor
                        
    (* 
        Assumes the set of triples is an rdf representation of OwlMinQualifiedCardinality  on an object property like this:
     _:x rdf:type owl:Restriction .
    _:x owl:minQualifiedCardinality NN_INT(n) .
    _:x owl:onProperty y .
    _:x owl:onClass z .
    { OPE(y) ≠ ε and CE(z) ≠ ε } 
        
        Sets CE(_:x) to ObjectMinCardinality( n OPE(y) CE(z) )  
    *)
    member internal this.parseObjectMinQualifiedCardinality (restrictionTriples : Triple seq) =
        this.parseObjectQualifiedCardinality restrictionTriples owlMinQualifiedCardinalityId ObjectMinQualifiedCardinality

    (* 
        Assumes the set of triples is an rdf representation of OwlMaxQualifiedCardinality  on an object property like this:
     _:x rdf:type owl:Restriction .
    _:x owl:maxQualifiedCardinality NN_INT(n) .
    _:x owl:onProperty y .
    _:x owl:onClass z .
    { OPE(y) ≠ ε and CE(z) ≠ ε } 
        
        Sets CE(_:x) to ObjectMaxCardinality( n OPE(y) CE(z) )  
    *)
    member internal this.parseObjectMaxQualifiedCardinality (restrictionTriples : Triple seq)  =
        this.parseObjectQualifiedCardinality restrictionTriples owlMaxQualifiedCardinalityId ObjectMaxQualifiedCardinality
    (* 
        Assumes the set of triples is an rdf representation of OwlQualifiedCardinality  on an object property like this:
        _:x rdf:type owl:Restriction .
        _:x owl:qualifiedCardinality NN_INT(n) .
        _:x owl:onProperty y .
        _:x owl:onClass z . and { OPE(y) ≠ ε and CE(z) ≠ ε }
        
        Sets CE(_:x) to ObjectExactCardinality( n OPE(y) CE(z) ) 
    *)
    member internal this.parseObjectExactQualifiedCardinality (restrictionTriples : Triple seq) =
        this.parseObjectQualifiedCardinality restrictionTriples owlQualifiedCardinalityId ObjectExactQualifiedCardinality
    
    member internal this.parseObjectCardinality (restrictionTriples : Triple seq) cardinalityId cardinalityExpressionConstructor =
        let x = restrictionTriples |> Seq.head |> (_.subject)
        let y = restrictionTriples |> Seq.find (fun tr -> tr.predicate = owlOnPropertyId) |> (_.obj)
        (x, [y], fun () ->
        let n = this.RequireQualificationCardinality restrictionTriples cardinalityId
        let OPE_y = tryGetObjectPropertyExpressions y
        let restriction = cardinalityExpressionConstructor(n, OPE_y)
        trySetClassExpression x restriction)
    
    member internal this.parseObjectMinCardinality (restrictionTriples : Triple seq) =
        this.parseObjectCardinality restrictionTriples owlMinCardinalityId ObjectMinCardinality
                
    (* 
        Assumes the set of triples is an rdf representation of OwlMinCardinality  on an object property like this:
     _:x rdf:type owl:Restriction .
    _:x owl:maxCardinality NN_INT(n) .
    _:x owl:onProperty y .
    { OPE(y) ≠ ε } 
        
        Sets CE(_:x) to ObjectMaxCardinality( n OPE(y) )  
    *)
    member internal this.parseObjectMaxCardinality (restrictionTriples : Triple seq) =
        this.parseObjectCardinality restrictionTriples owlMaxCardinalityId ObjectMaxCardinality
                
    (* 
        Assumes the set of triples is an rdf representation of OwlMinCardinality  on an object property like this:
     _:x rdf:type owl:Restriction .
    _:x owl:cardinality NN_INT(n) .
    _:x owl:onProperty y .
    { OPE(y) ≠ ε } 
        
        Sets CE(_:x) to ObjectExactCardinality( n OPE(y) )  
    *)
    member internal this.parseObjectExactCardinality (restrictionTriples : Triple seq) =
        this.parseObjectCardinality  restrictionTriples owlCardinalityId ObjectExactCardinality
    
    (* 
        Assumes the set of triples is an rdf representation of OwlMinQualifiedCardinality  on an object property like this:
     _:x rdf:type owl:Restriction .
    _:x owl:minQualifiedCardinality NN_INT(n) .
    _:x owl:onProperty y .
    _:x owl:onDataRamge z .
    { DPE(y) ≠ ε and DR(z) ≠ ε } 
        
        Sets CE(_:x) to DataMinCardinality( n DPE(y) CE(z) )  
    *)
    member internal this.parseDataMinQualifiedCardinality (restrictionTriples : Triple seq) =
        this.parseDataQualifiedCardinality restrictionTriples owlMinQualifiedCardinalityId DataMinQualifiedCardinality

    (* 
        Assumes the set of triples is an rdf representation of OwlMaxQualifiedCardinality  on an object property like this:
     _:x rdf:type owl:Restriction .
    _:x owl:maxQualifiedCardinality NN_INT(n) .
    _:x owl:onProperty y .
    _:x owl:onDataRange z .
    { DPE(y) ≠ ε and DR(z) ≠ ε } 
        
        Sets CE(_:x) to DataMaxCardinality( n EPE(y) DR(z) )  
    *)
    member internal this.parseDataMaxQualifiedCardinality (restrictionTriples : Triple seq) =
        this.parseDataQualifiedCardinality restrictionTriples owlMaxQualifiedCardinalityId DataMaxQualifiedCardinality
        
    (* 
        Assumes the set of triples is an rdf representation of OwlQualifiedCardinality  on an object property like this:
     _:x rdf:type owl:Restriction .
    _:x owl:qualifiedCardinality NN_INT(n) .
    _:x owl:onProperty y .
    _:x owl:onDataRange z .
    { DPE(y) ≠ ε and DR(z) ≠ ε } 
        
        Sets CE(_:x) to DataMaxCardinality( n DPE(y) DR(z) )  
    *)
    member internal this.parseDataExactQualifiedCardinality (restrictionTriples : Triple seq) =
        this.parseDataQualifiedCardinality restrictionTriples owlQualifiedCardinalityId DataExactQualifiedCardinality
    
    (* This is called from the parseAnonymousTriples below, assuming that parseRestrictions is all the triples in a store with the same subject of type owl:Restriction  *)
    member internal this.parseRestrictions restrictionTriples predicates triplesString =
        
        if predicates |> Seq.contains OwlOnProperties then
                if (predicates |> Seq.contains OwlAllValuesFrom) then
                        (this.parseAllValuesFromProperties restrictionTriples)
                else if predicates |> Seq.contains OwlSomeValuesFrom then
                    (this.parseSomeValueFromProperties restrictionTriples)
                else failwith $"Invalid OWL ontology: OwlOnProperties without either OwlAllValuesFrom or OwlSomeValuesFrom"
            else if predicates |> Seq.contains OwlSomeValuesFrom then
                this.parseSomeValueFrom restrictionTriples
            else if predicates |> Seq.contains OwlAllValuesFrom then
                this.parseAllValuesFrom restrictionTriples
            else if predicates |> Seq.contains OwlHasValue then
                this.parseHasValue restrictionTriples
            else if predicates |> Seq.contains OwlHasSelf then
                this.parseObjectHasSelf restrictionTriples
            else if predicates |> Seq.contains OwlOnClass then
                if predicates |> Seq.contains OwlMaxQualifiedCardinality then
                    this.parseObjectMaxQualifiedCardinality restrictionTriples
                else if predicates |> Seq.contains OwlMinQualifiedCardinality then
                    this.parseObjectMinQualifiedCardinality restrictionTriples
                else if predicates |> Seq.contains OwlQualifiedCardinality then
                    this.parseObjectExactQualifiedCardinality restrictionTriples
                else failwith $"Invalid OWL Ontology: OwlOnClass without qualified cardinality specified"
            else if predicates |> Seq.contains OwlOnDataRange then
                if predicates |> Seq.contains OwlMaxQualifiedCardinality then
                    this.parseDataMaxQualifiedCardinality restrictionTriples
                else if predicates |> Seq.contains OwlMinQualifiedCardinality then
                    this.parseDataMinQualifiedCardinality restrictionTriples
                else if predicates |> Seq.contains OwlQualifiedCardinality then
                    this.parseDataExactQualifiedCardinality restrictionTriples
                else failwith $"Invalid OWL Ontology OwlOnDataRange without qualified cardingality specified"
            else if predicates |> Seq.contains OwlMinCardinality then
                    this.parseObjectMinCardinality restrictionTriples
            else if predicates |> Seq.contains OwlMaxCardinality then
                    this.parseObjectMaxCardinality restrictionTriples
            else if predicates |> Seq.contains OwlCardinality then
                    this.parseObjectExactCardinality restrictionTriples
            else if predicates |> Seq.contains OwlOnProperties then
                    failwith $"TODO: {triplesString} not implemented yet"
            else
                failwith $"Invalid owl:Restriction on triples {triplesString}"
                
                
    member internal this.parseAnonymousRestrictions() =
        let anonymousClassTriples = tripleTable.GetTriplesWithObjectPredicate(owlRestrictionId, rdfTypeId)
                                            |> Seq.choose (fun tr -> match resources.GetResource(tr.subject) with
                                                                                    | Some (AnonymousBlankNode x) -> Some (tr.subject)
                                                                                    | x -> failwith $"Invalid OWL Ontology: owl:Restriction cannot be used on {x}, only on blank nodes. ")
                                            |> Seq.map tripleTable.GetTriplesWithSubject
                                            |> Seq.map (fun trs -> trs |> Seq.filter (fun triple -> triple.predicate <> rdfTypeId))
                                                                                                   
        anonymousClassTriples
        |> Seq.map (fun restrictionTriples ->
            let predicates = restrictionTriples
                                |> Seq.map  (fun triple ->  triple.predicate
                                                                |> RequireResourceIri
                                                                |> _.ToString())
            (* For easier error messages *)
            let triplesString = restrictionTriples
                                |> Seq.map resources.GetResourceTriple
                                |> Seq.map (_.ToString()) |> String.concat "."
            
            this.parseRestrictions restrictionTriples predicates triplesString
            
            )
            
    
    member internal this.parseClassExpressions() =
        let unorderedClassExpressions = (Seq.concat [this.parseAnonymousClassExpressions() ; this.parseAnonymousRestrictions()])
                                        |> Seq.map (Monad.updateSecond (List.filter GetPredecessorClassExpressions))
        Ingress.sortClassExpressions unorderedClassExpressions
    
    member internal this.getAxiomParser() =
        this.parseClassExpressions()
        |> Seq.iter (fun clExpr -> clExpr())
        new AxiomParser(tripleTable,
                        resources,
                        tryGetClassExpressions,
                        tryGetDataRange,
                        tryGetObjectPropertyExpressions,
                        tryGetDataPropertyExpressions,
                        AnnotationProperties,
                        getAnnotations,
                        tryGetAnyPropertyAxiom,
                        tryGetPropertyDeclaration,
                        tryGetClassOrDataRangeAxiom)