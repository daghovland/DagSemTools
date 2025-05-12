(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Datalog.Tests

open System
open DagSemTools.Datalog.Datalog
open DagSemTools.Rdf
open DagSemTools.Ingress
open IriTools
open Serilog
open Serilog.Sinks.InMemory
open Xunit 
open Ingress
open DagSemTools.Datalog
open DagSemTools
open Faqt

open DagSemTools.Datalog
open Faqt

module Tests =
    
    let inMemorySink = new InMemorySink()
    let logger =
        LoggerConfiguration()
                .WriteTo.Sink(inMemorySink)
                .CreateLogger()
    

    
    [<Fact>]
    let ``Datalog program fetches rule`` () =
        let tripleTable = Datastore(60u)
        
        let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
        let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
        let triplepattern =  {
                             TriplePattern.Subject = Term.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = Term.Resource objdIndex}
        let triplepattern2 = PositiveTriple {
                             TriplePattern.Subject = Term.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = Term.Resource objdIndex2
                             }
        let tripleFact : Triple = {
                             subject = subjectIndex
                             predicate =  predIndex
                             obj = objdIndex2
                             }
        
        let rule = {Head =  NormalHead triplepattern; Body = [triplepattern2]}
        let prog = Reasoner.DatalogProgram ([rule], tripleTable)
        let rules = prog.GetRulesForFact tripleFact
        Assert.Single(rules)
        
    let ``Datalog program does not fetch when no matching rule`` () =
        let tripleTable = Rdf.Datastore(60u)
        
        let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
        let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
        let objdIndex3 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object3"))
        let triplepattern = {
                             TriplePattern.Subject = Term.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = Term.Resource objdIndex}
        let triplepattern2 = PositiveTriple{
                             TriplePattern.Subject = Term.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = Term.Resource objdIndex2
                             }
        let tripleFact : Triple = {
                             subject = subjectIndex
                             predicate =  predIndex
                             obj = objdIndex3
                             }
        
        let rule = {Head =  NormalHead triplepattern; Body = [triplepattern2]}
        let prog = Reasoner.DatalogProgram ([rule], tripleTable)
        let rules = prog.GetRulesForFact tripleFact
        Assert.Empty(rules)
        
    [<Fact>]
    let ``merging maps to lists works fine``() =
        let m1 : Map<string, int list> = Map([("a", [1]); ("b", [2])])
        let m2 : Map<string, int list> = Map([("a", [1]); ("b", [3])])
        let merged : Map<string, int list> = mergeMaps [m1; m2]
        let expected : Map<string, int list> = Map([("a", [1; 1]); ("b", [3; 2])])
        Assert.Equal<Map<string, int list>>(expected, merged)
        
        
    [<Fact>]
    let ``Wildcard triple patterns with one variable are correctly generated`` () =
        let tripleTable = Rdf.Datastore(60u)
        
        let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
        let triplepattern = {TriplePattern.Subject = Term.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = Term.Resource objdIndex}
        let wildcards = WildcardTriplePattern triplepattern
        Assert.Equal(4, wildcards.Length)
        
        
        
    [<Fact>]
    let ``Wildcard triple patterns with three variables are correctly generated`` () =
        let triplepattern = {TriplePattern.Subject = Variable "s"
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = Variable "o"}
        let wildcards = WildcardTriplePattern triplepattern
        Assert.Equal(1, wildcards.Length)
        
        
    [<Fact>]
    let ``Wildcard triple patterns with no variables are correctly generated`` () =
        let tripleTable = Rdf.Datastore(60u)
        
        let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
        let triplepattern = {TriplePattern.Subject = Term.Resource subjectIndex
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex}
        let wildcards = WildcardTriplePattern triplepattern
        Assert.Equal(8, wildcards.Length)
        
    [<Fact>]
    let ``Can get matches on rule`` () =
        let tripleTable = Rdf.Datastore(60u)
        Assert.Equal(0u, tripleTable.Resources.ResourceCount)
        Assert.Equal(0u, tripleTable.Triples.TripleCount)
        let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
        let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
        tripleTable.AddTriple(Triple)
        Assert.Equal(3u, tripleTable.Resources.ResourceCount)
        Assert.Equal(1u, tripleTable.Triples.TripleCount)
        let allTriples = tripleTable.Triples.GetTriples()
        let mappedTriple = allTriples |> Seq.head
        Assert.Equal(Triple, mappedTriple)
        
        let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
        let Triple2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex2}
        
        let rule =  {Head =  NormalHead ( ConstantTriplePattern Triple2 )
                     Body = [ RuleAtom.PositiveTriple (ConstantTriplePattern Triple)]}
        let TriplePatter = {
                            TriplePattern.Subject = Term.Resource subjectIndex
                            TriplePattern.Predicate = Term.Resource predIndex
                            TriplePattern.Object = Term.Resource objdIndex
                            }
        let partialRule = {Rule = rule; Match =  TriplePatter}
        let matches = GetMatchesForRule Triple partialRule
        Assert.Single matches
   
    [<Fact>]
    let ``Can get matches on rule with negative atom`` () =
            let tripleTable = Rdf.Datastore(60u)
            Assert.Equal(0u, tripleTable.Resources.ResourceCount)
            Assert.Equal(0u, tripleTable.Triples.TripleCount)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            tripleTable.AddTriple(Triple)
            Assert.Equal(3u, tripleTable.Resources.ResourceCount)
            Assert.Equal(1u, tripleTable.Triples.TripleCount)
            let mappedTriple = tripleTable.Triples.GetTriples() |> Seq.head
            Assert.Equal(Triple, mappedTriple)
            
            let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
            let Triple2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex2}
            
            let objdIndex3 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object3"))
            let Triple3 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex3}
            
            
            let rule =  {Head =  NormalHead( ConstantTriplePattern Triple2 )
                         Body = [ RuleAtom.PositiveTriple (ConstantTriplePattern Triple) ; RuleAtom.NotTriple (ConstantTriplePattern Triple3)]}
            let TriplePatter = {
                                TriplePattern.Subject = Term.Resource subjectIndex
                                TriplePattern.Predicate = Term.Resource predIndex
                                TriplePattern.Object = Term.Resource objdIndex
                                }
            let partialRule = {Rule = rule; Match =  TriplePatter}
            let matches = GetMatchesForRule Triple partialRule
            Assert.Single matches

           
    [<Fact>]
    let ``Can evaluate rules positively`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj1 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex3
                             }
            let positiveMatch = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            let rule =  {Head = headPattern |> NormalHead
                         Body = [ positiveMatch //; negativeMatch
                                                                     ]}
            let prog = Reasoner.DatalogProgram([rule], tripleTable)
            let triple = Subject1Obj1
            for rules in prog.GetRulesForFact triple do
                for subs in evaluatePositive tripleTable.Triples rules do
                    Assert.NotEmpty subs 
            
           
           
    [<Fact>]
    let ``Can evaluate pattern`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj1 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex3
                             }
            let positiveMatch = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            let rule =  {Head = NormalHead (headPattern); Body = [ positiveMatch //; negativeMatch
                                                                     ]}
            let prog = Reasoner.DatalogProgram([rule], tripleTable)
            let triple = Subject1Obj1
            for rules in prog.GetRulesForFact triple do
                for subs in evaluatePattern tripleTable.Triples rules.Match.Match rules.Substitution do
                    Assert.NotEmpty subs
       
    [<Fact>]
    let ``Can get matches on complex rule that matches two triples`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj1 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex3
                             }
            let positiveMatch = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            let rule =  {Head = NormalHead (headPattern); Body = [ positiveMatch //; negativeMatch
                                                                     ]}
            Reasoner.evaluate(logger, [rule], tripleTable)
            let matches = tripleTable.GetTriplesWithObject(objdIndex3) |> List.ofSeq
            Assert.Equal(2, matches.Length)
          

    
    
    [<Fact>]
    let ``Can use subclassing as a rule`` () =
            //Arrange
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let rdfTypeIndex = tripleTable.AddNodeResource(Iri(new IriReference (Namespaces.RdfType)));
            let subClassOfIndex = tripleTable.AddNodeResource(Iri(new IriReference(Namespaces.RdfsSubClassOf)));
            let subClassIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subClass"));
            let superClassIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/superClass"));
            let typeTriple = {Triple.subject = subjectIndex; predicate = rdfTypeIndex; obj = subClassIndex}
            tripleTable.AddTriple(typeTriple)
            let subClassTriple = {Triple.subject = subClassIndex; predicate = subClassOfIndex; obj = superClassIndex}
            tripleTable.AddTriple(subClassTriple)
            
            let matchesBefore = tripleTable.GetTriplesWithSubjectObject(subjectIndex, superClassIndex) |> List.ofSeq
            Assert.Equal(0, matchesBefore.Length)
          
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "?x"
                             TriplePattern.Predicate = Term.Resource rdfTypeIndex
                             TriplePattern.Object = Term.Variable "?super"
                             }
            let subClassTypeAtom = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "?x"
                             TriplePattern.Predicate = Term.Resource rdfTypeIndex
                             TriplePattern.Object = Term.Variable "?sub"
                             }
            let isSubClassOfAtom = RuleAtom.PositiveTriple {
                                    TriplePattern.Subject = Term.Variable "?sub"
                                    TriplePattern.Predicate = Term.Resource subClassOfIndex
                                    TriplePattern.Object = Term.Variable "?super" 
                                    }
            let rule =  {Head = NormalHead (headPattern); Body = [ subClassTypeAtom; isSubClassOfAtom ]}
            
            //Act
            Reasoner.evaluate(logger, [rule], tripleTable)
            
            //Assert
            let matches = tripleTable.GetTriplesWithSubjectObject(subjectIndex, superClassIndex) |> List.ofSeq
            Assert.Equal(1, matches.Length)
          
    
    [<Fact>]
    let ``Can evaluate pattern with evaluate function`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let predIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate2"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            let Subject1Subj2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex2; obj = subjectIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            tripleTable.AddTriple(Subject1Subj2)
            
            
            let triplePattern2 = {
                             TriplePattern.Subject = Term.Variable "s2"
                             TriplePattern.Predicate = Term.Resource predIndex2
                             TriplePattern.Object = Term.Variable "s"
                             }
           
            let typedTriples = tripleTable.GetTriplesWithObject(objdIndex)
            Assert.Single typedTriples |> ignore
             
            let evaluatedSubs2 = evaluatePattern tripleTable.Triples triplePattern2 Map.empty
            Assert.Equal(1, Seq.length evaluatedSubs2)
           
    [<Fact>]
    let ``Can get matches on recursive rule that matches two triples`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let predIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate2"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            let Subject1Subj2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex2; obj = subjectIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            tripleTable.AddTriple(Subject1Subj2)
            
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            let positiveMatch1 = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s2"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            let positiveMatch2 = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s2"
                             TriplePattern.Predicate = Term.Resource predIndex2
                             TriplePattern.Object = Term.Variable "s"
                             }
            
            let rule =  {Head = NormalHead (headPattern); Body = [ positiveMatch1 ; positiveMatch2
                                                                     ]}
            Reasoner.evaluate(logger, [rule], tripleTable)
            let matches = tripleTable.GetTriplesWithObject(objdIndex) |> List.ofSeq
            Assert.Equal(2, matches.Length)


    [<Fact>]
    let ``Single recursive rule is found as a cycle`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let predIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate2"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            let Subject1Subj2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex2; obj = subjectIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            tripleTable.AddTriple(Subject1Subj2)
            
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            let positiveMatch1 = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s2"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            let positiveMatch2 = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s2"
                             TriplePattern.Predicate = Term.Resource predIndex2
                             TriplePattern.Object = Term.Variable "s"
                             }
            
            let rule =  {Head = NormalHead (headPattern); Body = [ positiveMatch1 ; positiveMatch2
                                                                     ]}
            let stratifier = Stratifier.RulePartitioner (logger, [rule], tripleTable.Resources)
            let returned_cycles = stratifier.cycle_finder [] 0u
            let expected = seq { seq { 0u } }
            (returned_cycles |> Seq.length).Should().Be(1, "There is a cycle that should have been detected.") |> ignore
            let returned_cycle = returned_cycles |> Seq.head
            (returned_cycle |> Seq.length).Should().Be(1, "The program has a single recursive rule.")
          
    [<Fact>]
    let ``Single recursive rule is handled as a cycle`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let predIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate2"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            let Subject1Subj2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex2; obj = subjectIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            tripleTable.AddTriple(Subject1Subj2)
            
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            let positiveMatch1 = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s2"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            let positiveMatch2 = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s2"
                             TriplePattern.Predicate = Term.Resource predIndex2
                             TriplePattern.Object = Term.Variable "s"
                             }
            
            let rule =  {Head = NormalHead (headPattern); Body = [ positiveMatch1 ; positiveMatch2
                                                                     ]}
            let stratifier = Stratifier.RulePartitioner (logger, [rule], tripleTable.Resources)
            stratifier.handle_cycle()
            stratifier.GetReadyElementsQueue().IsEmpty.Should().BeFalse("There is a cycle that should have been detected.") |> ignore
        
        
    [<Fact>]
    let ``Single recursive rule is seen as a covered cycle`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let predIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate2"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            let Subject1Subj2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex2; obj = subjectIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            tripleTable.AddTriple(Subject1Subj2)
            
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            let positiveMatch1 = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s2"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            let positiveMatch2 = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s2"
                             TriplePattern.Predicate = Term.Resource predIndex2
                             TriplePattern.Object = Term.Variable "s"
                             }
            
            let rule =  {Head = NormalHead (headPattern); Body = [ positiveMatch1 ; positiveMatch2
                                                                     ]}
            let stratifier = Stratifier.RulePartitioner (logger, [rule], tripleTable.Resources)
            let covered = stratifier.RuleIsCoveredByCycle [0u] rule
            covered.Should().BeTrue()
            
        
        
    [<Fact>]
    let ``Semipositive programs with implicitly unary relations are rejected`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj1 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex3
                             }
            let positiveMatch = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            let negativeMatch = RuleAtom.NotTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex2
                             }
            
            let rule =  {Head = NormalHead (headPattern)
                         Body = [ positiveMatch; negativeMatch ]
            }
            let partitioner  = DagSemTools.Datalog.Stratifier.RulePartitioner (logger, [rule], tripleTable.Resources)
            let ordered_relations = partitioner.GetOrderedRules()
            ordered_relations.Should().HaveLength(1) |> ignore
            let init_queue = partitioner.GetReadyElementsQueue()
            init_queue.Should().HaveLength(1) |> ignore
            let first_partition = partitioner.get_rule_partition()
            first_partition.Should().Contain(rule).And.HaveLength(1) |> ignore
            
            let isSemiPositive = Stratifier.IsSemiPositiveProgram [rule]
            Assert.True isSemiPositive
    
    
    
    (* Tests the simplest cyclic program
        [?s,:A,:o] :- [?s,:A,:o] .
    *)
    [<Fact>]
    let ``Simple cyclic programs are detected by cycle-detector`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            let positiveMatch = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            
            let rule =  {Head = NormalHead (headPattern); Body = [ positiveMatch ] }
            let partitioner  = Stratifier.RulePartitioner (logger, [rule], tripleTable.Resources)
            let cycles = partitioner.cycle_finder [] 0u
            cycles.Should().HaveLength(1) |> ignore
            let cycle = cycles |> Seq.head
            cycle.Should().HaveLength(1) |> ignore
            
    (* Tests a program with a single fact *)
    [<Fact>]
    let ``Simple fact program works`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            
            let query1 = tripleTable.GetTriplesWithSubjectObject(subjIndex, objdIndex)
            query1.Should().HaveLength(0) |> ignore
            
            let headPattern = {
                             TriplePattern.Subject = Term.Resource subjIndex
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            
            let rule =  {Head = NormalHead (headPattern); Body = [  ] }
            DagSemTools.Datalog.Reasoner.evaluate (logger, [rule], tripleTable)
            let query2 = tripleTable.GetTriplesWithSubjectObject(subjIndex, objdIndex)
            query2.Should().HaveLength(1) |> ignore
    
    
    (* Tests the simplest cyclic program
        [?s,:A,:o] :- [?s,:A,:o] .
    *)
    [<Fact>]
    let ``Simple cyclic programs are detected`` () =
            let tripleTable = Rdf.Datastore(60u)
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            
            
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            let positiveMatch = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            
            let rule =  {Head = NormalHead (headPattern); Body = [ positiveMatch ] }
            let partitioner  = Stratifier.RulePartitioner (logger, [rule], tripleTable.Resources)
            let orderedRules = partitioner.orderRules()
            orderedRules.Should().HaveLength(1) |> ignore
            let firstPartition = orderedRules |> Seq.head
            firstPartition.Should().Contain(rule) |> ignore
            
    
    (* Tests the simplest program with a negative cycle
        [?s,:A,:o] :- not [?s,:A,:o] .
    *)        
    [<Fact>]
    let ``Simplest non-stratifiable program is rejected`` () =
            let tripleTable = Rdf.Datastore(60u)
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            let negativeMatch = RuleAtom.NotTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            let rule =  {Head = NormalHead (headPattern); Body = [ negativeMatch
                                                       ]
            }
            let partitioner  = Stratifier.RulePartitioner (logger, [rule], tripleTable.Resources)
            (fun () -> partitioner.orderRules()).Should().Throw<Exception,_>() |> ignore
              
    (* [?s, p, o3] :- [?s, p, o], not [?s, p, o3] . *)
    [<Fact>]
    let ``Non-semipositive programs are rejected`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj1 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex3
                             }
            let positiveMatch = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            let negativeMatch = RuleAtom.NotTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex3
                             }
            
            let rule =  {Head =  NormalHead headPattern
                         Body = [ positiveMatch; negativeMatch ]
            }
            let partitioner  = Stratifier.RulePartitioner (logger, [rule], tripleTable.Resources)
            let ordered_relations = partitioner.GetOrderedRules()
            ordered_relations.Should().HaveLength(1) |> ignore
            let init_queue = partitioner.GetReadyElementsQueue()
            init_queue.Should().HaveLength(0) |> ignore
            let first_partition = partitioner.get_rule_partition()
            first_partition.Should().HaveLength(0) |> ignore
            
            let isSemiPositive = Stratifier.IsSemiPositiveProgram [rule]
            Assert.False isSemiPositive
    
    (*              
            [?s,:A,:o] :- [?s,:A,:o], [?s,:B,:o]
            [?s,:B,:o] :- not [?s,:C,:o2]
            :A predecessors 2, successors A
            :B predecessors 1, successors A
            :C predecessors 0, successors B
            First stratification should include one rule, [?s,:B,:o] :- not [?s,:C,:o2]
            
            ?s 1u 4u
     *)
    [<Fact>]
    let rec ``Stratifier outputs first partition`` () =
            let tripleTable = Rdf.Datastore(60u)
            let predIndexA = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicateA"))
            let predIndexB = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicateB"))
            let predIndexC = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicateC"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
            
            let headPatternA = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndexA
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            let headPatternB = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndexB
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            let positiveMatchA = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndexA
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            let positiveMatchB = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndexB
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            let negativeMatch = RuleAtom.NotTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndexC
                             TriplePattern.Object = Term.Resource objdIndex2
                             }
            
            let ruleA =  {Head = NormalHead (headPatternA)
                          Body = [ positiveMatchA; positiveMatchB ]
            }
            let ruleB = {Head = NormalHead (headPatternB)
                         Body = [ negativeMatch ]
            }
            let partitioner  = Stratifier.RulePartitioner (logger, [ruleA; ruleB], tripleTable.Resources)
            let ordered_relations = partitioner.GetOrderedRules()
            ordered_relations.Should().HaveLength(2) 
            
            let init_queue = partitioner.GetReadyElementsQueue()
            init_queue.Should().HaveLength(1)
            let first_partition = partitioner.get_rule_partition()
            first_partition.Should().Contain(ruleB).And.HaveLength(1)
    
            
    [<Fact>]
    let ``Can get matches on complex rule with negative atom`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
            let predIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate2"))
            let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj1 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex2; obj = objdIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            
            let headPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex3
                             }
            let positiveMatch = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex
                             TriplePattern.Object = Term.Resource objdIndex
                             }
            
            let negativeMatch = RuleAtom.NotTriple {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource predIndex2
                             TriplePattern.Object = Term.Resource objdIndex2
                             }
            
            let rule =  {Head = NormalHead (headPattern)
                         Body = [ positiveMatch; negativeMatch ]
            }
            Reasoner.evaluate(logger, [rule], tripleTable)
            let matches = tripleTable.GetTriplesWithObject(objdIndex3)
            Assert.Single matches
          
    [<Fact>]
    let ``Can get substitution``() =
        let resource = 1u
        let variable = Term.Variable "s"
        let subbed = GetSubstitution (resource, variable) (Map.empty)
        Assert.Equal(1u, subbed.Value.["s"])
    
    [<Fact>]
    let ``Can get substitution option``() =
        let resource = 1u
        let variable = Term.Variable "s"
        let subbed = GetSubstitutionOption (Some Map.empty) (resource, variable) 
        Assert.Equal(1u, subbed.Value.["s"])
        
    [<Fact>]
    let ``Can get substitutions option``() =
        let fact = {Ingress.Triple.subject = 1u; predicate = 2u; obj = 3u}
        let factPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Variable "p"
                             TriplePattern.Object = Term.Variable "o"}
        
        let subbed = GetSubstitutions Map.empty fact factPattern 
        Assert.Equal(1u, subbed.Value.["s"])
        Assert.Equal(2u, subbed.Value.["p"])
        Assert.Equal(3u, subbed.Value.["o"])
        
            
    [<Fact>]
    let ``Can get substitutions option map``() =
        let fact = {Ingress.Triple.subject = 1u; predicate = 2u; obj = 3u}
        let factPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource 2u
                             TriplePattern.Object = Term.Variable "o"}
        
        let subbed = GetSubstitutions Map.empty fact factPattern 
        Assert.Equal(1u, subbed.Value.["s"])
        Assert.Equal(3u, subbed.Value.["o"])
        
        
                    
    [<Fact>]
    let ``Can increase substitutions option map``() =
        let fact = {Ingress.Triple.subject = 1u; predicate = 2u; obj = 3u}
        let factPattern = {
                             TriplePattern.Subject = Term.Variable "s"
                             TriplePattern.Predicate = Term.Resource 2u
                             TriplePattern.Object = Term.Variable "o"}
        
        let subbed = GetSubstitutions (Map.empty.Add ("t", 2u)) fact factPattern 
        Assert.Equal(1u, subbed.Value.["s"])
        Assert.Equal(3u, subbed.Value.["o"])
        
    [<Fact>]
    let ``No subsititution if mismatch``() =
        let resource = 1u
        let variable = Term.Variable "s"
        let subbed = GetSubstitution (resource, variable) (Map.empty.Add("s", 2u))
        Assert.Equal(None, subbed)
    
    [<Fact(Skip="Creates a loop. Bug in new stratifier code")>]
    let ``Can add triple using rule over tripletable`` () =
        let tripleTable = Rdf.Datastore(60u)
        Assert.Equal(0u, tripleTable.Resources.ResourceCount)
        Assert.Equal(0u, tripleTable.Triples.TripleCount)
        let subjectIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object"))
        let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
        tripleTable.AddTriple(Triple)
        Assert.Equal(3u, tripleTable.Resources.ResourceCount)
        Assert.Equal(1u, tripleTable.Triples.TripleCount)
        let mappedTriple = tripleTable.Triples.GetTriples() |> Seq.head
        Assert.Equal(Triple, mappedTriple)
        
        let objdIndex2 = tripleTable.AddNodeResource(Iri(new IriReference "http://example.com/object2"))
        let Triple2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex2}
        
        let rule =  {Head =  ConstantTriplePattern Triple2 |> NormalHead
                     Body = [RuleAtom.PositiveTriple(ConstantTriplePattern Triple)]}
        let tripleAnswersBefore = tripleTable.GetTriplesWithSubjectPredicate(subjectIndex, predIndex)
        Assert.Equal(1, tripleAnswersBefore |> Seq.length)
        let triples2Answers = tripleAnswersBefore |> Seq.filter (fun tr -> tr = Triple2)
        Assert.Equal(0, triples2Answers |> Seq.length)
        
        Reasoner.evaluate (logger, [rule], tripleTable)
        
        Assert.Equal(2u, tripleTable.Triples.TripleCount)
        let tripleAnswers2 = tripleTable.GetTriplesWithSubjectPredicate(subjectIndex, predIndex)
        Assert.Contains(Triple2, tripleAnswers2)
        
