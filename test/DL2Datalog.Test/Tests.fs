module Tests

open System
open DagSemTools.AlcTableau.ALC
open DagSemTools.Datalog
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
    let query2 = tripleTable.GetTriplesWithObject(objdIndex2)
    query2.Should().NotBeEmpty()
    
    
    
[<Fact>]
let ``Can create axiom rules`` () =
    let tripleTable = new DagSemTools.Rdf.Datastore(new DagSemTools.Rdf.TripleTable(10u), new DagSemTools.Rdf.QuadTable(10u), new DagSemTools.Rdf.QuadTable(10u), new DagSemTools.Rdf.ResourceManager(10u))
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference (Namespaces.RdfType)))
    let subObj = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.org/subConcept"))
    let superObj = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.org/superConcept"))
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = subObj}
    tripleTable.AddTriple(Triple)

    let subConcept = ALC.ConceptName(IriTools.IriReference("http://example.org/subConcept"))
    let superConcept = ALC.ConceptName(IriTools.IriReference("http://example.org/superConcept"))
    let subclass_assertion = ALC.Inclusion(subConcept, superConcept)
    let ontologyVersion = ALC.ontologyVersion.UnNamedOntology
     
    let subclass_rule =  Translator.CreateAxiomRule tripleTable.Resources subclass_assertion |> Seq.head
    subclass_rule.Head.Object.Should().Be(ResourceOrVariable.Resource superObj) |> ignore
    subclass_rule.Head.Subject.Should().Be(ResourceOrVariable.Variable "s") |> ignore
    let sub_atom = subclass_rule.Body |> Seq.head
    match sub_atom with
    | PositiveTriple triplePattern -> triplePattern.Object.Should().Be(ResourceOrVariable.Resource subObj) |> ignore
    | NotTriple triplePattern -> failwith "todo"
    