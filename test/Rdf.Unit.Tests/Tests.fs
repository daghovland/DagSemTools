(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
module DagSemTools.Rdf.Tests

open DagSemTools.Rdf
open Xunit
open DagSemTools
open DagSemTools.Rdf.Ingress
open Faqt
open IriTools

[<Fact>]
let ``Can add resource to tripletable`` () =
    let tripleTable = Datastore(1u)
    Assert.Equal(0u, tripleTable.Resources.ResourceCount)
    let newIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com"))
    Assert.Equal(0u, newIndex)
    Assert.Equal(1u, tripleTable.Resources.ResourceCount)
    let mappedResourceId = tripleTable.GetResourceId(Ingress.Resource.Iri(new IriReference "http://example.com"))
    Assert.Equal(0u, mappedResourceId)
    let mappedResource = tripleTable.GetResource (mappedResourceId)
    Assert.Equal(Ingress.Resource.Iri(new IriReference "http://example.com"), mappedResource)
    
[<Fact>]
let ``Can add triple to tripletable`` () =
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
    
    
    
[<Fact>]
let ``Can query with subject to tripletable`` () =
    let tripleTable = Rdf.Datastore(60u)
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
    let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
    tripleTable.AddTriple(Triple)
    let query = tripleTable.GetTriplesWithSubject(subjectIndex)
    Assert.Single(query)
    
    
[<Fact>]
let ``Can query with object to tripletable`` () =
    let tripleTable = Rdf.Datastore(60u)
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
    let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
    tripleTable.AddTriple(Triple)
    let query = tripleTable.GetTriplesWithObject(objdIndex)
    Assert.Single(query)
    
    
    
[<Fact>]
let ``Can query with subject object to tripletable`` () =
    let tripleTable = Rdf.Datastore(60u)
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
    let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
    tripleTable.AddTriple(Triple)
    let query = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objdIndex)
    Assert.Single(query)
    
    
[<Fact>]
let ``Can query with resources to larger tripletable`` () =
    let tripleTable = Rdf.Datastore(60u)
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
    let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
    let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
    let Triple2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex2}
    tripleTable.AddTriple(Triple)
    tripleTable.AddTriple(Triple2)
    let squery = tripleTable.GetTriplesWithSubject(subjectIndex)
    squery
        .Should()
        .HaveLength(2, "There are two triples in the store with the same subject") |> ignore
    
    let o1query = tripleTable.GetTriplesWithObject(objdIndex)
    Assert.Single(o1query) |> ignore
    let o2query = tripleTable.GetTriplesWithObject(objdIndex2)
    Assert.Single(o2query) |> ignore
    let pquery = tripleTable.GetTriplesWithPredicate predIndex
    pquery
        .Should()
        .HaveLength(2, "There are two triples in the store with the same predicate") |> ignore
    

[<Fact>]
let ``Can get ObjectPredicate index after two triples`` () =
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
    
            let o1query = tripleTable.GetTriplesWithObject(objdIndex)
            Assert.Single(o1query) |> ignore

    
[<Fact>]
let ``Can query with subject object to larger tripletable`` () =
    let tripleTable = Rdf.Datastore(60u)
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
    let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
    let objdIndex2 = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object2"))
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
    let Triple2 = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex2}
    tripleTable.AddTriple(Triple)
    tripleTable.AddTriple(Triple2)
    let squery = tripleTable.GetTriplesWithSubject(subjectIndex)
    Assert.Equal(2, Seq.length squery)
    
    let o1query = tripleTable.GetTriplesWithObject(objdIndex)
    Assert.Single(o1query) |> ignore
    let o2query = tripleTable.GetTriplesWithObject(objdIndex2)
    Assert.Single(o2query) |> ignore
    let pquery = tripleTable.GetTriplesWithPredicate predIndex
    Assert.Equal(2, Seq.length pquery)
    let query = tripleTable.GetTriplesWithSubjectObject(subjectIndex, objdIndex)
    Assert.Single(query) |> ignore

    
[<Fact>]
let ``Can query with subject predicate when object is literal`` () =
    let tripleTable = Rdf.Datastore(60u)
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
    let objdIndex = tripleTable.AddResource(Ingress.LangLiteral("object", "en"))
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
    tripleTable.AddTriple(Triple)
    let squery = tripleTable.GetTriplesWithSubjectPredicate(subjectIndex, predIndex)
    Assert.Equal(1, Seq.length squery)
    Assert.Equal(Triple, Seq.head squery)
    let literal = tripleTable.GetResource(objdIndex)
    Assert.Equal(Ingress.LangLiteral("object", "en"), literal)


    
[<Fact>]
let ``Can query with predicate to tripletable`` () =
    let tripleTable = Rdf.Datastore(60u)
    let subjectIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/subject"))
    let predIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/predicate"))
    let objdIndex = tripleTable.AddResource(Ingress.Resource.Iri(new IriReference "http://example.com/object"))
    let Triple = {Ingress.Triple.subject = subjectIndex; predicate = predIndex; obj = objdIndex}
    tripleTable.AddTriple(Triple)
    let query = tripleTable.GetTriplesWithPredicate(predIndex)
    Assert.Single(query)