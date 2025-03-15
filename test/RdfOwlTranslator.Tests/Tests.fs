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
open DagSemTools.OwlOntology
open Xunit
open DagSemTools.Ingress
open IriTools
open DagSemTools.Rdf.Ingress
open Faqt
open Serilog
open Serilog.Sinks.InMemory

module Tests =
    let inMemorySink = new InMemorySink()
    let logger =
        LoggerConfiguration()
                .WriteTo.Sink(inMemorySink)
                .CreateLogger()
    

    [<Theory>]
    [<InlineData(Namespaces.OwlClass)>]
    [<InlineData(Namespaces.OwlDatatypeProperty)>]
    [<InlineData(Namespaces.OwlObjectProperty)>]
    [<InlineData(Namespaces.OwlAnnotationProperty)>]
    [<InlineData(Namespaces.RdfsDatatype)>]
    let ``Class declarations can be parsed from triples`` (typeIri: String) =
        //Arrange
        let tripleTable = TripleTable(100u)
        let resources = GraphElementManager(100u)

        let subjectResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference "http://example.com/subject"))

        let rdfTypeResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfType)))

        let owlclassResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(typeIri)))

        let typeTriple: Triple =
            { subject = subjectResource
              predicate = rdfTypeResource
              obj = owlclassResource }

        tripleTable.AddTriple typeTriple

        let typeDeclaration =
            match typeIri with
            | Namespaces.OwlClass -> ClassDeclaration
            | Namespaces.OwlObjectProperty -> ObjectPropertyDeclaration
            | Namespaces.OwlDatatypeProperty -> DataPropertyDeclaration
            | Namespaces.OwlAnnotationProperty -> AnnotationPropertyDeclaration
            | Namespaces.RdfsDatatype -> DatatypeDeclaration
            | _ -> failwith $"Invalid inline data type {typeIri} given to test"

        let expectedAxiom =
            AxiomDeclaration([], typeDeclaration (Iri.FullIri(new IriReference "http://example.com/subject")))

        //Act
        let translator = new DagSemTools.RdfOwlTranslator.Rdf2Owl(tripleTable, resources, logger)
        let ontology = translator.extractOntology
        //Assert
        let ontologyAxioms = ontology.Ontology.Axioms
        ontologyAxioms.Should().Contain(expectedAxiom)


    let ``Create axiom-based Declaration Helper`` (typeIri: string) =
        let tripleTable = TripleTable(100u)
        let resources = GraphElementManager(100u)

        let subjectResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference $"{typeIri}_instance"))

        let anonymousReificationResource = resources.CreateUnnamedAnonResource()

        let rdfTypeResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfType)))

        let owlAxiomResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.OwlAxiom)))

        let owlAnnTarget =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.OwlAnnotatedTarget)))

        let owlAnnSource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.OwlAnnotatedSource)))

        let owlAnnProp =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.OwlAnnotatedProperty)))

        let owlclassResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(typeIri)))

        let typeTriple: Triple =
            { subject = anonymousReificationResource
              predicate = rdfTypeResource
              obj = owlAxiomResource }

        tripleTable.AddTriple typeTriple

        let propTriple: Triple =
            { subject = anonymousReificationResource
              predicate = owlAnnProp
              obj = rdfTypeResource }

        tripleTable.AddTriple propTriple

        let sourceTriple: Triple =
            { subject = anonymousReificationResource
              predicate = owlAnnSource
              obj = subjectResource }

        tripleTable.AddTriple sourceTriple

        let targetTriple: Triple =
            { subject = anonymousReificationResource
              predicate = owlAnnTarget
              obj = owlclassResource }

        tripleTable.AddTriple targetTriple

        (tripleTable, resources, subjectResource)


    [<Fact>]
    let ``SubClass declarations can be parsed from axiom triples`` () =
        //Arrange
        let owlClassIri = Namespaces.OwlClass

        let (tripleTable, resources, subjectResource) =
            ``Create axiom-based Declaration Helper`` owlClassIri

        let superClassResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference "http://example.com/superclass"))

        let subClassOfResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfsSubClassOf)))

        let subclassOfTriple: Triple =
            { subject = subjectResource
              predicate = subClassOfResource
              obj = superClassResource }

        tripleTable.AddTriple(subclassOfTriple)

        //Act
        let translator = new DagSemTools.RdfOwlTranslator.Rdf2Owl(tripleTable, resources, logger)
        let ontology = translator.extractOntology

        //Assert
        let expectedAxiom =
            AxiomClassAxiom(
                SubClassOf(
                    [],
                    ClassName(Iri.FullIri(new IriReference $"{owlClassIri}_instance")),
                    ClassName(Iri.FullIri(new IriReference "http://example.com/superclass"))
                )
            )

        let ontologyAxioms =
            ontology.Ontology.Axioms
            |> Seq.filter (fun ax ->
                match ax with
                | AxiomClassAxiom x -> true
                | _ -> false)

        ontologyAxioms.Should().Contain(expectedAxiom)

    [<Fact>]
    let ``SubProperty declarations can be parsed from axiom triples`` () =
        //Arrange
        let (tripleTable, resources, subjectResource) =
            ``Create axiom-based Declaration Helper`` Namespaces.OwlObjectProperty

        let superPropertyResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference "http://example.com/superclass"))

        let subClassOfResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfsSubPropertyOf)))

        let rdfTypeResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfType)))

        let objectPropResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.OwlObjectProperty)))

        let superPropertyDeclarationTriple: Triple =
            { subject = superPropertyResource
              predicate = rdfTypeResource
              obj = objectPropResource }

        tripleTable.AddTriple(superPropertyDeclarationTriple)

        let subclassOfTriple: Triple =
            { subject = subjectResource
              predicate = subClassOfResource
              obj = superPropertyResource }

        tripleTable.AddTriple(subclassOfTriple)

        //Act
        let translator = new DagSemTools.RdfOwlTranslator.Rdf2Owl(tripleTable, resources, logger)
        let ontology = translator.extractOntology

        //Assert
        let expectedAxiom =
            AxiomObjectPropertyAxiom(
                SubObjectPropertyOf(
                    [],
                    SubObjectPropertyExpression(
                        NamedObjectProperty(Iri.FullIri(new IriReference $"{Namespaces.OwlObjectProperty}_instance"))
                    ),
                    NamedObjectProperty(Iri.FullIri(new IriReference "http://example.com/superclass"))
                )
            )

        let ontologyAxioms =
            ontology.Ontology.Axioms
            |> Seq.filter (fun ax ->
                match ax with
                | AxiomObjectPropertyAxiom x -> true
                | _ -> false)

        ontologyAxioms.Should().Contain(expectedAxiom)


    [<Fact>]
    let ``Subclass Axiom can be parsed from triples`` () =
        //Arrange
        let tripleTable = TripleTable(100u)
        let resources = GraphElementManager(100u)

        let subjectResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference "http://example.com/subject"))

        let rdfTypeResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfType)))

        let subclassResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference "http://example.com/subclass"))

        let typeTriple: Triple =
            { subject = subjectResource
              predicate = rdfTypeResource
              obj = subclassResource }

        tripleTable.AddTriple typeTriple

        let subClassOfRelation =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfsSubClassOf)))

        let superclassResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference "http://example.com/superclass"))

        let subClassTriple: Triple =
            { subject = subclassResource
              predicate = subClassOfRelation
              obj = superclassResource }

        tripleTable.AddTriple subClassTriple

        //Act
        let translator = new DagSemTools.RdfOwlTranslator.Rdf2Owl(tripleTable, resources, logger)
        let ontology = translator.extractOntology

        //Assert
        let subClass: ClassExpression =
            ClassName(Iri.FullIri(new IriReference "http://example.com/subclass"))

        let superClass: ClassExpression =
            ClassName(Iri.FullIri(new IriReference "http://example.com/superclass"))

        let expectedAxioms = AxiomClassAxiom(SubClassOf([], subClass, superClass))

        let ontologyAxioms =
            ontology.Ontology.Axioms
            |> Seq.where (fun ax ->
                match ax with
                | Axiom.AxiomClassAxiom _ -> true
                | _ -> false)

        ontologyAxioms.Should().Contain(expectedAxioms)

    
    [<Fact>]
    let ``Sorting of class expressions works`` () =
        //Arrange
        let tripleTable = TripleTable(100u)
        let resources = GraphElementManager(100u)
        let superClassNode = resources.CreateUnnamedAnonResource()
        let subClassOfRelation =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfsSubClassOf)))
        let rdfTypeRelation =
                    resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfType)))
        let owlClassRelation =
                    resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.OwlClass)))
        let owlIntersectionRelation =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.OwlIntersectionOf)))

        let subclassResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference "http://example.com/subclass"))
        let superClassResource1 =
            resources.AddNodeResource(RdfResource.Iri(new IriReference "http://example.com/superclass1"))
        let superClassResource2 =
            resources.AddNodeResource(RdfResource.Iri(new IriReference "http://example.com/superclass2"))

        
        
        let subClassTriple: Triple =
            { subject = subclassResource
              predicate = subClassOfRelation
              obj = superClassNode }

        tripleTable.AddTriple subClassTriple

        let owlClassTriple: Triple =
            { subject = superClassNode
              predicate = rdfTypeRelation
              obj = owlClassRelation }

        tripleTable.AddTriple owlClassTriple
        
        let intersectionListHead = resources.CreateUnnamedAnonResource()
        let owlIntersectionTriple: Triple =
                    { subject = superClassNode
                      predicate = owlIntersectionRelation
                      obj = intersectionListHead }

        tripleTable.AddTriple owlIntersectionTriple
        
        let rdfHeadId =
                    resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfFirst)))
        
        let owlIntersectionheadTriple: Triple =
                    { subject = intersectionListHead
                      predicate = rdfHeadId
                      obj = superClassResource1 }

        tripleTable.AddTriple owlIntersectionheadTriple
        
        let intersectionListRest = resources.CreateUnnamedAnonResource()
        let rdfRestId =
                    resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfRest)))
        let owlIntersectionRestTriple: Triple =
                    { subject = intersectionListHead
                      predicate = rdfRestId
                      obj = intersectionListRest }

        tripleTable.AddTriple owlIntersectionRestTriple
        
        let owlIntersectionSecondTriple: Triple =
                    { subject = intersectionListRest
                      predicate = rdfHeadId
                      obj = superClassResource2 }
        tripleTable.AddTriple owlIntersectionSecondTriple
        
        let rdfNilId =
                    resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfNil)))
        let owlIntersectionLastTriple: Triple =
                    { subject = intersectionListRest
                      predicate = rdfRestId
                      obj = rdfNilId }

        tripleTable.AddTriple owlIntersectionLastTriple
        
        //Act
        let translator = new DagSemTools.RdfOwlTranslator.Rdf2Owl(tripleTable, resources, logger)
        let classExpressionParser = new DagSemTools.RdfOwlTranslator.ClassExpressionParser(tripleTable, resources, logger)
        let anonExpr = classExpressionParser.parseAnonymousClassExpressions()
        anonExpr.Should().HaveLength(1) |> ignore
        let restrExpr = classExpressionParser.parseAnonymousRestrictions()
        restrExpr.Should().HaveLength(0) |> ignore
        let orderedExpr = classExpressionParser.parseClassExpressions()
        orderedExpr.Should().HaveLength(1)
        
    [<Fact>]
    let ``Subclass of intersection Axiom can be parsed from triples`` () =
        //Arrange
        let tripleTable = TripleTable(100u)
        let resources = GraphElementManager(100u)
        let superClassNode = resources.CreateUnnamedAnonResource()
        let subClassOfRelation =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfsSubClassOf)))
        let rdfTypeRelation =
                    resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfType)))
        let owlClassRelation =
                    resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.OwlClass)))
        let owlIntersectionRelation =
            resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.OwlIntersectionOf)))

        let subclassResource =
            resources.AddNodeResource(RdfResource.Iri(new IriReference "http://example.com/subclass"))
        let superClassResource1 =
            resources.AddNodeResource(RdfResource.Iri(new IriReference "http://example.com/superclass1"))
        let superClassResource2 =
            resources.AddNodeResource(RdfResource.Iri(new IriReference "http://example.com/superclass2"))

        
        
        let subClassTriple: Triple =
            { subject = subclassResource
              predicate = subClassOfRelation
              obj = superClassNode }

        tripleTable.AddTriple subClassTriple

        let owlClassTriple: Triple =
            { subject = superClassNode
              predicate = rdfTypeRelation
              obj = owlClassRelation }

        tripleTable.AddTriple owlClassTriple
        
        let intersectionListHead = resources.CreateUnnamedAnonResource()
        let owlIntersectionTriple: Triple =
                    { subject = superClassNode
                      predicate = owlIntersectionRelation
                      obj = intersectionListHead }

        tripleTable.AddTriple owlIntersectionTriple
        
        let rdfHeadId =
                    resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfFirst)))
        
        let owlIntersectionheadTriple: Triple =
                    { subject = intersectionListHead
                      predicate = rdfHeadId
                      obj = superClassResource1 }

        tripleTable.AddTriple owlIntersectionheadTriple
        
        let intersectionListRest = resources.CreateUnnamedAnonResource()
        let rdfRestId =
                    resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfRest)))
        let owlIntersectionRestTriple: Triple =
                    { subject = intersectionListHead
                      predicate = rdfRestId
                      obj = intersectionListRest }

        tripleTable.AddTriple owlIntersectionRestTriple
        
        let owlIntersectionSecondTriple: Triple =
                    { subject = intersectionListRest
                      predicate = rdfHeadId
                      obj = superClassResource2 }
        tripleTable.AddTriple owlIntersectionSecondTriple
        
        let rdfNilId =
                    resources.AddNodeResource(RdfResource.Iri(new IriReference(Namespaces.RdfNil)))
        let owlIntersectionLastTriple: Triple =
                    { subject = intersectionListRest
                      predicate = rdfRestId
                      obj = rdfNilId }

        tripleTable.AddTriple owlIntersectionLastTriple
        
        
        //Act
        let translator = new DagSemTools.RdfOwlTranslator.Rdf2Owl(tripleTable, resources, logger)
        let ontology = translator.extractOntology

        //Assert
        let subClass: ClassExpression =
            ClassName(Iri.FullIri(new IriReference "http://example.com/subclass"))

        let superClass1: ClassExpression =
            ClassName(Iri.FullIri(new IriReference "http://example.com/superclass2"))
        let superClass2: ClassExpression =
                    ClassName(Iri.FullIri(new IriReference "http://example.com/superclass1"))

        let intersection : ClassExpression =
            ObjectIntersectionOf [superClass1; superClass2]
        let expectedAxioms = AxiomClassAxiom(SubClassOf([], subClass, intersection))

        let ontologyAxioms =
            ontology.Ontology.Axioms
            |> Seq.where (fun ax ->
                match ax with
                | Axiom.AxiomClassAxiom _ -> true
                | _ -> false)

        ontologyAxioms.Should().Contain(expectedAxioms)
