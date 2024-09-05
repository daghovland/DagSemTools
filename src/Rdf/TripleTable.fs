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
                 threeKeysIndex: Dictionary<Triple, TripleListIndex>,
                 subjectIndex: TripleListLink array,
                 predicateIndex: TripleListLink array,
                 objectIndex: TripleListLink array,
                 subjectPredicateIndex: Dictionary<Tuple<ResourceId, ResourceId>, TripleListIndex>,
                 objectPredicateIndex: Dictionary<Tuple<ResourceId, ResourceId>, TripleListIndex>) =

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

    new(init_rdf_size : uint) =
        let init_resources = max 10 (int init_rdf_size / 10)
        let init_triples = max 10 (int init_rdf_size / 60)
        TripleTable(new Dictionary<Resource, ResourceId>(), Array.zeroCreate init_resources, 0u, Array.zeroCreate init_triples, 0u, new Dictionary<Triple, TripleListIndex>(), Array.zeroCreate init_resources, Array.zeroCreate init_resources, Array.zeroCreate init_resources, new Dictionary<Tuple<ResourceId, ResourceId>, TripleListIndex>(), new Dictionary<Tuple<ResourceId, ResourceId>, TripleListIndex>())
        
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
    
    member this.GetTripleListLinkedEntry (link: TripleListLink) : TripleListEntry option =
        match link with
        | TripleListLink.ArrayIndex index -> Some (this.GetTripleListEntry index)
        | TripleListLink.End -> None
    
    member this.GetTripleListEntry (index: TripleListIndex) : TripleListEntry =
        this.TripleList.[int index]
        
    member this.AddPredicateIndex (predicate: ResourceId, tripleIndex: TripleListIndex) =
        let nextIndex = this.PredicateIndex.[int(predicate)]
        this.PredicateIndex.[int(predicate)] <- TripleListLink.ArrayIndex(int(tripleIndex))
        nextIndex
    member this.AddSubjectIndex (subject: ResourceId, tripleIndex: TripleListIndex) =
        let nextIndex = this.SubjectIndex.[int(subject)]
        this.SubjectIndex.[int(subject)] <- TripleListLink.ArrayIndex(int(tripleIndex))
        nextIndex
    member this.AddObjectIndex (object: ResourceId, tripleIndex: TripleListIndex) =
        let nextIndex = this.ObjectIndex.[int(object)]
        this.ObjectIndex.[int(object)] <- TripleListLink.ArrayIndex(int(tripleIndex))
        nextIndex
    
    member this.AddSubjectPredicateIndex (subject: ResourceId, predicate: ResourceId, tripleIndex: TripleListIndex) =
        let key = Tuple.Create(subject, predicate)
        if this.SubjectPredicateIndex.ContainsKey key then
            let existIndex = this.SubjectPredicateIndex.[key]
            let existEntry = this.GetTripleListEntry existIndex
            let nextIndex = existEntry.next_subject_predicate_list
            this.SubjectPredicateIndex.[key] <- tripleIndex
            nextIndex
        else
            this.SubjectPredicateIndex.Add(key, tripleIndex) |> ignore
            this.AddSubjectIndex(subject, tripleIndex)
            
    member this.AddObjectPredicateIndex (object: ResourceId, predicate: ResourceId, tripleIndex: TripleListIndex) =
        let key = Tuple.Create(object, predicate)
        if this.ObjectPredicateIndex.ContainsKey key then
            let existIndex = this.ObjectPredicateIndex.[key]
            let existEntry = this.GetTripleListEntry existIndex
            let nextIndex = existEntry.next_subject_predicate_list
            this.ObjectPredicateIndex.[key] <- tripleIndex
            nextIndex
        else
            this.ObjectPredicateIndex.Add(key, tripleIndex) |> ignore
            this.AddObjectIndex(object, tripleIndex)
    member this.AddTriple (triple : RDFStore.Triple) =
            let nextTripleCount = this.TripleCount + 1u
            if nextTripleCount > uint32(this.TripleList.Length) then
                    this.doubleResourceListSize()
            // TODO Update indexes
            let sp_list = this.AddSubjectPredicateIndex(triple.subject, triple.predicate, int this.TripleCount)
            let op_list = this.AddObjectPredicateIndex(triple.object, triple.predicate, int this.TripleCount)
            let p_list = this.AddPredicateIndex(triple.predicate, int this.TripleCount) 
            this.TripleList.[int(this.TripleCount)] <- {
                triple = triple
                next_subject_predicate_list = sp_list
                next_predicate_list = p_list
                next_object_predicate_list = op_list
            }
            this.ThreeKeysIndex.Add(triple, int(this.TripleCount)) |> ignore
            
            this.TripleCount <- nextTripleCount
            ()
            
                