(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace AlcTableau.Datalog.Tests

open System
open AlcTableau.Rdf
open IriTools
open Xunit 
open RDFStore
open Faqt
open AlcTableau.Rdf.RDFStore
open AlcTableau
open AlcTableau.Datalog

module Tests =
    
    
    [<Fact>]
    let ``Datalog program fetches rule`` () =
        let tripleTable = Rdf.TripleTable(60u)
        
        let subjectIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/object"))
        let objdIndex2 = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/object2"))
        let triplepattern = {
                             TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex}
        let triplepattern2 = {
                             TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex2
                             }
        let tripleFact : Triple = {
                             subject = subjectIndex
                             predicate =  predIndex
                             object = objdIndex2
                             }
        
        let rule = {Head =  triplepattern; Body = [triplepattern2]}
        let prog = DatalogProgram ([rule], tripleTable)
        let rules = prog.GetRulesForFact tripleFact
        Assert.Single(rules)
        
    let ``Datalog program does not fetch when no matching rule`` () =
        let tripleTable = Rdf.TripleTable(60u)
        
        let subjectIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/object"))
        let objdIndex2 = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/object2"))
        let objdIndex3 = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/object3"))
        let triplepattern = {
                             TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex}
        let triplepattern2 = {
                             TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                             TriplePattern.Predicate =  Variable "p"
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex2
                             }
        let tripleFact : Triple = {
                             subject = subjectIndex
                             predicate =  predIndex
                             object = objdIndex3
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
        let tripleTable = Rdf.TripleTable(60u)
        
        let subjectIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/object"))
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
        let tripleTable = Rdf.TripleTable(60u)
        
        let subjectIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/object"))
        let triplepattern = {TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                             TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                             TriplePattern.Object = ResourceOrVariable.Resource objdIndex}
        let wildcards = WildcardTriplePattern triplepattern
        Assert.Equal(8, wildcards.Length)
        
    [<Fact>]
    let ``Can get matches on rule`` () =
        let tripleTable = Rdf.TripleTable(60u)
        Assert.Equal(0u, tripleTable.ResourceCount)
        Assert.Equal(0u, tripleTable.TripleCount)
        let subjectIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/object"))
        let Triple = {RDFStore.Triple.subject = subjectIndex; predicate = predIndex; object = objdIndex}
        tripleTable.AddTriple(Triple)
        Assert.Equal(3u, tripleTable.ResourceCount)
        Assert.Equal(1u, tripleTable.TripleCount)
        let mappedTriple = tripleTable.TripleList.[0]
        Assert.Equal(Triple, mappedTriple)
        
        let objdIndex2 = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/object2"))
        let Triple2 = {RDFStore.Triple.subject = subjectIndex; predicate = predIndex; object = objdIndex2}
        
        let rule =  {Head =  ConstantTriplePattern Triple2; Body = [ConstantTriplePattern Triple]}
        let TriplePatter = {
                            TriplePattern.Subject = ResourceOrVariable.Resource subjectIndex
                            TriplePattern.Predicate = ResourceOrVariable.Resource predIndex
                            TriplePattern.Object = ResourceOrVariable.Resource objdIndex
                            }
        let partialRule = {Rule = rule; Match =  TriplePatter}
        let matches = GetMatchesForRule Triple partialRule
        Assert.Single matches
   
         
        
    [<Fact>]
    let ``Can add triple using rule over tripletable`` () =
        let tripleTable = Rdf.TripleTable(60u)
        Assert.Equal(0u, tripleTable.ResourceCount)
        Assert.Equal(0u, tripleTable.TripleCount)
        let subjectIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/subject"))
        let predIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/predicate"))
        let objdIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/object"))
        let Triple = {RDFStore.Triple.subject = subjectIndex; predicate = predIndex; object = objdIndex}
        tripleTable.AddTriple(Triple)
        Assert.Equal(3u, tripleTable.ResourceCount)
        Assert.Equal(1u, tripleTable.TripleCount)
        let mappedTriple = tripleTable.TripleList.[0]
        Assert.Equal(Triple, mappedTriple)
        
        let objdIndex2 = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com/object2"))
        let Triple2 = {RDFStore.Triple.subject = subjectIndex; predicate = predIndex; object = objdIndex2}
        
        let rule =  {Head =  ConstantTriplePattern Triple2; Body = [ConstantTriplePattern Triple]}
        let prog = DatalogProgram ([rule], tripleTable)
        let tripleAnswersBefore = tripleTable.GetTriplesWithSubjectPredicate(subjectIndex, predIndex)
        Assert.Equal(1, tripleAnswersBefore |> Seq.length)
        let triples2Answers = tripleAnswersBefore |> Seq.filter (fun tr -> tr = Triple2)
        Assert.Equal(0, triples2Answers |> Seq.length)
        
        prog.materialise()
        
        Assert.Equal(2u, tripleTable.TripleCount)
        let tripleAnswers2 = tripleTable.GetTriplesWithSubjectPredicate(subjectIndex, predIndex)
        Assert.Contains(Triple2, tripleAnswers2)
        
