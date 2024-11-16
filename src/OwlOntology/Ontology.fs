module OwlOntology.Ontology
open Axioms
open IriTools
open OwlOntology.Axioms

type ontologyVersion =
        | UnNamedOntology
        | NamedOntology of OntologyIri: IriReference
        | VersionedOntology of OntologyIri: IriReference * OntologyVersionIri: IriReference

type directlyImportsDocument = IriReference

    

type Ontology(directlyImportsDocuments: directlyImportsDocument list, version: ontologyVersion, annotations: Annotation list, axioms: Axiom list) =

    let OWL2DatatypeMap = [
                "http://www.w3.org/2002/07/owl#real"
                "http://www.w3.org/2002/07/owl#rational"
                "http://www.w3.org/2001/XMLSchema#decimal"
                "http://www.w3.org/2001/XMLSchema#integer"
                "http://www.w3.org/2001/XMLSchema#nonNegativeInteger"
                "http://www.w3.org/2001/XMLSchema#nonPositiveInteger"
                "http://www.w3.org/2001/XMLSchema#positiveInteger"
                "http://www.w3.org/2001/XMLSchema#negativeInteger"
                "http://www.w3.org/2001/XMLSchema#long"
                "http://www.w3.org/2001/XMLSchema#int"
                "http://www.w3.org/2001/XMLSchema#short"
                "http://www.w3.org/2001/XMLSchema#byte"
                "http://www.w3.org/2001/XMLSchema#unsignedLong"
                "http://www.w3.org/2001/XMLSchema#unsignedInt"
                "http://www.w3.org/2001/XMLSchema#unsignedShort"
                "http://www.w3.org/2001/XMLSchema#unsignedByte "
            ]
    let builtInAnnotationProperties = [
        "http://www.w3.org/2000/01/rdf-schema#label"
        "http://www.w3.org/2000/01/rdf-schema#comment"
        "http://www.w3.org/2000/01/rdf-schema#seeAlso"
        "http://www.w3.org/2000/01/rdf-schema#isDefinedBy"
        "http://www.w3.org/2002/07/owl#deprecated"
        "http://www.w3.org/2002/07/owl#versionInfo"
        "http://www.w3.org/2002/07/owl#priorVersion"
        "http://www.w3.org/2002/07/owl#backwardCompatibleWith"
        "http://www.w3.org/2002/07/owl#incompatibleWith"
    ]
            
    let BuiltInDeclarations : Declaration list =
        let staticBuiltIns = [
            Declaration ([],ClassDeclaration (Class.FullIri (IriReference "http://www.w3.org/2002/07/owl#Thing")))
            Declaration ([],ClassDeclaration (Class.FullIri (IriReference "http://www.w3.org/2002/07/owl#Nothing")))
            Declaration ([],ObjectPropertyDeclaration (Class.FullIri (IriReference "http://www.w3.org/2002/07/owl#topObjectProperty")))
            Declaration ([],ObjectPropertyDeclaration (Class.FullIri (IriReference "http://www.w3.org/2002/07/owl#bottomObjectProperty")))
            Declaration ([],DataPropertyDeclaration (Class.FullIri (IriReference "http://www.w3.org/2002/07/owl#bottomDataProperty")))
            Declaration ([],DataPropertyDeclaration (Class.FullIri (IriReference "http://www.w3.org/2002/07/owl#topDataProperty")))
            Declaration ([],DatatypeDeclaration (Class.FullIri (IriReference "http://www.w3.org/2000/01/rdf-schema#Literal")))
            Declaration ([],AnnotationPropertyDeclaration (Class.FullIri (IriReference "TODO:: All built in annotation properties")))
        ]
        let datatypeBuiltIns = OWL2DatatypeMap |> List.map (fun x -> Declaration ([],DatatypeDeclaration (Class.FullIri (IriReference x))))
        let annotationBuiltIns = builtInAnnotationProperties |> List.map (fun x -> Declaration ([],AnnotationPropertyDeclaration (Class.FullIri (IriReference x))))
        List.concat [staticBuiltIns; datatypeBuiltIns; annotationBuiltIns]
                                                                  
        
    member this.DirectlyImportsDocuments = directlyImportsDocuments
    member this.Annotations = annotations
    
    member this.Axioms = List.concat [axioms; BuiltInDeclarations |> List.map Axiom.AxiomDeclaration] 
    member this.Version = version
    
    member this.TryGetOntologyVersionIri() =
        match this.Version with
        | NamedOntology iri -> null
        | VersionedOntology (_, iri) -> iri
        | _ -> null
    member this.TryGetOntologyIri() =
            match this.Version with
            | NamedOntology iri -> iri
            | VersionedOntology (iri, _) -> iri
            | _ -> null
            
type prefixDeclaration =
        | PrefixDefinition of PrefixName: string * PrefixIri: IriReference
type prefixDeclaration with
        member x.TryGetPrefixName() =
            match x with
            | PrefixDefinition (name, iri) -> (name, iri)
            
type OntologyDocument(prefixes: prefixDeclaration list, ontology: Ontology) =
    member this.Prefixes = prefixes
    member this.Ontology = ontology
    member this.TryGetOntologyVersionIri() = this.Ontology.TryGetOntologyVersionIri()
    member this.TryGetOntologyIri() = this.Ontology.TryGetOntologyIri()
    
            
    