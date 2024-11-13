module Tests

open System
open DagSemTools.AlcTableau.ALC
open DagSemTools.Datalog
open Xunit
open DagSemTools.DL2Datalog
open DagSemTools.Rdf
open IriTools
open DagSemTools.Rdf.Ingress
open DagSemTools
open DagSemTools.AlcTableau
open Faqt



[<Fact>]
let ``Simplest owl axiom to datalog`` () =
    let tripleTable = new DagSemTools.Rdf.Datastore(new DagSemTools.Rdf.TripleTable(10u), new DagSemTools.Rdf.QuadTable(10u), new DagSemTools.Rdf.QuadTable(10u), new DagSemTools.Rdf.ResourceManager(10u))
    let subjectIndex = tripleTable.AddResource(Resource.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Resource.Resource.Iri(new IriReference (Namespaces.RdfType)))
    let concept1Iri = new IriReference "http://example.org/concept1"
    let objdIndex = tripleTable.AddResource(Resource.Resource.Iri concept1Iri)
    let concept2Iri = new IriReference "http://example.org/concept2"
    let objdIndex2 = tripleTable.AddResource(Resource.Resource.Iri(concept2Iri))
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
    tripleTable.AddTriple(Triple)

    let concept1 = ALC.ConceptName(concept1Iri)
    let concept2 = ALC.ConceptName(concept2Iri)
    let X_concept1 = tripleTable.AddResource(Resource.Resource.DLTranslatedConceptName concept1Iri)
    let X_concept2 = tripleTable.AddResource(Resource.Resource.DLTranslatedConceptName concept2Iri)
    let subclass_assertion = ALC.Inclusion(concept1, concept2)
    let ontologyVersion = ALC.ontologyVersion.UnNamedOntology
     
    let kb = ([subclass_assertion], [])
    let ontology = OntologyDocument.Ontology ([], ontologyVersion, kb)
    let dlprog =  Translator.Ontology2Rules tripleTable.Resources ontology
   
    let query = tripleTable.GetTriplesWithObject(X_concept2)
    query.Should().BeEmpty() |> ignore
    DagSemTools.Datalog.Reasoner.evaluate (dlprog |> Seq.toList, tripleTable)
    let query2 = tripleTable.GetTriplesWithObject(X_concept2)
    query2.Should().NotBeEmpty()
    
    
    
[<Fact>]
let  ``Can create axiom rules`` () =
    let tripleTable = new DagSemTools.Rdf.Datastore(new DagSemTools.Rdf.TripleTable(10u), new DagSemTools.Rdf.QuadTable(10u), new DagSemTools.Rdf.QuadTable(10u), new DagSemTools.Rdf.ResourceManager(10u))
    let subjectIndex = tripleTable.AddResource(Resource.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Resource.Resource.Iri(new IriReference (Namespaces.RdfType)))
    let subConceptIri = new IriReference "http://example.org/subConcept"
    let superConceptIri = new IriReference "http://example.org/superConcept"
    
    let subConcept = ALC.ConceptName(subConceptIri)
    let X_subConcept = tripleTable.AddResource(Resource.Resource.DLTranslatedConceptName(subConceptIri))
    let superConcept = ALC.ConceptName(superConceptIri)
    let X_superConcept = tripleTable.AddResource(Resource.Resource.DLTranslatedConceptName superConceptIri)
    let subclass_assertion = ALC.Inclusion(subConcept, superConcept)
     
    let axiomsRules = Translator.CreateAxiomRule tripleTable.Resources subclass_assertion
    axiomsRules.Should().HaveLength(1) |> ignore
    let subclass_rule =  axiomsRules |> Seq.head
    subclass_rule.Head.Subject.Should().Be(ResourceOrVariable.Variable "s") |> ignore
    subclass_rule.Head.Object.Should().Be(ResourceOrVariable.Resource X_superConcept) |> ignore
    let sub_atom = subclass_rule.Body |> Seq.head
    match sub_atom with
    | PositiveTriple triplePattern -> triplePattern.Object.Should().Be(ResourceOrVariable.Resource X_subConcept) |> ignore
    | NotTriple _ -> failwith "Test failed. NotTriple found" |> ignore
    
    