(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace AlcTableau.Datalog.Tests

open System
open DagSemTools.Datalog.Datalog
open DagSemTools.Rdf
open IriTools
open Xunit 
open Ingress
open DagSemTools.Datalog
open DagSemTools
open Faqt
open DagSemTools.Rdf.Ingress
open DagSemTools.Datalog
open Faqt

module Tests =
    
    
    [<Fact>]
    let ``Datalog program fetches rule`` () =
        let tripleTable = Datastore(60u)
        
        let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
        let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
        let triplepattern =  {
                             TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex}
        let triplepattern2 = PositiveTriple {
                             TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex2
                             }
        let tripleFact : Triple = {
                             subject = subjectIndex
                             predicate =  predIndex
                             obj = objdIndex2
                             }
        
        let rule = {Head =  triplepattern; Body = [triplepattern2]}
        let prog = DatalogProgram ([rule], tripleTable)
        let rules = prog.GetRulesForFact tripleFact
        Assert.Single(rules)
        
    let ``Datalog program does not fetch when no matching rule`` () =
        let tripleTable = Rdf.Datastore(60u)
        
        let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
        let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
        let objdIndex3 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object3"))
        let triplepattern = {
                             TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex}
        let triplepattern2 = PositiveTriple{
                             TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex2
                             }
        let tripleFact : Triple = {
                             subject = subjectIndex
                             predicate =  predIndex
                             obj = objdIndex3
                             }
        
        let rule = {Head =  triplepattern; Body = [triplepattern2]}
        let prog = DatalogProgram ([rule], tripleTable)
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
        
        let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
        let triplepattern = {TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex}
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
        
        let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
        let triplepattern = {TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex}
        let wildcards = WildcardTriplePattern triplepattern
        Assert.Equal(8, wildcards.Length)
        
    [<Fact>]
    let ``Can get matches on rule`` () =
        let tripleTable = Rdf.Datastore(60u)
        Assert.Equal(0u, tripleTable.Resources.ResourceCount)
        Assert.Equal(0u, tripleTable.Triples.TripleCount)
        let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
        let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
        tripleTable.AddTriple(Triple)
        Assert.Equal(3u, tripleTable.Resources.ResourceCount)
        Assert.Equal(1u, tripleTable.Triples.TripleCount)
        let allTriples = tripleTable.Triples.GetTriples()
        let mappedTriple = allTriples |> Seq.head
        Assert.Equal(Triple, mappedTriple)
        
        let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
        let Triple2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex2}
        
        let rule =  {Head =  ConstantTriplePattern Triple2; Body = [ RuleAtom.PositiveTriple (ConstantTriplePattern Triple)]}
        let TriplePatter = {
                            TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                            TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                            TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                            }
        let partialRule = {Rule = rule; Match =  TriplePatter}
        let matches = GetMatchesForRule Triple partialRule
        Assert.Single matches
   
    [<Fact>]
    let ``Can get matches on rule with negative atom`` () =
            let tripleTable = Rdf.Datastore(60u)
            Assert.Equal(0u, tripleTable.Resources.ResourceCount)
            Assert.Equal(0u, tripleTable.Triples.TripleCount)
            let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
            let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
            let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            tripleTable.AddTriple(Triple)
            Assert.Equal(3u, tripleTable.Resources.ResourceCount)
            Assert.Equal(1u, tripleTable.Triples.TripleCount)
            let mappedTriple = tripleTable.Triples.GetTriples() |> Seq.head
            Assert.Equal(Triple, mappedTriple)
            
            let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
            let Triple2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex2}
            
            let objdIndex3 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object3"))
            let Triple3 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex3}
            
            
            let rule =  {Head =  ConstantTriplePattern Triple2; Body = [ RuleAtom.PositiveTriple (ConstantTriplePattern Triple) ; RuleAtom.NotTriple (ConstantTriplePattern Triple3)]}
            let TriplePatter = {
                                TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                                TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                                TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                                }
            let partialRule = {Rule = rule; Match =  TriplePatter}
            let matches = GetMatchesForRule Triple partialRule
            Assert.Single matches

           
    [<Fact>]
    let ``Can evaluate rules positively`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj1 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            
            let headPattern = {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex3
                             }
            let positiveMatch = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                             }
            let rule =  {Head =  headPattern; Body = [ positiveMatch //; negativeMatch
                                                                     ]}
            let prog = DatalogProgram([rule], tripleTable)
            let triple = Subject1Obj1
            for rules in prog.GetRulesForFact triple do
                for subs in evaluatePositive tripleTable.Triples rules do
                    Assert.NotEmpty subs 
            
           
           
    [<Fact>]
    let ``Can evaluate pattern`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj1 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            
            let headPattern = {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex3
                             }
            let positiveMatch = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                             }
            let rule =  {Head =  headPattern; Body = [ positiveMatch //; negativeMatch
                                                                     ]}
            let prog = DatalogProgram([rule], tripleTable)
            let triple = Subject1Obj1
            for rules in prog.GetRulesForFact triple do
                for subs in evaluatePattern tripleTable.Triples rules.Match.Match rules.Substitution do
                    Assert.NotEmpty subs
       
    [<Fact>]
    let ``Can get matches on complex rule that matches two triples`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj1 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            
            let headPattern = {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex3
                             }
            let positiveMatch = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                             }
            
            let rule =  {Head =  headPattern; Body = [ positiveMatch //; negativeMatch
                                                                     ]}
            let prog = DatalogProgram([rule], tripleTable)
            prog.materialise()
            let matches = tripleTable.GetTriplesWithObject(objdIndex3) |> List.ofSeq
            Assert.Equal(2, matches.Length)
          

    [<Fact>]
    let ``Can evaluate pattern with evaluate function`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
            let predIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate2"))
            let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            let Subject1Subj2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex2; obj = subjectIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            tripleTable.AddTriple(Subject1Subj2)
            
            
            let triplePattern2 = {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s2"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex2
                             TriplePattern.Object = ResourceOrVariable.Variable "s"
                             }
           
            let typedTriples = tripleTable.GetTriplesWithObject(objdIndex)
            Assert.Single typedTriples |> ignore
             
            let evaluatedSubs2 = evaluatePattern tripleTable.Triples triplePattern2 Map.empty
            Assert.Equal(1, Seq.length evaluatedSubs2)
           
    [<Fact>]
    let ``Can get matches on recursive rule that matches two triples`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
            let predIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate2"))
            let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            let Subject1Subj2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex2; obj = subjectIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            tripleTable.AddTriple(Subject1Subj2)
            
            let headPattern = {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                             }
            let positiveMatch1 = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s2"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                             }
            
            let positiveMatch2 = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s2"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex2
                             TriplePattern.Object = ResourceOrVariable.Variable "s"
                             }
            
            let rule =  {Head =  headPattern; Body = [ positiveMatch1 ; positiveMatch2
                                                                     ]}
            let prog = DatalogProgram([rule], tripleTable)
            prog.materialise()
            let matches = tripleTable.GetTriplesWithObject(objdIndex) |> List.ofSeq
            Assert.Equal(2, matches.Length)

              
    [<Fact>]
    let ``Non-semipositive programs are rejected`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj1 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            
            let headPattern = {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex3
                             }
            let positiveMatch = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                             }
            
            let negativeMatch = RuleAtom.NotTriple {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex2
                             }
            
            let rule =  {Head =  headPattern; Body = [ positiveMatch
                                                       negativeMatch
                                                       ]
            }
            let isSemiPositive = Stratifier.IsSemiPositiveProgram [rule]
            Assert.False isSemiPositive
    
    (*              
            [?s,:A,:o] :- [?s,:A,:o], [?s,:B,:o]
            [?s,:B,:o] :- not [?s,:C,:o2]
            :A predecessors 2, successors A
            :B predecessors 1, successors A
            :C predecessors 0, successors B
            First stratification should only include UnaryPredicate :C, :o2
            
            ?s 1u 4u
     *)
    [<Fact>]
    let rec ``Stratifier outputs first partition`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
            let predIndexA = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicateA"))
            let predIndexB = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicateB"))
            let predIndexC = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicateC"))
            let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
            
            let headPatternA = {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndexA
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                             }
            let headPatternB = {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndexB
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                             }
            let positiveMatchA = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndexA
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                             }
            
            let positiveMatchB = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndexB
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                             }
            
            let negativeMatch = RuleAtom.NotTriple {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndexC
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex2
                             }
            
            let ruleA =  {Head =  headPatternA; Body = [ positiveMatchA; positiveMatchB ]
            }
            let ruleB = {Head =  headPatternB; Body = [ negativeMatch
                                                       ]
            }
            let partitioner  = Stratifier.RulePartitioner [ruleA; ruleB]
            let ordered_relations = partitioner.GetOrderedRelations()
            ordered_relations.Should().HaveLength(3) |> ignore
            
            let init_queue = partitioner.GetReadyElementsQueue()
            init_queue.Should().HaveLength(1) |> ignore
            let first_partition = partitioner.get_rule_partition()
            first_partition.Should().Contain(ruleB).And.HaveLength(1)
    
            
    [<Fact>]
    let ``Can get matches on complex rule with negative atom`` () =
            let tripleTable = Rdf.Datastore(60u)
            let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
            let subjectIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject2"))
            let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
            let predIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate2"))
            let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
            let objdIndex3 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object3"))
            
            let Subject1Obj1 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
            let Subject2Obj1 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex; obj = objdIndex}
            let Subject2Obj2 = {Ingress.Triple.subject = subjectIndex2; predicate = predIndex2; obj = objdIndex2}
            tripleTable.AddTriple(Subject1Obj1)
            tripleTable.AddTriple(Subject2Obj1)
            tripleTable.AddTriple(Subject2Obj2)
            
            let headPattern = {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex3
                             }
            let positiveMatch = RuleAtom.PositiveTriple {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                             }
            
            let negativeMatch = RuleAtom.NotTriple {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex2
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex2
                             }
            
            let rule =  {Head =  headPattern; Body = [ positiveMatch
                                                       negativeMatch
                                                       ]
            }
            let prog = DatalogProgram([rule], tripleTable)
            prog.materialise()
            let matches = tripleTable.GetTriplesWithObject(objdIndex3)
            Assert.Single matches
          
    [<Fact>]
    let ``Can get substitution``() =
        let resource = 1u
        let variable = ResourceOrVariable.Variable "s"
        let subbed = GetSubstitution (resource, variable) (Map.empty)
        Assert.Equal(1u, subbed.Value.["s"])
    
    [<Fact>]
    let ``Can get substitution option``() =
        let resource = 1u
        let variable = ResourceOrVariable.Variable "s"
        let subbed = GetSubstitutionOption (Some Map.empty) (resource, variable) 
        Assert.Equal(1u, subbed.Value.["s"])
        
    [<Fact>]
    let ``Can get substitutions option``() =
        let fact = {Ingress.Triple.subject = 1u; predicate = 2u; obj = 3u}
        let factPattern = {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Variable "p"
                             TriplePattern.Object = ResourceOrVariable.Variable "o"}
        
        let subbed = GetSubstitutions Map.empty fact factPattern 
        Assert.Equal(1u, subbed.Value.["s"])
        Assert.Equal(2u, subbed.Value.["p"])
        Assert.Equal(3u, subbed.Value.["o"])
        
            
    [<Fact>]
    let ``Can get substitutions option map``() =
        let fact = {Ingress.Triple.subject = 1u; predicate = 2u; obj = 3u}
        let factPattern = {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource 2u
                             TriplePattern.Object = ResourceOrVariable.Variable "o"}
        
        let subbed = GetSubstitutions Map.empty fact factPattern 
        Assert.Equal(1u, subbed.Value.["s"])
        Assert.Equal(3u, subbed.Value.["o"])
        
        
                    
    [<Fact>]
    let ``Can increase substitutions option map``() =
        let fact = {Ingress.Triple.subject = 1u; predicate = 2u; obj = 3u}
        let factPattern = {
                             TriplePattern.Subject = ResourceOrVariable.Variable "s"
                             TriplePattern.Predicate = ResourceOrVariable.Resource 2u
                             TriplePattern.Object = ResourceOrVariable.Variable "o"}
        
        let subbed = GetSubstitutions (Map.empty.Add ("t", 2u)) fact factPattern 
        Assert.Equal(1u, subbed.Value.["s"])
        Assert.Equal(3u, subbed.Value.["o"])
        
    [<Fact>]
    let ``No subsititution if mismatch``() =
        let resource = 1u
        let variable = ResourceOrVariable.Variable "s"
        let subbed = GetSubstitution (resource, variable) (Map.empty.Add("s", 2u))
        Assert.Equal(None, subbed)
    
    [<Fact>]
    let ``Can add triple using rule over tripletable`` () =
        let tripleTable = Rdf.Datastore(60u)
        Assert.Equal(0u, tripleTable.Resources.ResourceCount)
        Assert.Equal(0u, tripleTable.Triples.TripleCount)
        let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
        let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
        tripleTable.AddTriple(Triple)
        Assert.Equal(3u, tripleTable.Resources.ResourceCount)
        Assert.Equal(1u, tripleTable.Triples.TripleCount)
        let mappedTriple = tripleTable.Triples.GetTriples() |> Seq.head
        Assert.Equal(Triple, mappedTriple)
        
        let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
        let Triple2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex2}
        
        let rule =  {Head =  ConstantTriplePattern Triple2; Body = [RuleAtom.PositiveTriple(ConstantTriplePattern Triple)]}
        let prog = DatalogProgram ([rule], tripleTable)
        let tripleAnswersBefore = tripleTable.GetTriplesWithSubjectPredicate(subjectIndex, predIndex)
        Assert.Equal(1, tripleAnswersBefore |> Seq.length)
        let triples2Answers = tripleAnswersBefore |> Seq.filter (fun tr -> tr = Triple2)
        Assert.Equal(0, triples2Answers |> Seq.length)
        
        prog.materialise()
        
        Assert.Equal(2u, tripleTable.Triples.TripleCount)
        let tripleAnswers2 = tripleTable.GetTriplesWithSubjectPredicate(subjectIndex, predIndex)
        Assert.Contains(Triple2, tripleAnswers2)
        
