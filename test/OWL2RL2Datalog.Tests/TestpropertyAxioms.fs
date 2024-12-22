module TestPropertyAxioms

open DagSemTools
open DagSemTools.OwlOntology
open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.OWL2RL2Datalog
open DagSemTools.Ingress
open IriTools
open Xunit
open Faqt

[<Fact>]
let ``Object Property Domain and Range RL reasoning works`` () =
    let tripleTable = new Datastore(100u)
    let errorOutput = new System.IO.StringWriter()
    
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let objectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
    let propertyIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/property"))
    let rdfTypeIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference (Namespaces.RdfType)))
    let objPropTypeIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference (Namespaces.OwlObjectProperty)))
    let rangeIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/rangeClass"))
    let domainIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/domainClass"))
    let owlDomainId = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference (Namespaces.RdfsDomain)))
    let owlRangeId = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference (Namespaces.RdfsRange)))
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
    let rlProgram = Library.owl2Datalog tripleTable.Resources ontology errorOutput
    DagSemTools.Datalog.Reasoner.evaluate (rlProgram |> Seq.toList, tripleTable)
    
    let query2 = tripleTable.GetTriplesWithSubjectObject(subjectIndex, domainIndex)
    query2.Should().HaveLength(1) |> ignore
    let query3 = tripleTable.GetTriplesWithSubjectObject(objectIndex, rangeIndex)
    query3.Should().HaveLength(1) |> ignore
    
