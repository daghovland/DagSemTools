module TestClassAxioms

open System
open DagSemTools.Datalog.Reasoner
open DagSemTools.Rdf
open DagSemTools.Datalog
open DagSemTools.Rdf.Ingress
open DagSemTools.OWL2RL2Datalog
open IriTools
open Microsoft.FSharp.Quotations
open Xunit
open Faqt

[<Fact>]
let ``Subclass RL reasoning works`` () =
    let tripleTable = new Datastore(100u)
    let errorOutput = new System.IO.StringWriter()
    
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let rdfTypeIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference (Namespaces.RdfType)))
    let objIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
    let subClassIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference (Namespaces.RdfsSubClassOf)))
    let objIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
    let contentTriple = {Ingress.Triple.subject = subjectIndex; predicate = rdfTypeIndex; obj = objIndex}
    tripleTable.AddTriple(contentTriple)
    let query = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex2)
    query.Should().HaveLength(0) |> ignore
    
    let subClassRule : Rule = {
        Head = {Subject = ResourceOrVariable.Resource objIndex; Predicate = ResourceOrVariable.Resource subClassIndex; Object = ResourceOrVariable.Resource objIndex2}
        Body = [
            PositiveTriple {Subject = ResourceOrVariable.Resource subjectIndex; Predicate = ResourceOrVariable.Resource rdfTypeIndex; Object = ResourceOrVariable.Resource objIndex}
        ]
    }
    let rlProgram = Reasoner.enableOwlReasoning tripleTable [subClassRule] errorOutput
    DagSemTools.Datalog.Reasoner.evaluate (rlProgram |> Seq.toList, tripleTable)
    let query2 = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex2)
    query2.Should().HaveLength(1) |> ignore
    
