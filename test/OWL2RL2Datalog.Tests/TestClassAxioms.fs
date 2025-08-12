(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.OWL2RL2Datalog.Tests

open System.Numerics
open DagSemTools.Rdf.Query

module TestClassAxioms =

    open DagSemTools
    open DagSemTools.AlcTableau.ALC
    open DagSemTools.Datalog
    open DagSemTools.Rdf
    open DagSemTools.Rdf.Ingress
    open DagSemTools.OWL2RL2Datalog
    open DagSemTools.OwlOntology
    open DagSemTools.Ingress
    open IriTools
    open Xunit
    open Faqt
    open Serilog
    open Serilog.Sinks.InMemory
    open DagSemTools.ELI.ELIExtractor

    let inMemorySink = new InMemorySink()
    let logger =
        LoggerConfiguration()
                .WriteTo.Sink(inMemorySink)
                .CreateLogger()
        

    [<Fact>]
    let ``Subclass RL reasoning from rdf works`` () =
        let tripleTable = new Datastore(100u)
        let errorOutput = new System.IO.StringWriter()
        
        let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
        let rdfTypeIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.RdfType)))
        let objIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
        let subClassIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.RdfsSubClassOf)))
        let objIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object2"))
        let contentTriple = {Ingress.Triple.subject = subjectIndex; predicate = rdfTypeIndex; obj = objIndex}
        tripleTable.AddTriple(contentTriple)
        let subClassTriple = {Triple.subject = objIndex; predicate = subClassIndex; obj = objIndex2}
        tripleTable.AddTriple(subClassTriple)
        let query = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex)
        query.Should().HaveLength(1) |> ignore
        let query = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex2)
        query.Should().HaveLength(0) |> ignore
        
        let ontologyTranslator = new RdfOwlTranslator.Rdf2Owl(tripleTable.Triples, tripleTable.Resources, logger)
        let ontology = ontologyTranslator.extractOntology
        let rlProgram = Library.owl2Datalog logger tripleTable.Resources ontology.Ontology
        DagSemTools.Datalog.Reasoner.evaluate (logger, rlProgram |> Seq.toList, tripleTable)
        let query2 = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex2)
        query2.Should().HaveLength(1) |> ignore
        inMemorySink.LogEvents.Should().BeEmpty
        
    [<Fact>]
    let ``Equivalentclass RL reasoning from rdf works`` () =
        let tripleTable = new Datastore(100u)
        let errorOutput = new System.IO.StringWriter()
        
        let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
        let rdfTypeIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.RdfType)))
        let objIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
        let eqClassIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.OwlEquivalentClass)))
        let classTypeIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.OwlClass)))
        let objIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object2"))
        let class1Triple = {Ingress.Triple.subject = objIndex; predicate = rdfTypeIndex; obj = classTypeIndex}
        tripleTable.AddTriple(class1Triple)
        let class2Triple = {Ingress.Triple.subject = objIndex2; predicate = rdfTypeIndex; obj = classTypeIndex}
        tripleTable.AddTriple(class2Triple)
        
        
        let contentTriple = {Ingress.Triple.subject = subjectIndex; predicate = rdfTypeIndex; obj = objIndex}
        tripleTable.AddTriple(contentTriple)
        let subClassTriple = {Triple.subject = objIndex; predicate = eqClassIndex; obj = objIndex2}
        tripleTable.AddTriple(subClassTriple)
        let query = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex)
        query.Should().HaveLength(1) |> ignore
        let query = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex2)
        query.Should().HaveLength(0) |> ignore
        
        let ontologyTranslator = new RdfOwlTranslator.Rdf2Owl(tripleTable.Triples, tripleTable.Resources, logger)
        let ontology = ontologyTranslator.extractOntology
        let rlProgram = Library.owl2Datalog logger tripleTable.Resources ontology.Ontology
        DagSemTools.Datalog.Reasoner.evaluate (logger, rlProgram |> Seq.toList, tripleTable)
        let query2 = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex2)
        query2.Should().HaveLength(1) |> ignore

    //Checks the axiom:
    // >= 1 t (E U F) subClassOf A 
    [<Fact>]
    let ``Min qualified cardinality on union works``() =
        // Arrange
        let tripleTable = new Datastore(100u)
        let errorOutput = new System.IO.StringWriter()
        
        let Airi = IriReference "https://example.com/class/A"
        let A = ClassName (FullIri Airi)
        let Eiri = IriReference "https://example.com/class/E"
        let E = ClassName (FullIri Eiri)
        let Firi = IriReference "https://example.com/class/F"
        let F = ClassName (FullIri Firi)
        
        let union = ObjectUnionOf [E; F]
        let roleIri = IriReference "https://example.com/property/t"
        let role = NamedObjectProperty (FullIri roleIri)
        let restriction = ObjectMinQualifiedCardinality(BigInteger(1), role, union)
        let classAxiom = (SubClassOf ([], restriction, A))
        // Act
        let eliAxioms = SubClassAxiomNormalization logger classAxiom
        let rlProgram = ELI.ELI2RL.GenerateTBoxRL logger tripleTable.Resources eliAxioms 
        
        // Assert
        let Aresource = tripleTable.Resources.AddResource (NodeOrEdge (Iri Airi))
        let Eresource = tripleTable.Resources.AddResource (NodeOrEdge (Iri Eiri))
        let Fresource = tripleTable.Resources.AddResource (NodeOrEdge (Iri Firi))
        let roleresource = tripleTable.Resources.AddResource (NodeOrEdge (Iri roleIri))
        
        let rdfTypeResource = tripleTable.Resources.AddResource (NodeOrEdge (Iri (IriReference Namespaces.RdfType)))
        let ruleHead =
            NormalHead {Subject = Term.Variable "X"
                        Predicate = Term.Resource rdfTypeResource
                        Object = Term.Resource Aresource}
        
        let Arules = rlProgram |> Seq.filter (fun rule -> rule.Head = ruleHead)
        Arules.Should().NotBeEmpty() |> ignore
        
    //Checks the axiom:
    // >= 1 t (E and F) subClassOf A 
    [<Fact>]
    let ``Min qualified cardinality on intersection works``() =
        // Arrange
        let tripleTable = new Datastore(100u)
        let errorOutput = new System.IO.StringWriter()
        
        let Airi = IriReference "https://example.com/class/A"
        let A = ClassName (FullIri Airi)
        let Eiri = IriReference "https://example.com/class/E"
        let E = ClassName (FullIri Eiri)
        let Firi = IriReference "https://example.com/class/F"
        let F = ClassName (FullIri Firi)
        
        let intersection = ObjectIntersectionOf [E; F]
        let roleIri = IriReference "https://example.com/property/t"
        let role = NamedObjectProperty (FullIri roleIri)
        let restriction = ObjectMinQualifiedCardinality(BigInteger(1), role, intersection)
        let subClassAxiom = AxiomClassAxiom (SubClassOf ([], restriction, A))
        let ontology = OwlOntology.Ontology([], ontologyVersion.UnNamedOntology,[], [subClassAxiom])
        // Act
        let rlProgram = Library.owl2Datalog logger tripleTable.Resources ontology
        
        // Assert
        let Aresource = tripleTable.Resources.AddResource (NodeOrEdge (Iri Airi))
        let Eresource = tripleTable.Resources.AddResource (NodeOrEdge (Iri Eiri))
        let Fresource = tripleTable.Resources.AddResource (NodeOrEdge (Iri Firi))
        let roleresource = tripleTable.Resources.AddResource (NodeOrEdge (Iri roleIri))
        
        let rdfTypeResource = tripleTable.Resources.AddResource (NodeOrEdge (Iri (IriReference Namespaces.RdfType)))
        let ruleHead =
            NormalHead {Subject = Term.Variable "X"
                        Predicate = Term.Resource rdfTypeResource
                        Object = Term.Resource Aresource}
        let expectedAxiom = {
            DagSemTools.Datalog.Head = ruleHead
            DagSemTools.Datalog.Body = [
                PositiveTriple {
                    Subject = Term.Variable "X"
                    Predicate = Term.Resource roleresource
                    Object = Term.Variable "X_1"
                };
                PositiveTriple{
                 Subject = Term.Variable "X_1"
                 Predicate = Term.Resource rdfTypeResource
                 Object = Term.Resource Fresource
                 };
                PositiveTriple{
                 Subject = Term.Variable "X_1"
                 Predicate = Term.Resource rdfTypeResource
                 Object = Term.Resource Eresource
                 }]
        }
        let Arules = rlProgram |> Seq.filter (fun rule -> rule.Head = ruleHead)
        Arules.Should().NotBeEmpty() |> ignore
        Arules.Should().Contain(expectedAxiom)
        
    [<Fact>]
    let ``MaxQualifiedCardinality is ignored correctly``() =
        // Arrange
        let tripleTable = new Datastore(100u)
        let errorOutput = new System.IO.StringWriter()
        let aspectClassIri = new IriReference $"http://ns.imfid.org/imf#Aspect"
        let hasCharacteristic = IriReference "http://ns.imfid.org/imf#hasCharacteristic"
        let imfInterestIri = IriReference "http://ns.imfid.org/imf#Interest"
        let imfInformationDomainIri = new IriReference "http://ns.imfid.org/imf#InformationDomain"
        let imfModalityIri = new IriReference "http://ns.imfid.org/imf#Modality"
        let axioms = [
            AxiomClassAxiom(
                SubClassOf(
                    [],
                    ClassName(Iri.FullIri(aspectClassIri)),
                    ObjectMaxQualifiedCardinality (BigInteger(1),
                                                   (NamedObjectProperty (Iri.FullIri hasCharacteristic)),
                                                   ClassName(Iri.FullIri imfInterestIri))
                )
            )
            
         ]
        let ontology = Ontology([], ontologyVersion.UnNamedOntology,[], axioms)
        
        // Act
        let rlProgram = Library.owl2Datalog logger tripleTable.Resources ontology
        
        //Assert
        let allRulesHaveSingleAtom = 
            rlProgram 
            |> Seq.forall (fun rule -> rule.Body.Length = 1)
        allRulesHaveSingleAtom.Should().BeTrue("There should only be the default rules with one body element, nothing with actual logic") |> ignore
        
    [<Fact>]
    let ``Equivalentclass RL reasoning from rdf works the other way`` () =
        let tripleTable = new Datastore(100u)
        let errorOutput = new System.IO.StringWriter()
        
        let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
        let rdfTypeIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.RdfType)))
        let objIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
        let eqClassIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.OwlEquivalentClass)))
        let objIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object2"))
        let classTypeIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.OwlClass)))
        let contentTriple = {Ingress.Triple.subject = subjectIndex; predicate = rdfTypeIndex; obj = objIndex2}
        let class1Triple = {Ingress.Triple.subject = objIndex; predicate = rdfTypeIndex; obj = classTypeIndex}
        tripleTable.AddTriple(class1Triple)
        let class2Triple = {Ingress.Triple.subject = objIndex2; predicate = rdfTypeIndex; obj = classTypeIndex}
        tripleTable.AddTriple(class2Triple)
        tripleTable.AddTriple(contentTriple)
        let subClassTriple = {Triple.subject = objIndex; predicate = eqClassIndex; obj = objIndex2}
        tripleTable.AddTriple(subClassTriple)
        let query = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex)
        query.Should().HaveLength(0) |> ignore
        let query = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex2)
        query.Should().HaveLength(1) |> ignore
        
        let ontologyTranslator = new RdfOwlTranslator.Rdf2Owl(tripleTable.Triples, tripleTable.Resources, logger)
        let ontology = ontologyTranslator.extractOntology
        let rlProgram = Library.owl2Datalog logger tripleTable.Resources ontology.Ontology
        DagSemTools.Datalog.Reasoner.evaluate (logger, rlProgram |> Seq.toList, tripleTable)
        let query2 = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex)
        query2.Should().HaveLength(1) |> ignore
        inMemorySink.LogEvents.Should().BeEmpty