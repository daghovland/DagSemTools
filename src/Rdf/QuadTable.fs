namespace DagSemTools.Rdf
open Ingress

open System
open System.Collections.Generic

type QuadTable(quadList: Quad array,
                 quadCount: TripleListIndex,
                 fourKeysIndex: Dictionary<Quad, QuadListIndex>,
                 tripleIndex: Dictionary<Triple, QuadListIndex>,
                 tripleIdIndex: Dictionary<ResourceId, QuadListIndex list>,
                 predicateIndex: Dictionary<ResourceId, QuadListIndex list>,
                 subjectPredicateIndex: Dictionary<ResourceId, Dictionary<ResourceId, QuadListIndex list>>,
                 objectPredicateIndex: Dictionary<ResourceId, Dictionary<ResourceId, QuadListIndex list>>) =
        
    member val QuadList = quadList with get, set
    member val QuadCount = quadCount with get, set
    member val FourKeysIndex = fourKeysIndex with get, set
    member val TripleIndex = tripleIndex with get, set
    member val TripleIdIndex = tripleIdIndex with get, set
    member val PredicateIndex = predicateIndex with get, set
    member val SubjectPredicateIndex = subjectPredicateIndex with get, set
    member val ObjectPredicateIndex = objectPredicateIndex with get, set

    new(init_rdf_size : uint) =
        let init_resources = max 10 (int init_rdf_size / 10)
        let init_triples = max 10 (int init_rdf_size / 60)
        QuadTable(Array.zeroCreate init_triples,
                    0u,
                    new Dictionary<Quad, QuadListIndex>(),
                    new Dictionary<Triple, QuadListIndex>(),
                    new Dictionary<ResourceId, QuadListIndex list>(),
                    new Dictionary<ResourceId, QuadListIndex list>(),
                    new Dictionary<ResourceId, Dictionary<ResourceId, QuadListIndex list>>(),
                    new Dictionary<ResourceId, Dictionary<ResourceId, QuadListIndex list>>()
                    )
        
        
    member this.doubleQuadListSize () =
        this.QuadList <- doubleArraySize this.QuadList
    member this.GetQuadListEntry (index: QuadListIndex) : Quad =
        this.QuadList.[int index]
    
    
    member this.AddTripleIdIndex (id: ResourceId, tripleIndex: QuadListIndex) =
        if this.TripleIdIndex.ContainsKey id then
            let existList = this.TripleIdIndex.[id]
            this.TripleIdIndex.[id] <- tripleIndex :: existList
        else
            this.TripleIdIndex.Add(id, [tripleIndex]) |> ignore
            
    member this.AddPredicateIndex (predicate: ResourceId, tripleIndex: QuadListIndex) =
        if this.PredicateIndex.ContainsKey predicate then
            let existList = this.PredicateIndex.[predicate]
            this.PredicateIndex.[predicate] <- tripleIndex :: existList
        else
            this.PredicateIndex.Add(predicate, [tripleIndex]) |> ignore
            
    member this.AddSubjectPredicateIndex (subject: ResourceId, predicate: ResourceId, tripleIndex: QuadListIndex) =
        let existSubjectMap = match  (this.SubjectPredicateIndex.TryGetValue subject) with 
                                |    true, subjMap -> subjMap
                                |    false, _ -> new Dictionary<ResourceId, QuadListIndex list>()
        let existSubjectPredicateList = match (existSubjectMap.TryGetValue predicate) with
                                        | true, subjPredList -> subjPredList
                                        | false,_ -> []
        existSubjectMap.[predicate] <- tripleIndex :: existSubjectPredicateList
        this.SubjectPredicateIndex.[subject] <- existSubjectMap
        
    member this.AddObjectPredicateIndex (object: ResourceId, predicate: ResourceId, tripleIndex: QuadListIndex) =
        let existObjectMap = match  (this.ObjectPredicateIndex.TryGetValue object) with 
                                |    true, objMap -> objMap
                                |    false, _ -> new Dictionary<ResourceId, QuadListIndex list>()
        let existSubjectPredicateList = match (existObjectMap.TryGetValue predicate) with
                                        | true, objPredList -> objPredList
                                        | false, _ -> []
        existObjectMap.[predicate] <- tripleIndex :: existSubjectPredicateList
        this.ObjectPredicateIndex.[object] <- existObjectMap
            
    member this.AddQuad (quad : Ingress.Quad) =
            if this.FourKeysIndex.ContainsKey quad then
                ()
            else
                let nextQuadCount = this.QuadCount + 1u
                if nextQuadCount > uint32(this.QuadList.Length) then
                        this.doubleQuadListSize()   
                this.AddSubjectPredicateIndex(quad.subject, quad.predicate, this.QuadCount)
                this.AddObjectPredicateIndex(quad.object, quad.predicate, this.QuadCount)
                this.AddPredicateIndex(quad.predicate, this.QuadCount)
                this.AddTripleIdIndex(quad.tripleId, this.QuadCount)
                this.QuadList.[int(this.QuadCount)] <- quad
                this.FourKeysIndex.Add(quad, this.QuadCount) |> ignore
                this.QuadCount <- nextQuadCount
                ()
            
        member this.GetQuadsWithSubject (subject: ResourceId) : Quad seq =
            let subjectIndex = this.SubjectPredicateIndex.[subject]
            subjectIndex |> Seq.collect (fun x -> x.Value) |> Seq.map (fun e -> this.GetQuadListEntry e) 
            
            
        member this.GetQuadsWithObject (object: ResourceId) : Quad seq =
            let objectIndex = this.ObjectPredicateIndex.[object]
            objectIndex |> Seq.collect (fun x -> x.Value) |> Seq.map (fun e -> this.GetQuadListEntry e) 
            
        member this.GetQuadsWithPredicate (predicate: ResourceId) : Quad seq =
            this.PredicateIndex.[predicate] |> Seq.map (fun e -> this.GetQuadListEntry e) 
        
        member this.GetQuadsWithId (id: ResourceId) : Quad seq =
            this.TripleIdIndex.[id] |> Seq.map (fun e -> this.GetQuadListEntry e) 
            
        member this.GetQuadsWithSubjectPredicate (subject: ResourceId, predicate: ResourceId) =
            this.SubjectPredicateIndex.[subject].[predicate] |> Seq.map (fun e -> this.GetQuadListEntry e)
            
            
        member this.GetQuadsWithObjectPredicate (object: ResourceId, predicate: ResourceId) =
            this.ObjectPredicateIndex.[object].[predicate] |> Seq.map (fun e -> this.GetQuadListEntry e)
            
        member this.GetQuadsWithSubjectObject (subject: ResourceId, object: ResourceId) : Quad seq =
            this.GetQuadsWithSubject subject
                |> Seq.where (fun triple ->  triple.object = object)
        
       