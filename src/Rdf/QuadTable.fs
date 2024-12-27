namespace DagSemTools.Rdf
open Ingress

open System
open System.Collections.Generic

type QuadTable(quadList: Quad array,
                 quadCount: TripleListIndex,
                 fourKeysIndex: Dictionary<Quad, QuadListIndex>,
                 tripleIndex: Dictionary<Triple, QuadListIndex>,
                 tripleIdIndex: Dictionary<GraphElementId, QuadListIndex list>,
                 predicateIndex: Dictionary<GraphElementId, QuadListIndex list>,
                 subjectPredicateIndex: Dictionary<GraphElementId, Dictionary<GraphElementId, QuadListIndex list>>,
                 objectPredicateIndex: Dictionary<GraphElementId, Dictionary<GraphElementId, QuadListIndex list>>) =
        
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
                    new Dictionary<GraphElementId, QuadListIndex list>(),
                    new Dictionary<GraphElementId, QuadListIndex list>(),
                    new Dictionary<GraphElementId, Dictionary<GraphElementId, QuadListIndex list>>(),
                    new Dictionary<GraphElementId, Dictionary<GraphElementId, QuadListIndex list>>()
                    )
        
        
    member this.doubleQuadListSize () =
        this.QuadList <- doubleArraySize this.QuadList
    member this.GetQuadListEntry (index: QuadListIndex) : Quad =
        this.QuadList.[int index]
    
    
    member this.AddTripleIdIndex (id: GraphElementId, tripleIndex: QuadListIndex) =
        if this.TripleIdIndex.ContainsKey id then
            let existList = this.TripleIdIndex.[id]
            this.TripleIdIndex.[id] <- tripleIndex :: existList
        else
            this.TripleIdIndex.Add(id, [tripleIndex]) |> ignore
            
    member this.AddPredicateIndex (predicate: GraphElementId, tripleIndex: QuadListIndex) =
        if this.PredicateIndex.ContainsKey predicate then
            let existList = this.PredicateIndex.[predicate]
            this.PredicateIndex.[predicate] <- tripleIndex :: existList
        else
            this.PredicateIndex.Add(predicate, [tripleIndex]) |> ignore
            
    member this.AddSubjectPredicateIndex (subject: GraphElementId, predicate: GraphElementId, tripleIndex: QuadListIndex) =
        let existSubjectMap = match  (this.SubjectPredicateIndex.TryGetValue subject) with 
                                |    true, subjMap -> subjMap
                                |    false, _ -> new Dictionary<GraphElementId, QuadListIndex list>()
        let existSubjectPredicateList = match (existSubjectMap.TryGetValue predicate) with
                                        | true, subjPredList -> subjPredList
                                        | false,_ -> []
        existSubjectMap.[predicate] <- tripleIndex :: existSubjectPredicateList
        this.SubjectPredicateIndex.[subject] <- existSubjectMap
        
    member this.AddObjectPredicateIndex (object: GraphElementId, predicate: GraphElementId, tripleIndex: QuadListIndex) =
        let existObjectMap = match  (this.ObjectPredicateIndex.TryGetValue object) with 
                                |    true, objMap -> objMap
                                |    false, _ -> new Dictionary<GraphElementId, QuadListIndex list>()
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
                this.AddObjectPredicateIndex(quad.obj, quad.predicate, this.QuadCount)
                this.AddPredicateIndex(quad.predicate, this.QuadCount)
                this.AddTripleIdIndex(quad.tripleId, this.QuadCount)
                this.QuadList.[int(this.QuadCount)] <- quad
                this.FourKeysIndex.Add(quad, this.QuadCount) |> ignore
                this.QuadCount <- nextQuadCount
                ()
            
        member this.GetQuadsWithSubject (subject: GraphElementId) : Quad seq =
            let subjectIndex = this.SubjectPredicateIndex.[subject]
            subjectIndex |> Seq.collect (fun x -> x.Value) |> Seq.map (fun e -> this.GetQuadListEntry e) 
            
            
        member this.GetQuadsWithObject (object: GraphElementId) : Quad seq =
            let objectIndex = this.ObjectPredicateIndex.[object]
            objectIndex |> Seq.collect (fun x -> x.Value) |> Seq.map (fun e -> this.GetQuadListEntry e) 
            
        member this.GetQuadsWithPredicate (predicate: GraphElementId) : Quad seq =
            this.PredicateIndex.[predicate] |> Seq.map (fun e -> this.GetQuadListEntry e) 
        
        member this.GetQuadsWithId (id: GraphElementId) : Quad seq =
            this.TripleIdIndex.[id] |> Seq.map (fun e -> this.GetQuadListEntry e) 
            
        member this.GetQuadsWithSubjectPredicate (subject: GraphElementId, predicate: GraphElementId) =
            this.SubjectPredicateIndex.[subject].[predicate] |> Seq.map (fun e -> this.GetQuadListEntry e)
            
            
        member this.GetQuadsWithObjectPredicate (object: GraphElementId, predicate: GraphElementId) =
            this.ObjectPredicateIndex.[object].[predicate] |> Seq.map (fun e -> this.GetQuadListEntry e)
            
        member this.GetQuadsWithSubjectObject (subject: GraphElementId, object: GraphElementId) : Quad seq =
            this.GetQuadsWithSubject subject
                |> Seq.where (fun triple ->  triple.obj = object)
        
       