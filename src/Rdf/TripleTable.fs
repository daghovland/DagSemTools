namespace Rdf
open RDFStore

open System
open System.Collections.Generic
open Rdf.RDFStore



type TripleTable(resourceMap: Dictionary<Resource, ResourceId>,
                 resourceList: Resource array,
                 resourceCount: uint,
                 tripleList: TripleListEntry array,
                 tripleCount: uint,
                 threeKeysIndex: Dictionary<Triple, TripleLookup>,
                 subjectIndex: TripleLookup array,
                 predicateIndex: TripleLookup array,
                 objectIndex: TripleLookup array,
                 subjectPredicateIndex: Dictionary<Tuple<ResourceId, ResourceId>, TripleLookup>,
                 objectPredicateIndex: Dictionary<Tuple<ResourceId, ResourceId>, TripleLookup>) =

    member val ResourceMap = resourceMap with get, set
    member val ResourceList = resourceList with get, set
    member val ResourceCount = resourceCount with get, set
    member val TripleList = tripleList with get, set
    member val TripleCount = tripleCount with get, set
    member val ThreeKeysIndex = threeKeysIndex with get, set
    member val SubjectIndex = subjectIndex with get, set
    member val PredicateIndex = predicateIndex with get, set
    member val ObjectIndex = objectIndex with get, set
    member val SubjectPredicateIndex = subjectPredicateIndex with get, set
    member val ObjectPredicateIndex = objectPredicateIndex with get, set

    new() = 
        TripleTable(new Dictionary<Resource, ResourceId>(), Array.empty, 0u, Array.empty, 0u, new Dictionary<Triple, TripleLookup>(), Array.empty, Array.empty, Array.empty, new Dictionary<Tuple<ResourceId, ResourceId>, TripleLookup>(), new Dictionary<Tuple<ResourceId, ResourceId>, TripleLookup>())
        
    member this.doubleArraySize (originalArray: 'T array) : 'T array =
        let newSize = originalArray.Length * 2
        let newArray = Array.zeroCreate<'T> newSize
        Array.blit originalArray 0 newArray 0 originalArray.Length
        newArray    
         
    member this.doubleResourceListSize () =
        this.ResourceList <- this.doubleArraySize this.ResourceList
        
        
    member this.doubleTripleListSize () =
        this.TripleList <- this.doubleArraySize this.TripleList
        
    member this.AddResource resource =
        if this.ResourceMap.ContainsKey resource then
            this.ResourceMap[resource]
        else
            let nextResourceIndex = this.ResourceCount + 1u
            if nextResourceIndex > uint32(this.ResourceList.Length) then
                    this.doubleResourceListSize()
            this.ResourceList.[int(this.ResourceCount)] <- resource
            this.ResourceMap.Add (resource, this.ResourceCount) |> ignore
            this.ResourceCount <- nextResourceIndex
            nextResourceIndex - 1u
       
    member this.AddTriple triple =
            let nextTripleCount = this.TripleCount + 1u
            if nextTripleCount > uint32(this.TripleList.Length) then
                    this.doubleResourceListSize()
            this.TripleList.[int(this.TripleCount)] <- triple
            this.ResourceCount <- nextTripleCount
            ()
            
                