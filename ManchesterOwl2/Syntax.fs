namespace ManchesterOwl2
open IriTools
// Abstract representation of https://www.w3.org/TR/owl2-manchester-syntax/
module Syntax =

    type IRI =
        | FullIri of IriReference
        | AbbreviatedIri of string * string
        | SimpleIri of string

    type Datatype = IRI | Integer | Decimal | Float | String
    type NodeId = string
    type Individual =
        | IRI
        | BlankNode of NodeId
        
    type ObjectPropertyExpression =
        | ObjectPropertyIRI of IRI
        | InverseObjectProperty of IRI
    type atomic =
        | Class of IRI
        | InidividualList of IRI list
        
    type restriction =
        | ExistentialRestriction of restriction
        | UniversalRestriction of restriction
        | NegativeRestriction of restriction
        | AtomicRestriction of atomic
    type conjunction = IRI * restriction list
    type description = ConjunctionList of  conjunction list
    type PrefixDeclaration = PrefixDeclaration of string * IRI
    type ClassFrameElement =
        SubClassOf of (IRI list)
        | EquivalentTo of (IRI list)
        | DisjointWith of IRI
        | DisjointUnion of IRI list
        | HasKey of IRI * (IRI list)
        
    type ClassFrame = 
        IRI * (ClassFrameElement list)
    
    type Ontology = 
        | Ontology of IRI * (PrefixDeclaration list) * (ClassFrame list) 
    
    type OntologyDocument = PrefixDeclaration * Ontology
    
    

