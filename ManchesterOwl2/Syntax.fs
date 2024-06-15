namespace ManchesterOwl2
open IriTools
// Abstract representation of https://www.w3.org/TR/owl2-manchester-syntax/
module Syntax =

    type IRI =
        | FullIri of IriReference
        | AbbreviatedIri of string * string
        | SimpleIri of string

    type ClassIRI = IRI
    type DatatypeIRI = IRI
    type Datatype = DatatypeIRI | Integer | Decimal | Float | String
    type ObjectPropertyIRI = IRI
    type IndividualIRI = IRI
    type AnnotationPropertyIRI = IRI
    type NodeId = string
    type Individual =
        | IndividualIRI
        | BlankNode of NodeId
    type PrefixDeclaration = PrefixDeclaration of string * IRI
    type Ontology = 
        | Ontology of ClassIRI * (PrefixDeclaration list) * (string list) 
    type OntologyDocument = PrefixDeclaration * Ontology
type ClassFrame = 
        ClassIRI * (ClassIRI list)
    
    
    

