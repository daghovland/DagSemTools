module Tests

open System
open IriTools
open Rdf
open Xunit

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
    let tripleTable = Rdf.TripleTable(3u)
    Assert.Equal(0u, tripleTable.ResourceCount)
    let newIndex = tripleTable.AddResource(RDFStore.Resource.Iri(new IriReference "http://example.com"))
    Assert.Equal(0u, newIndex)
    Assert.Equal(1u, tripleTable.ResourceCount)
    let mappedResourceId = tripleTable.ResourceMap.[RDFStore.Resource.Iri(new IriReference "http://example.com")]
    Assert.Equal(0u, mappedResourceId)
    let mappedResource = tripleTable.ResourceList.[int(mappedResourceId)]
    Assert.Equal(RDFStore.Resource.Iri(new IriReference "http://example.com"), mappedResource)