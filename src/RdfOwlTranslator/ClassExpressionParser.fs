namespace DagSemTools.RdfOwlTranslator

open DagSemTools.AlcTableau.NNF
open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.Resource
open DagSemTools.OwlOntology
open DagSemTools.Resource.Namespaces
open IriTools


(* These functions correspond to table 13 in https://www.w3.org/TR/owl2-mapping-to-rdf/#Parsing_of_Axioms 
    Assumes the IRI-based declarations CE, DR, etc. are already made
    This table / function only handles anonymous nodes/blank IRIs
*)
type ClassExpressionParser (triples : TripleTable,
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
    
    
    (* This is called  when propResourceId can only be an object property
       The function returns either the declared object property, or if it is not already a data or annotaiton property,
        a simple named declaration
        This is not completely per spec, but many ontologies around do not specify all properties *)
    let tryGetObjectPropertyExpressions propResourceId =
        match ObjectPropertyExpressions.TryGetValue propResourceId with
        | true, prop -> prop
        | false, _ -> if ((DataPropertyExpressions.ContainsKey propResourceId) || (AnnotationProperties.ContainsKey propResourceId)) then 
                            failwith $"Invalid OWL ontology: Property {resources.GetResource propResourceId} used both as object property and either data or annotation property"
                      else
                          match (resources.GetResource propResourceId) with
                          | Resource.Iri iri -> NamedObjectProperty (FullIri iri)
                          | Resource.AnonymousBlankNode bn -> AnonymousObjectProperty bn
    
    (* This is called  when propResourceId can only be a class
       The function returns either the declared class, or if it is not already a datarange,
        a simple named declaration
        This is not completely per spec, but many ontologies around do not specify all properties *)
    let tryGetObjectPropertyExpressions propResourceId =
        match ObjectPropertyExpressions.TryGetValue propResourceId with
        | true, prop -> prop
        | false, _ -> if ((DataPropertyExpressions.ContainsKey propResourceId) || (AnnotationProperties.ContainsKey propResourceId)) then 
                            failwith $"Invalid OWL ontology: Property {resources.GetResource propResourceId} used both as object property and either data or annotation property"
                      else
                          NamedObjectProperty (FullIri (tryGetResourceIri propResourceId))
                          // TODO HANDLE BLANK NODE!
    
                        
    let parseAnonymousClassExpressions() =
        let anonymousClassTriples = tripleTable.GetTriplesWithObjectPredicate(owlClassId, rdfTypeId)
                                            |> Seq.choose (fun tr -> match resources.GetResource(tr.subject) with
                                                                                    | AnonymousBlankNode x -> Some (tr.subject)
                                                                                    | _ -> None)
                                            |> Seq.map tripleTable.GetTriplesWithSubject
                                            |> Seq.map (fun trs -> trs |> Seq.filter (fun triple -> triple.predicate <> rdfTypeId)
                                                                        |> fun t -> match t |> Seq.toList with
                                                                                    | [tr] -> tr
                                                                                    | _ -> failwith $"Probably invalid or pointless construct in ontology: Anonymous class {resources.GetResource((trs |> Seq.head).subject)} without restriction"
                                                                                    )                                
        for classTriple in anonymousClassTriples do
            match (tryGetResourceIri classTriple.predicate).ToString()  with 
                    | Namespaces.OwlIntersectionOf -> failwith "TODO: Owl Union not implemented yet"
                    | Namespaces.OwlUnionOf -> failwith "TODO: Owl Union not implemented yet"
                    | Namespaces.OwlComplementOf -> failwith "TODO: Owl Union not implemented yet"
                    | Namespaces.OwlOneOf -> failwith "TODO: Owl Union not implemented yet"
                    | x -> failwith $"Invalid OWL ontology: Unknown predicate {x} used in class expression."
    
    (* 
        Assumes the set of triples is an rdf representation of OwlQualifiedCardinality  on an object property like this:
        _:x rdf:type owl:Restriction .
        _:x owl:qualifiedCardinality NN_INT(n) .
        _:x owl:onProperty y .
        _:x owl:onClass z . and { OPE(y) ≠ ε and CE(z) ≠ ε }
    *)
    let parseObjectQualifiedCardinality (restrictionTriples : Triple seq) =
        let y = restrictionTriples |> Seq.find (fun tr -> tr.predicate = owlOnPropertyId) |> (_.obj)
        let z = restrictionTriples |> Seq.find (fun tr -> tr.predicate = owlOnClassId) |> (_.obj)
        let n = match (restrictionTriples |> Seq.find (fun tr -> tr.predicate = owlQualifiedCardinalityId) |> (_.obj) |> resources.GetResource) with
                    | Resource.IntegerLiteral nn -> nn
                    | Resource.TypedLiteral (tp, nn) when (List.contains (tp.ToString()) [XsdInt ; XsdInteger; XsdNonNegativeInteger] ) -> nn |> int                              
                    | x -> failwith $"Invalid OWL Qualified cardinality integer constraint {x}"
        let OPE_y = tryGetObjectPropertyExpressions y
        let CE_z = ClassExpressions.[z]
        let restriction = ObjectExactCardinality(n, OPE_y, CE_z)
        if ClassExpressions.ContainsKey z then
            failwith $"Invalid ontology: class {resources.GetResource z} defined twice"
        else
            ClassExpressions <- ClassExpressions.Add(z, restriction)
                    
        
    let parseAnonymousRestrictions() =
        let anonymousClassTriples = tripleTable.GetTriplesWithObjectPredicate(owlRestrictionId, rdfTypeId)
                                            |> Seq.choose (fun tr -> match resources.GetResource(tr.subject) with
                                                                                    | AnonymousBlankNode x -> Some (tr.subject)
                                                                                    | x -> failwith $"Invalid OWL Ontology: owl:Restriction cannot be used on {x}, only on blank nodes. ")
                                            |> Seq.map tripleTable.GetTriplesWithSubject
                                            |> Seq.map (fun trs -> trs |> Seq.filter (fun triple -> triple.predicate <> rdfTypeId))
                                                                                                   
        for restrictionTriples in anonymousClassTriples do
            let predicates = restrictionTriples
                                |> Seq.map  (fun triple ->  triple.predicate
                                                                |> tryGetResourceIri
                                                                |> _.ToString())
            (* For easier error messages *)
            let triplesString = restrictionTriples
                                |> Seq.map resources.GetResourceTriple
                                |> Seq.map (_.ToString()) |> String.concat "."
                                
            if predicates |> Seq.contains OwlMaxQualifiedCardinality then
                    if predicates |> Seq.contains OwlOnDataRange then
                        failwith $"TODO: DataMaxCardinality not implemented yet"
                    else if predicates |> Seq.contains OwlOnClass then
                        failwith $"TODO: ObjectMaxCardinality not implemented yet"
                    else
                        failwith "Invalid MaxQualifiedCardinality. Must specify class or data range"
            else if predicates |> Seq.contains OwlMinQualifiedCardinality then
                    failwith $"TODO: {triplesString} not implemented yet"
            else if predicates |> Seq.contains OwlQualifiedCardinality then
                    if predicates |> Seq.contains OwlOnDataRange then
                        failwith $"TODO: DataMaxCardinality not implemented yet"
                    else if predicates |> Seq.contains OwlOnClass then
                        parseObjectQualifiedCardinality(restrictionTriples)
                    else
                        failwith "Invalid MaxQualifiedCardinality. Must specify class or data range"
                    
            else if predicates |> Seq.contains OwlMinCardinality then
                    failwith $"TODO: {triplesString} not implemented yet"
            else if predicates |> Seq.contains OwlMaxCardinality then
                    failwith $"TODO: {triplesString} not implemented yet"
            else if predicates |> Seq.contains OwlCardinality then
                    failwith $"TODO: {triplesString} not implemented yet"
            else if predicates |> Seq.contains OwlHasSelf then
                    failwith $"TODO: {triplesString} not implemented yet"
            else if predicates |> Seq.contains OwlHasValue then
                    failwith $"TODO: {triplesString} not implemented yet"
            else if predicates |> Seq.contains OwlOnProperties then
                    failwith $"TODO: {triplesString} not implemented yet"
            else if predicates |> Seq.contains OwlSomeValuesFrom then
                    failwith $"TODO: {triplesString} not implemented yet"
            else if predicates |> Seq.contains OwlAllValuesFrom then
                    failwith $"TODO: {triplesString} not implemented yet"
            else
                failwith $"Invalid owl:Restriction on triples {triplesString}"
                    
    let parseClassExpressions() =
        parseAnonymousClassExpressions()
        parseAnonymousRestrictions()
    
    
    member internal this.getAxiomParser() =
        parseClassExpressions()
        new AxiomParser(tripleTable,
                        resources,
                        ClassExpressions,
                        DataRanges,
                        ObjectPropertyExpressions,
                        DataPropertyExpressions,
                        AnnotationProperties,
                        Annotations)