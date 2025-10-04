(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Datalog.Tests

open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools
open DagSemTools.Rdf.Query
open IriTools
open Xunit 
open DagSemTools.Datalog
open Faqt

module PredicateGrounderTests =
    
    [<Fact>]
    let ``Multiplier removes variables`` () =
        let tripleTable = Datastore(60u)
        
        let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
        let objdIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object2"))
        let triplepattern =  {
                             TriplePattern.Subject = Term.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = Term.Resource objdIndex}
        let groundRule = PredicateGrounder.instantiateTripleWithVariableMapping triplepattern (Variable "p") predIndex
        groundRule.Predicate.Should().Be(Term.Resource predIndex)

    [<Fact>]
     let ``Multiplier removes variables from negative triple`` () =
            let tripleTable = Datastore(60u)
            
            let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
            let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object2"))
            let triplepattern =  {
                                 TriplePattern.Subject = Term.Resource subjectIndex
                                 TriplePattern.Predicate =  Variable "p"
                                 TriplePattern.Object = Term.Resource objdIndex}
            let triplepattern2 = PositiveTriple {
                             TriplePattern.Subject = Term.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = Term.Resource objdIndex2
                             }
            let rule = {Head =  NormalHead( triplepattern )
                        Body = [triplepattern2]}
            
            let groundRule = PredicateGrounder.instantiateRuleWithVariableMapping (predIndex, rule, (Variable "p"))
            match groundRule.Head with
            | Contradiction -> failwith "bug"
            | NormalHead headPattern ->  headPattern.Predicate.Should().Be(Term.Resource predIndex) |> ignore
            groundRule.Body.Should().HaveLength(1) |> ignore
            let pred = groundRule.Body.[0] |> PredicateGrounder.getAtomPredicate |> Option.get
            pred.Should().Be(predIndex)
            
      [<Fact>]
      let ``Multiplier removes variables rules`` () =
            let tripleTable = Datastore(60u)
            
            let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
            let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object2"))
            let triplepattern =  {
                                 TriplePattern.Subject = Term.Resource subjectIndex
                                 TriplePattern.Predicate =  Variable "p"
                                 TriplePattern.Object = Term.Resource objdIndex}
            let triplepattern2 = PositiveTriple {
                             TriplePattern.Subject = Term.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = Term.Resource objdIndex2
                             }
            let rule = {Head = NormalHead triplepattern; Body = [triplepattern2]}
            
            let groundRule = PredicateGrounder.instantiateRuleWithVariableMapping (predIndex, rule, (Variable "p"))
            match groundRule.Head with
            | Contradiction -> failwith "bug"
            | NormalHead headPattern ->  headPattern.Predicate.Should().Be(Term.Resource predIndex) |> ignore
            groundRule.Body.Should().HaveLength(1) |> ignore
            let pred = groundRule.Body.[0] |> PredicateGrounder.getAtomPredicate |> Option.get
            pred.Should().Be(predIndex)
            
            
        
       [<Fact>]
       let ``Multiplier changes rules from program`` () =
            let tripleTable = Datastore(60u)
            
            let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
            let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object2"))
            let triple : Triple = {
                                 subject = subjectIndex
                                 predicate =  predIndex
                                 obj =  objdIndex
                                 }
            tripleTable.AddTriple triple
            let triplepattern =  {
                                 TriplePattern.Subject = Term.Resource subjectIndex
                                 TriplePattern.Predicate =  Variable "p"
                                 TriplePattern.Object = Term.Resource objdIndex}
            let triplepattern2 = PositiveTriple {
                             TriplePattern.Subject = Term.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = Term.Resource objdIndex2
                             }
            let rule = {Head = NormalHead triplepattern; Body = [triplepattern2]}
            
            let groundRules = PredicateGrounder.groundRulePredicates ([rule], tripleTable)
            groundRules.Should().HaveLength(4) |> ignore
            let predGroundRules = groundRules
                                  |> Seq.choose (fun rule -> match rule.Head with
                                                                | Contradiction -> None
                                                                | NormalHead headPattern ->
                                                                    if headPattern.Predicate = Term.Resource predIndex then
                                                                        Some rule
                                                                    else
                                                                        None)
            predGroundRules.Should().HaveLength(1) |> ignore
            let groundRule = predGroundRules |> Seq.head
            groundRule.Body.Should().HaveLength(1) |> ignore
            let pred = groundRule.Body.[0] |> PredicateGrounder.getAtomPredicate |> Option.get
            pred.Should().Be(predIndex)
            
       [<Fact>]
        let ``Grounding adds rules from program`` () =
            let tripleTable = Datastore(60u)
            
            let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
            let objdIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object2"))
            let triplepattern =  {
                                 TriplePattern.Subject = Term.Resource subjectIndex
                                 TriplePattern.Predicate =  Variable "p"
                                 TriplePattern.Object = Term.Resource objdIndex}
            let triplepattern2 = PositiveTriple {
                             TriplePattern.Subject = Term.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = Term.Resource objdIndex2
                             }
            let rule = {Head = NormalHead triplepattern; Body = [triplepattern2]}
            
            let groundRules = PredicateGrounder.groundRulePredicates ([rule], tripleTable)
            groundRules.Should().HaveLength(3) |> ignore
            
            
        [<Fact>]
         let ``Multiplier can get predicates from rule`` () =
            let tripleTable = Datastore(60u)
            
            let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
            let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object2"))
            let triplepattern =  {
                                 TriplePattern.Subject = Term.Resource subjectIndex
                                 TriplePattern.Predicate =  Term.Resource predIndex
                                 TriplePattern.Object = Term.Resource objdIndex}
            let triplepattern2 = PositiveTriple {
                             TriplePattern.Subject = Term.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = Term.Resource objdIndex2
                             }
            let rule = {Head = NormalHead triplepattern; Body = [triplepattern2]}
            
            let predicate = PredicateGrounder.getRuleHeadPredicate rule.Head
            predicate.Should().Be(Some(predIndex)) |> ignore
            
            
         [<Fact>]
          let ``Multiplier can get predicates from program`` () =
            let tripleTable = Datastore(60u)
            
            let subjectIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/subject"))
            let predIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/predicate"))
            let objdIndex = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object"))
            let objdIndex2 = tripleTable.AddNodeResource(Ingress.RdfResource.Iri(new IriReference "http://example.com/object2"))
            let triple : Triple = {
                                 subject = subjectIndex
                                 predicate =  predIndex
                                 obj =  objdIndex
                                 }
            tripleTable.AddTriple triple
            let triplepattern =  {
                                 TriplePattern.Subject = Term.Resource subjectIndex
                                 TriplePattern.Predicate =  Variable "p"
                                 TriplePattern.Object = Term.Resource objdIndex}
            let triplepattern2 = PositiveTriple {
                             TriplePattern.Subject = Term.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = Term.Resource objdIndex2
                            }
            let rule = {Head = NormalHead triplepattern; Body = [triplepattern2]}
            
            let predicates = PredicateGrounder.getPredicatesInUse ([rule], tripleTable)
            predicates.Should().HaveLength(4) |> ignore