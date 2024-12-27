(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
module DagSemTools.OWL2RL2Datalog.TestPropertyAxioms

open DagSemTools
open DagSemTools.OwlOntology
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
let ``Object Property Domain and Range RL reasoning works`` () =
    let tripleTable = new Datastore(100u)
    let errorOutput = new System.IO.StringWriter()
    
    let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
    let objectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
    let propertyIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/property"))
    let rdfTypeIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.RdfType)))
    let objPropTypeIndex = tripleTable.AddResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.OwlObjectProperty)))
    let rangeIndex = tripleTable.AddResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/rangeClass"))
    let domainIndex = tripleTable.AddResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/domainClass"))
    let owlDomainId = tripleTable.AddResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.RdfsDomain)))
    let owlRangeId = tripleTable.AddResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.RdfsRange)))
    let objPropTypeTriple = {Ingress.Triple.subject = propertyIndex; predicate = rdfTypeIndex; obj = objPropTypeIndex}
    tripleTable.AddTriple(objPropTypeTriple)
    let domainTriple = {Triple.subject = propertyIndex; predicate = owlDomainId; obj = domainIndex}
    tripleTable.AddTriple(domainTriple)
    let rangeTriple = {Triple.subject = propertyIndex; predicate = owlRangeId; obj = rangeIndex}
    tripleTable.AddTriple(rangeTriple)
    let objPropAssertion = {Triple.subject = subjectIndex; predicate = propertyIndex; obj = objectIndex}
    tripleTable.AddTriple(objPropAssertion)
    let query = tripleTable.GetTriplesWithSubjectObject(subjectIndex, domainIndex)
    query.Should().HaveLength(0) |> ignore
    let query = tripleTable.GetTriplesWithSubjectObject(objectIndex, rangeIndex)
    query.Should().HaveLength(0) |> ignore
    
    let ontologyTranslator = new RdfOwlTranslator.Rdf2Owl(tripleTable.Triples, tripleTable.Resources)
    let ontology = ontologyTranslator.extractOntology
    let rlProgram = Library.owl2Datalog logger tripleTable.Resources ontology 
    DagSemTools.Datalog.Reasoner.evaluate (rlProgram |> Seq.toList, tripleTable)
    
    let query2 = tripleTable.GetTriplesWithSubjectObject(subjectIndex, domainIndex)
    query2.Should().HaveLength(1) |> ignore
    let query3 = tripleTable.GetTriplesWithSubjectObject(objectIndex, rangeIndex)
    query3.Should().HaveLength(1) |> ignore
    inMemorySink.LogEvents.Should().BeEmpty
