(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
module DagSemTools.OWL2RL2Datalog.TestEqualityAxioms 

    open System
    open DagSemTools.Datalog.Reasoner
    open DagSemTools.Rdf
    open DagSemTools.Datalog
    open DagSemTools.Rdf.Ingress
    open DagSemTools.OWL2RL2Datalog
    open DagSemTools
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
    

    // [<Fact>]
    let ``Equality RL adds equality axioms`` () =
        // Arrange
        let tripleTable = new Datastore(100u)
        let errorOutput = new System.IO.StringWriter()
        let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.OwlSameAs)))
        let subjectIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject2"))
        let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = subjectIndex2}
        tripleTable.AddTriple(Triple)
        
        let query = tripleTable.GetTriplesWithObject(subjectIndex2)
        query.Should().HaveLength(1) |> ignore
        
        //Act
        let ontologyTranslator = new RdfOwlTranslator.Rdf2Owl(tripleTable.Triples, tripleTable.Resources)
        let ontology = ontologyTranslator.extractOntology
        let rlProgram = Library.owl2Datalog logger tripleTable.Resources ontology
        DagSemTools.Datalog.Reasoner.evaluate (logger, rlProgram |> Seq.toList, tripleTable)
        
        //Assert
        let query2 = tripleTable.GetTriplesWithObject(subjectIndex2)
        query2.Should().HaveLength(2) |> ignore
        let query3 = tripleTable.GetTriplesWithPredicate(predIndex)
        query3.Should().HaveLength(5) |> ignore
        inMemorySink.LogEvents.Should().BeEmpty;

    [<Fact>]
    let ``Predicate variables are ordered`` () =
        let tripleTable = new Datastore(100u)
        let errorOutput = new System.IO.StringWriter()
        
        let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference ("http://example.com/predicate")))
        let objextIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
        let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objextIndex}
        tripleTable.AddTriple(Triple)
        let triplePattern varName : TriplePattern =
            {
                Subject = ResourceOrVariable.Resource subjectIndex
                Predicate = ResourceOrVariable.Variable varName
                Object = ResourceOrVariable.Resource objextIndex
            }
        let rule : Rule = {Head = NormalHead ( triplePattern "s1" )
                           Body = [PositiveTriple (triplePattern "s2")]}
        let partitioner = DagSemTools.Datalog.Stratifier.RulePartitioner (logger, [rule])
        let stratification = partitioner.orderRules
        stratification.Should().HaveLength(1) |> ignore

    [<Fact>]
    let ``Unsafe rules are rejected`` () =
        let tripleTable = new Datastore(100u)
        let errorOutput = new System.IO.StringWriter()
        let program : DatalogProgram = new DatalogProgram([], tripleTable)
        let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference ("http://example.com/predicate")))
        let objextIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
        let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objextIndex}
        tripleTable.AddTriple(Triple)
        let triplePattern varName : TriplePattern =
            {
                Subject = ResourceOrVariable.Resource subjectIndex
                Predicate = ResourceOrVariable.Variable varName
                Object = ResourceOrVariable.Resource objextIndex
            }
        let rule : Rule = {Head = NormalHead ( triplePattern "s1" )
                           Body = [PositiveTriple (triplePattern "s2")]}
        let evaluatorFunction = fun () -> DagSemTools.Datalog.Reasoner.evaluate (logger, [rule], tripleTable)
        Assert.Throws<ArgumentException>(evaluatorFunction) |> ignore
        

    [<Fact>]
    let ``Predicate variables are handled`` () =
        let tripleTable = new Datastore(100u)
        let errorOutput = new System.IO.StringWriter()
        
        let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference ("http://example.com/predicate")))
        let objextIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
        let objextIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object2"))
        let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objextIndex}
        tripleTable.AddTriple(Triple)
        let headPattern : TriplePattern  =
            {
                Subject = ResourceOrVariable.Resource subjectIndex
                Predicate = ResourceOrVariable.Variable "p"
                Object = ResourceOrVariable.Resource objextIndex2
            }
        let bodyPattern : TriplePattern  =
            {
                Subject = ResourceOrVariable.Resource subjectIndex
                Predicate = ResourceOrVariable.Variable "p"
                Object = ResourceOrVariable.Resource objextIndex
            }
        let rule : Rule = {Head = NormalHead headPattern; Body = [PositiveTriple (bodyPattern)]}
        
        let query1 = tripleTable.GetTriplesWithObject(objextIndex2)
        query1.Should().HaveLength(0) |> ignore
        
        DagSemTools.Datalog.Reasoner.evaluate (logger, [rule],  tripleTable)
        let query2 = tripleTable.GetTriplesWithObject(objextIndex2)
        query2.Should().HaveLength(1) |> ignore
        let query3 = tripleTable.GetTriplesWithPredicate(predIndex)
        query3.Should().HaveLength(2) |> ignore
        
    [<Fact>]
    let ``Equality axioms are handled`` () =
        // Arrange
        let tripleTable = new Datastore(100u)
        
        let objectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
        let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
        let sameAsIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.OwlSameAs)))
        let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/predicate"))
        let predIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/predicate2"))
        
        let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objectIndex}
        tripleTable.AddTriple(Triple)
        let SameAsTriple = {Ingress.Triple.subject = predIndex; predicate = sameAsIndex; obj = predIndex2}
        tripleTable.AddTriple(SameAsTriple)
        
        let oquery1 = tripleTable.GetTriplesWithObject(objectIndex)
        oquery1.Should().HaveLength(1) |> ignore
        let predQuery1 = tripleTable.GetTriplesWithPredicate(predIndex2)
        predQuery1.Should().HaveLength(0) |> ignore
        
        let sameAsRule2 : Rule = {
            Head = NormalHead{
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
        DagSemTools.Datalog.Reasoner.evaluate (logger, [sameAsRule2], tripleTable)
        
        // Assert
        let query3 = tripleTable.GetTriplesWithPredicate(predIndex2)
        query3.Should().HaveLength(1) |> ignore
        let query2 = tripleTable.GetTriplesWithObject(objectIndex)
        query2.Should().HaveLength(2) |> ignore


    [<Fact>]
    let ``Grounding + Stratifying starts ok`` () =
        // Arrange
        let tripleTable = new Datastore(100u)
        
        let objectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
        let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
        let sameAsIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.OwlSameAs)))
        let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/predicate"))
        let predIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/predicate2"))
        
        let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objectIndex}
        tripleTable.AddTriple(Triple)
        
        let oquery1 = tripleTable.GetTriplesWithObject(objectIndex)
        oquery1.Should().HaveLength(1) |> ignore
        let predQuery1 = tripleTable.GetTriplesWithPredicate(predIndex2)
        predQuery1.Should().HaveLength(0) |> ignore
        
        let sameAsRule2 : Rule = {
            Head = NormalHead {
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
        let rules_with_iri_predicates = PredicateGrounder.groundRulePredicates([sameAsRule2], tripleTable) |> Seq.toList
        let stratifier = Stratifier.RulePartitioner (logger, rules_with_iri_predicates)
        let relationInfos = stratifier.GetOrderedRelations()
        
        //Assert
        let predRelsInfo = relationInfos.[(int) predIndex]
        predRelsInfo.num_predecessors.Should().Be((uint) 2) |> ignore
        predRelsInfo.Successors.Should().HaveLength(1) |> ignore
        
    [<Fact>]
    let ``Equality axioms can be grounded`` () =
        // Arrange
        let tripleTable = new Datastore(100u)
        
        let objectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
        let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
        let sameAsIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.OwlSameAs)))
        let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/predicate"))
        let predIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/predicate2"))
        
        let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objectIndex}
        tripleTable.AddTriple(Triple)
        let SameAsTriple = {Ingress.Triple.subject = predIndex; predicate = sameAsIndex; obj = predIndex2}
        tripleTable.AddTriple(SameAsTriple)
        
        let oquery1 = tripleTable.GetTriplesWithObject(objectIndex)
        oquery1.Should().HaveLength(1) |> ignore
        let predQuery1 = tripleTable.GetTriplesWithPredicate(predIndex2)
        predQuery1.Should().HaveLength(0) |> ignore
        
        let sameAsRule2 : Rule = {
            Head = NormalHead {
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
            Head = NormalHead {
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
        
    // [<Fact>]
    let ``Equality RL reasoning works`` () =
        let tripleTable = new Datastore(100u)
        let errorOutput = new System.IO.StringWriter()
        
        let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
        let sameAsIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference (Namespaces.OwlSameAs)))
        let subjectIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject2"))
        let objIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
        let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/predicate"))
        let SameAsTriple = {Ingress.Triple.subject = subjectIndex; predicate = sameAsIndex; obj = subjectIndex2}
        let contentTriple = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objIndex}
        tripleTable.AddTriple(SameAsTriple)
        tripleTable.AddTriple(contentTriple)
        let query = tripleTable.GetTriplesWithObject(objIndex)
        query.Should().HaveLength(1) |> ignore
        let query1a = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objIndex)
        query1a.Should().HaveLength(0) |> ignore
        
        let ontologyTranslator = new RdfOwlTranslator.Rdf2Owl(tripleTable.Triples, tripleTable.Resources)
        let ontology = ontologyTranslator.extractOntology
        let rlProgram = Library.owl2Datalog logger tripleTable.Resources ontology
        
        DagSemTools.Datalog.Reasoner.evaluate (logger, rlProgram |> Seq.toList, tripleTable)
        let query2 = tripleTable.GetTriplesWithObject(objIndex)
        query2.Should().HaveLength(3) |> ignore
        let query3 = tripleTable.GetTriplesWithPredicate(predIndex)
        query3.Should().HaveLength(2) |> ignore
        let query1b = tripleTable.GetTriplesWithSubjectObject(subjectIndex2, objIndex)
        query1b.Should().HaveLength(1) |> ignore
        inMemorySink.LogEvents.Should().BeEmpty
        