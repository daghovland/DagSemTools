namespace AlcTableau.Rdf
open AlcTableau.Rdf.RDFStore
open RDFStore

open System
open System.Collections.Generic







type TripleTable(resourceMap: Dictionary<Resource, ResourceId>,
                 resourceList: Resource array,
                 resourceCount: uint,
                 tripleList: Triple array,
                 tripleCount: TripleListIndex,
                 threeKeysIndex: Dictionary<Triple, TripleListIndex>,
                 predicateIndex: Dictionary<ResourceId, TripleListIndex list>,
                 subjectPredicateIndex: Dictionary<ResourceId, Dictionary<ResourceId, TripleListIndex list>>,
                 objectPredicateIndex: Dictionary<ResourceId, Dictionary<ResourceId, TripleListIndex list>>) =
        
    member val ResourceMap = resourceMap with get, set
    member val ResourceList = resourceList with get, set
    member val ResourceCount = resourceCount with get, set
    member val TripleList = tripleList with get, set
    member val TripleCount = tripleCount with get, set
    member val ThreeKeysIndex = threeKeysIndex with get, set
    member val PredicateIndex = predicateIndex with get, set
    member val SubjectPredicateIndex = subjectPredicateIndex with get, set
    member val ObjectPredicateIndex = objectPredicateIndex with get, set

    new(init_rdf_size : uint) =
        let init_resources = max 10 (int init_rdf_size / 10)
        let init_triples = max 10 (int init_rdf_size / 60)
        TripleTable(new Dictionary<Resource, ResourceId>(),
                    Array.zeroCreate init_resources,
                    0u,
                    Array.zeroCreate init_triples,
                    0u,
                    new Dictionary<Triple, TripleListIndex>(),
                    new Dictionary<ResourceId, TripleListIndex list>(),
                    new Dictionary<ResourceId, Dictionary<ResourceId, TripleListIndex list>>(),
                    new Dictionary<ResourceId, Dictionary<ResourceId, TripleListIndex list>>()
                    )
        
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
        
    member this.NewAnonymousBlankNode() =
        this.AddResource(RDFStore.Resource.AnonymousBlankNode this.ResourceCount);
    member this.GetTripleListEntry (index: TripleListIndex) : Triple =
        this.TripleList.[int index]
        
    member this.AddPredicateIndex (predicate: ResourceId, tripleIndex: TripleListIndex) =
        if this.PredicateIndex.ContainsKey predicate then
            let existList = this.PredicateIndex.[predicate]
            this.PredicateIndex.[predicate] <- tripleIndex :: existList
        else
            this.PredicateIndex.Add(predicate, [tripleIndex]) |> ignore
            
    member this.AddSubjectPredicateIndex (subject: ResourceId, predicate: ResourceId, tripleIndex: TripleListIndex) =
        let existSubjectMap = match  (this.SubjectPredicateIndex.TryGetValue subject) with 
                                |    true, subjMap -> subjMap
                                |    false, _ -> new Dictionary<ResourceId, TripleListIndex list>()
        let existSubjectPredicateList = match (existSubjectMap.TryGetValue predicate) with
                                        | true, subjPredList -> subjPredList
                                        | false,_ -> []
        existSubjectMap.[predicate] <- tripleIndex :: existSubjectPredicateList
        this.SubjectPredicateIndex.[subject] <- existSubjectMap
        
    member this.AddObjectPredicateIndex (object: ResourceId, predicate: ResourceId, tripleIndex: TripleListIndex) =
        let existObjectMap = match  (this.ObjectPredicateIndex.TryGetValue object) with 
                                |    true, objMap -> objMap
                                |    false, _ -> new Dictionary<ResourceId, TripleListIndex list>()
        let existSubjectPredicateList = match (existObjectMap.TryGetValue predicate) with
                                        | true, objPredList -> objPredList
                                        | false, _ -> []
        existObjectMap.[predicate] <- tripleIndex :: existSubjectPredicateList
        this.ObjectPredicateIndex.[object] <- existObjectMap
            
    member this.AddTriple (triple : RDFStore.Triple) =
            if this.ThreeKeysIndex.ContainsKey triple then
                ()
            else
                let nextTripleCount = this.TripleCount + 1u
                if nextTripleCount > uint32(this.TripleList.Length) then
                        this.doubleResourceListSize()
                this.AddSubjectPredicateIndex(triple.subject, triple.predicate, this.TripleCount)
                this.AddObjectPredicateIndex(triple.object, triple.predicate, this.TripleCount)
                this.AddPredicateIndex(triple.predicate, this.TripleCount) 
                this.TripleList.[int(this.TripleCount)] <- triple
                this.ThreeKeysIndex.Add(triple, this.TripleCount) |> ignore
                this.TripleCount <- nextTripleCount
                ()
            
        member this.GetTriplesWithSubject (subject: ResourceId) : Triple seq =
            let subjectIndex = this.SubjectPredicateIndex.[subject]
            subjectIndex |> Seq.collect (fun x -> x.Value) |> Seq.map (fun e -> this.GetTripleListEntry e) 
            
            
        member this.GetTriplesWithObject (object: ResourceId) : Triple seq =
            let objectIndex = this.ObjectPredicateIndex.[object]
            objectIndex |> Seq.collect (fun x -> x.Value) |> Seq.map (fun e -> this.GetTripleListEntry e) 
            
        member this.GetTriplesWithPredicate (predicate: ResourceId) : Triple seq =
            this.PredicateIndex.[predicate] |> Seq.map (fun e -> this.GetTripleListEntry e) 
            
        member this.GetTriplesWithSubjectPredicate (subject: ResourceId, predicate: ResourceId) =
            this.SubjectPredicateIndex.[subject].[predicate] |> Seq.map (fun e -> this.GetTripleListEntry e)
            
            
        member this.GetTriplesWithObjectPredicate (object: ResourceId, predicate: ResourceId) =
            this.ObjectPredicateIndex.[object].[predicate] |> Seq.map (fun e -> this.GetTripleListEntry e)
            
        member this.GetTriplesWithSubjectObject (subject: ResourceId, object: ResourceId) : Triple seq =
            this.GetTriplesWithSubject subject
                |> Seq.where (fun triple ->  triple.object = object)
        
       