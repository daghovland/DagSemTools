namespace DagSemTools.RdfOwlTranslator

open System.Resources
open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.Resource
open IriTools
open OwlOntology
open OwlOntology.Axioms
open OwlOntology.Ontology

module Ingress =
    
    let createSubClassAxiom subclass superclass = 
        ClassAxiom.SubClassOf ([], (ClassName subclass), (ClassName superclass))
    
    
    let getTypeDeclarator entityTypeIri: 'a -> Entity =
        match entityTypeIri.ToString() with
        | Namespaces.OwlDatatypeProperty -> DataPropertyDeclaration
        | Namespaces.OwlObjectProperty -> ObjectPropertyDeclaration
        | Namespaces.RdfsDatatype -> DatatypeDeclaration
        | Namespaces.OwlClass -> ClassDeclaration
        | Namespaces.OwlAnnotationProperty -> AnnotationPropertyDeclaration
        | Namespaces.OwlNamedIndividual -> (fun indIri -> NamedIndividualDeclaration (NamedIndividual indIri))
        | _ -> failwith $"BUG: no declaration for iri {entityTypeIri}"
    
    