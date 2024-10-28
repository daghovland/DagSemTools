module Tests

open System
open DagSemTools.AlcTableau.ALC
open Xunit
open DagSemTools.DL2Datalog
open DagSemTools.Rdf
open IriTools
open DagSemTools.Rdf.Ingress
open DagSemTools.AlcTableau
open Faqt



[<Fact>]
let ``Simplest owl axiom to datalog`` () =
    let tripleTable = new DagSemTools.Rdf.Datastore(new DagSemTools.Rdf.TripleTable(10u), new DagSemTools.Rdf.QuadTable(10u), new DagSemTools.Rdf.QuadTable(10u), new DagSemTools.Rdf.ResourceManager(10u))
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference (Namespaces.RdfType)))
    let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.org/concept1"))
    let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.org/concept2"))
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
    tripleTable.AddTriple(Triple)

    let concept1 = ALC.ConceptName(IriTools.IriReference("http://example.org/concept1"))
    let concept2 = ALC.ConceptName(IriTools.IriReference("http://example.org/concept2"))
    let subclass_assertion = ALC.Inclusion(concept1, concept2)
    let ontologyVersion = ALC.ontologyVersion.UnNamedOntology
     
    let kb = ([subclass_assertion], [])
    let ontology = OntologyDocument.Ontology ([], ontologyVersion, kb)
    let dlprog =  Translator.Ontology2Rules tripleTable.Resources ontology
    
    
    let query = tripleTable.GetTriplesWithObject(objdIndex2)
    query.Should().BeEmpty() |> ignore
    DagSemTools.Datalog.Reasoner.evaluate (dlprog |> Seq.toList, tripleTable)
    let query2 = tripleTable.GetTriplesWithSubject(objdIndex2)
    query2.Should().NotBeEmpty()