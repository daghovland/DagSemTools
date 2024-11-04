module Tests

open System
open DagSemTools.Rdf
open DagSemTools.Datalog
open DagSemTools.Rdf.Ingress
open DagSemTools.OWL2RL2Datalog
open IriTools
open Microsoft.FSharp.Quotations
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
let ``Predicate variables are ordered`` () =
    let tripleTable = new Datastore(100u)
    let errorOutput = new System.IO.StringWriter()
    
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference ("http://example.com/predicate")))
    let objextIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objextIndex}
    tripleTable.AddTriple(Triple)
    let triplePattern varName : TriplePattern =
        {
            Subject = ResourceOrVariable.Resource subjectIndex
            Predicate = ResourceOrVariable.Variable varName
            Object = ResourceOrVariable.Resource objextIndex
        }
    let rule : Rule = {Head = triplePattern "s1"; Body = [PositiveTriple (triplePattern "s2")]}
    let partitioner = DagSemTools.Datalog.Stratifier.RulePartitioner [rule]
    let stratification = partitioner.orderRules
    stratification.Should().HaveLength(1) |> ignore

[<Fact>]
let ``Predicate variables are handled`` () =
    let tripleTable = new Datastore(100u)
    let errorOutput = new System.IO.StringWriter()
    
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference ("http://example.com/predicate")))
    let objextIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objextIndex}
    tripleTable.AddTriple(Triple)
    let triplePattern varName : TriplePattern =
        {
            Subject = ResourceOrVariable.Resource subjectIndex
            Predicate = ResourceOrVariable.Variable varName
            Object = ResourceOrVariable.Resource objextIndex
        }
    let rule : Rule = {Head = triplePattern "s1"; Body = [PositiveTriple (triplePattern "s2")]}
    DagSemTools.Datalog.Reasoner.evaluate ([rule], tripleTable)
    let query2 = tripleTable.GetTriplesWithObject(objextIndex)
    query2.Should().HaveLength(1) |> ignore
    let query3 = tripleTable.GetTriplesWithPredicate(predIndex)
    query3.Should().HaveLength(1) |> ignore

    
[<Fact>]
let ``Equality axioms are handled`` () =
    // Arrange
    let tripleTable = new Datastore(100u)
    
    let objectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let sameAsIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference (Namespaces.OwlSameAs)))
    let subjectIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject2"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
    let predIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate2"))
    
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objectIndex}
    tripleTable.AddTriple(Triple)
    let SameAsTriple = {Ingress.Triple.subject = predIndex; predicate = sameAsIndex; obj = predIndex2}
    tripleTable.AddTriple(SameAsTriple)
    
    let oquery1 = tripleTable.GetTriplesWithObject(objectIndex)
    oquery1.Should().HaveLength(1) |> ignore
    let predQuery1 = tripleTable.GetTriplesWithPredicate(predIndex2)
    predQuery1.Should().HaveLength(0) |> ignore
    
    let sameAsRule2 : Rule = {
        Head = {
            Subject = Variable "s"
            Predicate = Variable "p2"
            Object = Variable "o" 
        }
        Body = [
            PositiveTriple {
                Subject = Variable "p"
                Predicate = ResourceOrVariable.Resource sameAsIndex
                Object = Variable "p2"
            }
            PositiveTriple {
                Subject = Variable "s"
                Predicate = Variable "p"
                Object = Variable "o"
            }
        ] 
    }
    // Act
    DagSemTools.Datalog.Reasoner.evaluate ([sameAsRule2], tripleTable)
    
    // Assert
    let query3 = tripleTable.GetTriplesWithPredicate(predIndex2)
    query3.Should().HaveLength(1) |> ignore
    let query2 = tripleTable.GetTriplesWithObject(objectIndex)
    query2.Should().HaveLength(2) |> ignore

[<Fact>]
let ``Equality axioms can be grounded`` () =
    // Arrange
    let tripleTable = new Datastore(100u)
    
    let objectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let sameAsIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference (Namespaces.OwlSameAs)))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
    let predIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate2"))
    
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objectIndex}
    tripleTable.AddTriple(Triple)
    let SameAsTriple = {Ingress.Triple.subject = predIndex; predicate = sameAsIndex; obj = predIndex2}
    tripleTable.AddTriple(SameAsTriple)
    
    let oquery1 = tripleTable.GetTriplesWithObject(objectIndex)
    oquery1.Should().HaveLength(1) |> ignore
    let predQuery1 = tripleTable.GetTriplesWithPredicate(predIndex2)
    predQuery1.Should().HaveLength(0) |> ignore
    
    let sameAsRule2 : Rule = {
        Head = {
            Subject = Variable "s"
            Predicate = Variable "p2"
            Object = Variable "o" 
        }
        Body = [
            PositiveTriple {
                Subject = Variable "p"
                Predicate = ResourceOrVariable.Resource sameAsIndex
                Object = Variable "p2"
            }
            PositiveTriple {
                Subject = Variable "s"
                Predicate = Variable "p"
                Object = Variable "o"
            }
        ] 
    }
    // Act
    let groundRules = PredicateGrounder.groundRulePredicates ([sameAsRule2], tripleTable)
    
    // Assert
    groundRules.Should().HaveLength(5) |> ignore
    let correctGroundRule =  {
        Head = {
            Subject = Variable "s"
            Predicate = ResourceOrVariable.Resource predIndex2
            Object = Variable "o" 
        }
        Body = [
            PositiveTriple {
                Subject = Variable "p"
                Predicate = ResourceOrVariable.Resource sameAsIndex
                Object = ResourceOrVariable.Resource predIndex2
            }
            PositiveTriple {
                Subject = Variable "s"
                Predicate = Variable "p"
                Object = Variable "o"
            }
        ] 
    }
    groundRules.Should().Contain(correctGroundRule) |> ignore
    
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