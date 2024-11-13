module TestClassAxioms

open DagSemTools
open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.OWL2RL2Datalog
open IriTools
open Xunit
open Faqt

[<Fact>]
let ``Subclass RL reasoning works`` () =
    let tripleTable = new Datastore(100u)
    let errorOutput = new System.IO.StringWriter()
    
    let subjectIndex = tripleTable.AddResource(Resource.Resource.Iri(new IriReference "http://example.com/subject"))
    let rdfTypeIndex = tripleTable.AddResource(Resource.Resource.Iri(new IriReference (Namespaces.RdfType)))
    let objIndex = tripleTable.AddResource(Resource.Resource.Iri(new IriReference "http://example.com/object"))
    let subClassIndex = tripleTable.AddResource(Resource.Resource.Iri(new IriReference (Namespaces.RdfsSubClassOf)))
    let objIndex2 = tripleTable.AddResource(Resource.Resource.Iri(new IriReference "http://example.com/object2"))
    let contentTriple = {Ingress.Triple.subject = subjectIndex; predicate = rdfTypeIndex; obj = objIndex}
    tripleTable.AddTriple(contentTriple)
    let subClassTriple = {Triple.subject = objIndex; predicate = subClassIndex; obj = objIndex2}
    tripleTable.AddTriple(subClassTriple)
    let query = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex)
    query.Should().HaveLength(1) |> ignore
    let query = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex2)
    query.Should().HaveLength(0) |> ignore
    
    let rlProgram = Reasoner.enableOwlReasoning tripleTable [] errorOutput
    DagSemTools.Datalog.Reasoner.evaluate (rlProgram |> Seq.toList, tripleTable)
    let query2 = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex2)
    query2.Should().HaveLength(1) |> ignore
    
