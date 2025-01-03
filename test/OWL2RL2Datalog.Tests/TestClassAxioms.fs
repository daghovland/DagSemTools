(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
module DagSemTools.OWL2RL2Datalog.TestClassAxioms

open DagSemTools
open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.OWL2RL2Datalog
open DagSemTools.Ingress
open IriTools
open Xunit
open Faqt
open Serilog
open Serilog.Sinks.InMemory

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
    
    let ontologyTranslator = new RdfOwlTranslator.Rdf2Owl(tripleTable.Triples, tripleTable.Resources)
    let ontology = ontologyTranslator.extractOntology
    let rlProgram = Library.owl2Datalog logger tripleTable.Resources ontology
    DagSemTools.Datalog.Reasoner.evaluate (rlProgram |> Seq.toList, tripleTable)
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
    
    let ontologyTranslator = new RdfOwlTranslator.Rdf2Owl(tripleTable.Triples, tripleTable.Resources)
    let ontology = ontologyTranslator.extractOntology
    let rlProgram = Library.owl2Datalog logger tripleTable.Resources ontology
    DagSemTools.Datalog.Reasoner.evaluate (rlProgram |> Seq.toList, tripleTable)
    let query2 = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex2)
    query2.Should().HaveLength(1) |> ignore


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
    
    let ontologyTranslator = new RdfOwlTranslator.Rdf2Owl(tripleTable.Triples, tripleTable.Resources)
    let ontology = ontologyTranslator.extractOntology
    let rlProgram = Library.owl2Datalog logger tripleTable.Resources ontology
    DagSemTools.Datalog.Reasoner.evaluate (rlProgram |> Seq.toList, tripleTable)
    let query2 = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex)
    query2.Should().HaveLength(1) |> ignore
    inMemorySink.LogEvents.Should().BeEmpty