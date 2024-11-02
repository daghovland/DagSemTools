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
let ``Equality RL reasoning works`` () =
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
    query3.Should().HaveLength(2) |> ignore