namespace AlcTableau.Rdf
open AlcTableau.Rdf.Ingress
open Ingress

open System
open System.Collections.Generic

type TripleTable(tripleList: Triple array,
                 tripleCount: TripleListIndex,
                 threeKeysIndex: Dictionary<Triple, TripleListIndex>,
                 predicateIndex: Dictionary<ResourceId, TripleListIndex list>,
                 subjectPredicateIndex: Dictionary<ResourceId, Dictionary<ResourceId, TripleListIndex list>>,
                 objectPredicateIndex: Dictionary<ResourceId, Dictionary<ResourceId, TripleListIndex list>>) =
        
    member val TripleList = tripleList with get, set
    member val TripleCount = tripleCount with get, set
    member val ThreeKeysIndex = threeKeysIndex with get, set
    member val PredicateIndex = predicateIndex with get, set
    member val SubjectPredicateIndex = subjectPredicateIndex with get, set
    member val ObjectPredicateIndex = objectPredicateIndex with get, set

    new(init_rdf_size : uint) =
        let init_resources = max 10 (int init_rdf_size / 10)
        let init_triples = max 10 (int init_rdf_size / 60)
        TripleTable(Array.zeroCreate init_triples,
                    0u,
                    new Dictionary<Triple, TripleListIndex>(),
                    new Dictionary<ResourceId, TripleListIndex list>(),
                    new Dictionary<ResourceId, Dictionary<ResourceId, TripleListIndex list>>(),
                    new Dictionary<ResourceId, Dictionary<ResourceId, TripleListIndex list>>()
                    )
        
        
    member this.doubleTripleListSize () =
        this.TripleList <- doubleArraySize this.TripleList
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
            
    member this.AddTriple (triple : Ingress.Triple) =
            if this.ThreeKeysIndex.ContainsKey triple then
                ()
            else
                let nextTripleCount = this.TripleCount + 1u
                if nextTripleCount > uint32(this.TripleList.Length) then
                        this.doubleTripleListSize()   
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
        
       