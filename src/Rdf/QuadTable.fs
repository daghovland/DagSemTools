(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

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
        
    member val internal QuadList = quadList with get, set
    member val internal QuadCount = quadCount with get, set
    member val internal FourKeysIndex = fourKeysIndex with get, set
    member val internal TripleIndex = tripleIndex with get, set
    member val internal TripleIdIndex = tripleIdIndex with get, set
    member val internal PredicateIndex = predicateIndex with get, set
    member val internal SubjectPredicateIndex = subjectPredicateIndex with get, set
    member val internal ObjectPredicateIndex = objectPredicateIndex with get, set

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
        
        
    member internal this.doubleQuadListSize () =
        this.QuadList <- doubleArraySize this.QuadList
    member internal this.GetQuadListEntry (index: QuadListIndex) : Quad =
        this.QuadList.[int index]
    
    
    member internal this.AddTripleIdIndex (id: GraphElementId, tripleIndex: QuadListIndex) =
        if this.TripleIdIndex.ContainsKey id then
            let existList = this.TripleIdIndex.[id]
            this.TripleIdIndex.[id] <- tripleIndex :: existList
        else
            this.TripleIdIndex.Add(id, [tripleIndex]) |> ignore
            
    member internal this.AddPredicateIndex (predicate: GraphElementId, tripleIndex: QuadListIndex) =
        if this.PredicateIndex.ContainsKey predicate then
            let existList = this.PredicateIndex.[predicate]
            this.PredicateIndex.[predicate] <- tripleIndex :: existList
        else
            this.PredicateIndex.Add(predicate, [tripleIndex]) |> ignore
            
    member internal this.AddSubjectPredicateIndex (subject: GraphElementId, predicate: GraphElementId, tripleIndex: QuadListIndex) =
        let existSubjectMap = match  (this.SubjectPredicateIndex.TryGetValue subject) with 
                                |    true, subjMap -> subjMap
                                |    false, _ -> new Dictionary<GraphElementId, QuadListIndex list>()
        let existSubjectPredicateList = match (existSubjectMap.TryGetValue predicate) with
                                        | true, subjPredList -> subjPredList
                                        | false,_ -> []
        existSubjectMap.[predicate] <- tripleIndex :: existSubjectPredicateList
        this.SubjectPredicateIndex.[subject] <- existSubjectMap
        
    member internal this.AddObjectPredicateIndex (object: GraphElementId, predicate: GraphElementId, tripleIndex: QuadListIndex) =
        let existObjectMap = match  (this.ObjectPredicateIndex.TryGetValue object) with 
                                |    true, objMap -> objMap
                                |    false, _ -> new Dictionary<GraphElementId, QuadListIndex list>()
        let existSubjectPredicateList = match (existObjectMap.TryGetValue predicate) with
                                        | true, objPredList -> objPredList
                                        | false, _ -> []
        existObjectMap.[predicate] <- tripleIndex :: existSubjectPredicateList
        this.ObjectPredicateIndex.[object] <- existObjectMap
            
    member internal this.AddQuad (quad : Ingress.Quad) =
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
            
        member internal this.GetQuadsWithSubject (subject: GraphElementId) : Quad seq =
            match this.SubjectPredicateIndex.TryGetValue subject with  
            | true, subjectIndex -> subjectIndex |> Seq.collect (fun x -> x.Value) |> Seq.map (fun e -> this.GetQuadListEntry e)
            | false, _ -> []
            
            
        member internal this.GetQuadsWithObject (object: GraphElementId) : Quad seq =
            match this.ObjectPredicateIndex.TryGetValue object with 
            | true, objectIndex -> objectIndex |> Seq.collect (fun x -> x.Value) |> Seq.map (fun e -> this.GetQuadListEntry e)
            | false, _ -> []
            
        member internal this.GetQuadsWithPredicate (predicate: GraphElementId) : Quad seq =
            match this.PredicateIndex.TryGetValue predicate with
            | true, predicates -> predicates |> Seq.map (fun e -> this.GetQuadListEntry e)
            | false, _ -> []
        
        member internal this.GetGraph (id: GraphElementId) : Quad seq =
            this.TripleIdIndex.[id] |> Seq.map (fun e -> this.GetQuadListEntry e) 
            
        member internal this.GetQuadsWithSubjectPredicate (subject: GraphElementId, predicate: GraphElementId) =
            this.SubjectPredicateIndex.[subject].[predicate] |> Seq.map (fun e -> this.GetQuadListEntry e)
            
            
        member internal this.GetQuadsWithObjectPredicate (object: GraphElementId, predicate: GraphElementId) =
            this.ObjectPredicateIndex.[object].[predicate] |> Seq.map (fun e -> this.GetQuadListEntry e)
            
        member internal this.GetQuadsWithSubjectObject (subject: GraphElementId, object: GraphElementId) : Quad seq =
            this.GetQuadsWithSubject subject
                |> Seq.where (fun triple ->  triple.obj = object)
        
       member internal this.GetQuadsWithIdSubject (id: GraphElementId, subject: GraphElementId) : Quad seq =
            this.GetQuadsWithSubject subject
                |> Seq.where (fun quad -> quad.tripleId = id) 
        
       member internal this.GetQuadsWithIdPredicate (id: GraphElementId, predicate: GraphElementId) : Quad seq =
            this.GetQuadsWithPredicate predicate
                |> Seq.where (fun quad -> quad.tripleId = id) 
       
       member internal this.GetQuadsWithIdObject (id: GraphElementId, obj: GraphElementId) : Quad seq =
            this.GetQuadsWithObject obj
                |> Seq.where (fun quad -> quad.tripleId = id) 
       member internal this.GetQuadsWithIdSubjectPredicate (id: GraphElementId, subject: GraphElementId, predicate: GraphElementId) : Quad seq =
            this.GetQuadsWithSubjectPredicate (subject, predicate)
                |> Seq.where (fun quad -> quad.tripleId = id) 
       
       member internal this.GetQuadsWithIdSubjectObject (id: GraphElementId, subject: GraphElementId, object: GraphElementId) : Quad seq =
            this.GetQuadsWithSubjectObject (subject, object)
                |> Seq.where (fun quad -> quad.tripleId = id)
                
        member internal this.GetQuadsWithIdObjectPredicate (id: GraphElementId, object: GraphElementId, predicate: GraphElementId) : Quad seq =
            this.GetQuadsWithObjectPredicate (object, predicate)
                |> Seq.where (fun quad -> quad.tripleId = id)