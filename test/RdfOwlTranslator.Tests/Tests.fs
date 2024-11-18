(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.RdfOwlTranslator.Tests

open System
open DagSemTools.Rdf
open OwlOntology.Axioms
open Xunit
open DagSemTools.Resource
open IriTools
open DagSemTools.Rdf.Ingress
open Faqt

module Tests =
    
    
    [<Theory>]
    [<InlineData(Namespaces.OwlClass)>]
    [<InlineData(Namespaces.OwlDatatypeProperty)>]
    [<InlineData(Namespaces.OwlObjectProperty)>]
    [<InlineData(Namespaces.OwlAnnotationProperty)>]
    [<InlineData(Namespaces.RdfsDatatype)>]
    let ``Class declarations can be parsed from triples`` (typeIri: String ) =
        //Arrange
        let tripleTable = TripleTable(100u)
        let resources = ResourceManager(100u)
        let subjectResource = resources.AddResource(Resource.Iri(new IriReference "http://example.com/subject"))
        let rdfTypeResource = resources.AddResource(Resource.Iri(new IriReference (Namespaces.RdfType)))
        let owlclassResource = resources.AddResource(Resource.Iri(new IriReference (typeIri)))
        let typeTriple : Triple = { subject = subjectResource
                                    predicate = rdfTypeResource
                                    obj = owlclassResource }
        tripleTable.AddTriple typeTriple
        let typeDeclaration = match typeIri with
                                | Namespaces.OwlClass -> ClassDeclaration
                                | Namespaces.OwlObjectProperty -> ObjectPropertyDeclaration
                                | Namespaces.OwlDatatypeProperty -> DataPropertyDeclaration
                                | Namespaces.OwlAnnotationProperty -> AnnotationPropertyDeclaration
                                | Namespaces.RdfsDatatype -> DatatypeDeclaration
                                | _ -> failwith $"Invalid inline data type {typeIri} given to test"
        let expectedAxiom = AxiomDeclaration ([],  typeDeclaration (Iri.FullIri (new IriReference "http://example.com/subject"))) 
        
        //Act
        let ontology : OwlOntology.Ontology.Ontology = DagSemTools.RdfOwlTranslator.Rdf2Owl.extractOntology tripleTable resources
        
        //Assert
        let ontologyAxioms = ontology.Axioms 
        ontologyAxioms.Should().Contain(expectedAxiom)
        
    
    [<Theory>]
    [<InlineData(Namespaces.OwlClass)>]
    [<InlineData(Namespaces.OwlDatatypeProperty)>]
    [<InlineData(Namespaces.OwlObjectProperty)>]
    [<InlineData(Namespaces.OwlAnnotationProperty)>]
    [<InlineData(Namespaces.RdfsDatatype)>]
    let ``Class declarations can be parsed from axiom triples`` (typeIri: String ) =
        //Arrange
        let tripleTable = TripleTable(100u)
        let resources = ResourceManager(100u)
        let subjectResource = resources.AddResource(Resource.Iri(new IriReference "http://example.com/subject"))
        let anonymousReificationResource = resources.AddResource(Resource.AnonymousBlankNode(10u))
        let rdfTypeResource = resources.AddResource(Resource.Iri(new IriReference (Namespaces.RdfType)))
        let owlAxiomResource = resources.AddResource(Resource.Iri(new IriReference (Namespaces.OwlAxiom)))
        let owlAnnTarget = resources.AddResource(Resource.Iri(new IriReference (Namespaces.OwlAnnotatedTarget)))
        let owlAnnSource = resources.AddResource(Resource.Iri(new IriReference (Namespaces.OwlAnnotatedSource)))
        let owlAnnProp = resources.AddResource(Resource.Iri(new IriReference (Namespaces.OwlAnnotatedProperty)))
        let owlclassResource = resources.AddResource(Resource.Iri(new IriReference (typeIri)))
        
        let typeTriple : Triple = { subject = anonymousReificationResource
                                    predicate = rdfTypeResource
                                    obj = owlAxiomResource }
        tripleTable.AddTriple typeTriple
        let targetTriple : Triple = { subject = anonymousReificationResource
                                      predicate = owlAnnTarget
                                      obj = owlclassResource }
        tripleTable.AddTriple targetTriple
        let propTriple : Triple = { subject = anonymousReificationResource
                                    predicate = owlAnnProp
                                    obj = rdfTypeResource }
        tripleTable.AddTriple propTriple
        let sourceTriple : Triple = { subject = anonymousReificationResource
                                      predicate = owlAnnSource
                                      obj = subjectResource }
        tripleTable.AddTriple sourceTriple
        
        let typeDeclaration = match typeIri with
                                | Namespaces.OwlClass -> ClassDeclaration
                                | Namespaces.OwlObjectProperty -> ObjectPropertyDeclaration
                                | Namespaces.OwlDatatypeProperty -> DataPropertyDeclaration
                                | Namespaces.OwlAnnotationProperty -> AnnotationPropertyDeclaration
                                | Namespaces.RdfsDatatype -> DatatypeDeclaration
                                | _ -> failwith $"Invalid inline data type {typeIri} given to test"
        let expectedAxiom = AxiomDeclaration ([],  typeDeclaration (Iri.FullIri (new IriReference "http://example.com/subject"))) 
        
        //Act
        let ontology : OwlOntology.Ontology.Ontology = DagSemTools.RdfOwlTranslator.Rdf2Owl.extractOntology tripleTable resources
        
        //Assert
        let ontologyAxioms = ontology.Axioms 
        ontologyAxioms.Should().Contain(expectedAxiom)
        
    
    [<Fact>]
    let ``Subclass Axiom can be parsed from triples`` () =
        //Arrange
        let tripleTable = TripleTable(100u)
        let resources = ResourceManager(100u)
        let subjectResource = resources.AddResource(Resource.Iri(new IriReference "http://example.com/subject"))
        let rdfTypeResource = resources.AddResource(Resource.Iri(new IriReference (Namespaces.RdfType)))
        let subclassResource = resources.AddResource(Resource.Iri(new IriReference "http://example.com/subclass"))
        let typeTriple : Triple = { subject = subjectResource
                                    predicate = rdfTypeResource
                                    obj = subclassResource }
        tripleTable.AddTriple typeTriple
        let subClassOfRelation = resources.AddResource(Resource.Iri(new IriReference (Namespaces.RdfsSubClassOf)))
        let superclassResource = resources.AddResource(Resource.Iri(new IriReference "http://example.com/superclass"))
        let subClassTriple : Triple = { subject = subclassResource
                                        predicate = subClassOfRelation
                                        obj = superclassResource }
        tripleTable.AddTriple subClassTriple
        
        //Act
        let ontology : OwlOntology.Ontology.Ontology = DagSemTools.RdfOwlTranslator.Rdf2Owl.extractOntology tripleTable resources
        
        //Assert
        let subClass : OwlOntology.Axioms.ClassExpression = OwlOntology.Axioms.ClassName (OwlOntology.Axioms.Iri.FullIri (new IriReference "http://example.com/subclass"))
        let superClass : OwlOntology.Axioms.ClassExpression = OwlOntology.Axioms.ClassName (OwlOntology.Axioms.Iri.FullIri (new IriReference "http://example.com/superclass"))
        let expectedAxioms = OwlOntology.Axioms.AxiomClassAxiom ( OwlOntology.Axioms.SubClassOf ([], subClass, superClass) )
        let ontologyAxioms = ontology.Axioms |> List.where (fun ax -> match ax with
                                                                            | Axiom.AxiomClassAxiom _ -> true
                                                                            | _ -> false) 
        ontologyAxioms.Should().Contain(expectedAxioms)
        
