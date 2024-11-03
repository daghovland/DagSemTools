module Tests

open System
open DagSemTools.Rdf
open DagSemTools.Datalog
open DagSemTools.Rdf.Ingress
open DagSemTools.OWL2RL2Datalog
open IriTools
open Xunit
open Faqt

[<Fact>]
let ``Equality RL adds equality axioms`` () =
    let tripleTable = new Datastore(100u)
    let errorOutput = new System.IO.StringWriter()
    
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference (Namespaces.OwlSameAs)))
    let subjectIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject2"))
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = subjectIndex2}
    tripleTable.AddTriple(Triple)
    let query = tripleTable.GetTriplesWithObject(subjectIndex2)
    query.Should().HaveLength(1) |> ignore
    let rlProgram = Reasoner.enableEqualityReasoning tripleTable [] errorOutput
    DagSemTools.Datalog.Reasoner.evaluate (rlProgram |> Seq.toList, tripleTable)
    let query2 = tripleTable.GetTriplesWithObject(subjectIndex2)
    query2.Should().HaveLength(2) |> ignore
    let query3 = tripleTable.GetTriplesWithPredicate(predIndex)
    query3.Should().HaveLength(1) |> ignore
    
[<Fact>]
let ``Equality RL reasoning works`` () =
    let tripleTable = new Datastore(100u)
    let errorOutput = new System.IO.StringWriter()
    
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let sameAsIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference (Namespaces.OwlSameAs)))
    let subjectIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject2"))
    let objIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
    let SameAsTriple = {Ingress.Triple.subject = subjectIndex; predicate = sameAsIndex; obj = subjectIndex2}
    let contentTriple = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objIndex}
    tripleTable.AddTriple(SameAsTriple)
    tripleTable.AddTriple(contentTriple)
    let query = tripleTable.GetTriplesWithObject(objIndex)
    query.Should().HaveLength(1) |> ignore
    let rlProgram = Reasoner.enableEqualityReasoning tripleTable [] errorOutput
    DagSemTools.Datalog.Reasoner.evaluate (rlProgram |> Seq.toList, tripleTable)
    let query2 = tripleTable.GetTriplesWithObject(objIndex)
    query2.Should().HaveLength(2) |> ignore
    let query3 = tripleTable.GetTriplesWithPredicate(predIndex)
    query3.Should().HaveLength(2) |> ignore