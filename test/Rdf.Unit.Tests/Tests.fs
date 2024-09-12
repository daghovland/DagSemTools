
module Tests

open System
open IriTools
open AlcTableau
open AlcTableau.Rdf
open Xunit
open AlcTableau.Rdf.RDFStore

[<Fact>]
let ``Can add resource to tripletable`` () =
    let tripleTable = Rdf.TripleTable(1u)
    Assert.Equal(0u, tripleTable.ResourceCount)
    let newIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com"))
    Assert.Equal(0u, newIndex)
    Assert.Equal(1u, tripleTable.ResourceCount)
    let mappedResourceId = tripleTable.ResourceMap.[RDFStore.Resource.Iri(new IriReference "http://example.com")]
    Assert.Equal(0u, mappedResourceId)
    let mappedResource = tripleTable.ResourceList.[int(mappedResourceId)]
    Assert.Equal(RDFStore.Resource.Iri(new IriReference "http://example.com"), mappedResource)
    
[<Fact>]
let ``Can add triple to tripletable`` () =
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
    Assert.Equal(Triple, mappedTriple.triple)