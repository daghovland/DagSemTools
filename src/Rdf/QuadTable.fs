(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.Rdf

open DagSemTools.Rdf.Ingress
open Ingress

open System
open System.Collections.Generic

type QuadTable(quadList: Quad array,
                 quadCount: TripleListIndex,
                 fourKeysIndex: Dictionary<Quad, QuadListIndex>,
                 tripleIndex: Dictionary<Triple, QuadListIndex>,
                 tripleIdIndex: Dictionary<GraphElementId, QuadListIndex list>,
                 graphSubjectIndex: Dictionary<GraphSubjectKey, QuadListIndex list>,
                 graphPredicateIndex: Dictionary<GraphSubjectKey, QuadListIndex list>,
                 graphObjectIndex: Dictionary<GraphSubjectKey, QuadListIndex list>) =
        
    
    member val internal GraphSubjectIndex = graphSubjectIndex with get, set
    member val internal GraphObjectIndex = graphObjectIndex with get, set
    member val internal GraphPredicateIndex = graphPredicateIndex with get, set
    member val internal QuadList = quadList with get, set
    member val internal QuadCount = quadCount with get, set
    member val internal FourKeysIndex = fourKeysIndex with get, set
    member val internal TripleIndex = tripleIndex with get, set
    member val internal TripleIdIndex = tripleIdIndex with get, set
    
    new(init_rdf_size : uint) =
        let init_resources = max 10 (int init_rdf_size / 10)
        let init_triples = max 10 (int init_rdf_size / 60)
        QuadTable(Array.zeroCreate init_triples,
                    0u,
                    new Dictionary<Quad, QuadListIndex>(),
                    new Dictionary<Triple, QuadListIndex>(),
                    new Dictionary<GraphElementId, QuadListIndex list>(),
                    new Dictionary<GraphSubjectKey, QuadListIndex list>(),
                    new Dictionary<GraphSubjectKey, QuadListIndex list>(),
                    new Dictionary<GraphSubjectKey, QuadListIndex list>()
                    )
        
    member private this.doubleQuadListSize () =
        this.QuadList <- doubleArraySize this.QuadList
    member internal this.GetQuadListEntry (index: QuadListIndex) : Quad =
        this.QuadList.[int index]
    member val private ResourceGraphMap = new Dictionary<GraphElementId, HashSet<GraphElementId>>()

    member internal this.RegisterResourceInGraph (resId: GraphElementId, graphId: GraphElementId) =
        match this.ResourceGraphMap.TryGetValue resId with
        | true, graphSet -> 
            graphSet.Add(graphId) |> ignore
        | false, _ -> 
            let newSet = HashSet<GraphElementId>()
            newSet.Add(graphId) |> ignore
            this.ResourceGraphMap.Add(resId, newSet)

    member internal this.AddTripleIdIndex (id: GraphElementId, tripleIndex: QuadListIndex) =
        match this.TripleIdIndex.TryGetValue id with
        | true, existList -> 
            this.TripleIdIndex.[id] <- tripleIndex :: existList
        | false, _ ->
            this.TripleIdIndex.Add(id, [tripleIndex]) |> ignore
            
    member internal this.AddSPOIndex (index : Dictionary<GraphSubjectKey, QuadListIndex list>) indexKey (graphId : GraphElementId, predicate: GraphElementId, tripleIndex: QuadListIndex) =
        match index.TryGetValue indexKey with
        | true, existList ->
            index.[indexKey] <- tripleIndex :: existList
        | false, _ ->
            index.Add(indexKey, [tripleIndex]) |> ignore
                    
    member internal this.AddPredicateIndex (graphId : GraphElementId, predicate: GraphElementId, tripleIndex: QuadListIndex) =
        let indexKey = {GraphSubjectKey.Graph = graphId; Subject = predicate}
        this.AddSPOIndex this.GraphPredicateIndex indexKey (graphId, predicate, tripleIndex)
            
    member internal this.AddSubjectIndex (graphId : GraphElementId, subject: GraphElementId, tripleIndex: QuadListIndex) =
        let indexKey = {GraphSubjectKey.Graph = graphId; Subject = subject }
        this.AddSPOIndex this.GraphSubjectIndex indexKey (graphId, subject, tripleIndex)
    
    member internal this.AddObjectIndex (graphId : GraphElementId, object: GraphElementId, tripleIndex: QuadListIndex) =
        let indexKey = {GraphSubjectKey.Graph = graphId; Subject = object }
        this.AddSPOIndex this.GraphObjectIndex indexKey (graphId, object, tripleIndex)
            
    member internal this.AddQuad (quad : Ingress.Quad) =
            if this.FourKeysIndex.ContainsKey quad then
                ()
            else
                let nextQuadCount = this.QuadCount + 1u
                if nextQuadCount > uint32(this.QuadList.Length) then
                        this.doubleQuadListSize()   
                this.AddSubjectIndex(quad.tripleId, quad.subject, this.QuadCount)
                this.AddPredicateIndex(quad.tripleId, quad.predicate, this.QuadCount)
                this.AddObjectIndex(quad.tripleId, quad.obj, this.QuadCount)
                this.AddTripleIdIndex(quad.tripleId, this.QuadCount)
                this.RegisterResourceInGraph (quad.subject, quad.tripleId)
                this.RegisterResourceInGraph (quad.obj, quad.tripleId)
                this.QuadList.[int(this.QuadCount)] <- quad
                this.FourKeysIndex.Add(quad, this.QuadCount) |> ignore
                this.QuadCount <- nextQuadCount
                ()
            
        member internal this.Contains(q : Quad) =
            this.FourKeysIndex.ContainsKey q
            
        member internal this.GetQuadsWithSubject (subject: GraphElementId) : Quad seq =
            match this.ResourceGraphMap.TryGetValue subject with
            | true, graphSet -> graphSet
                                |> Seq.collect (fun x -> this.GraphSubjectIndex.[{Graph = x; Subject = subject}])
                                |> Seq.map (fun e -> this.GetQuadListEntry e)
            | false, _ -> []
       
        member internal this.GetQuadsWithObject (object: GraphElementId) : Quad seq =
            match this.ResourceGraphMap.TryGetValue object with
            | true, graphSet -> graphSet
                                |> Seq.collect (fun x -> this.GraphObjectIndex.[{Graph = x; Subject = object}])
                                |> Seq.map (fun e -> this.GetQuadListEntry e)
            | false, _ -> []
        member internal this.GetQuadsWithPredicate (predicate: GraphElementId) : Quad seq =
            this.TripleIdIndex.Keys
            |> Seq.collect (fun e -> this.GetQuadsWithIdPredicate (e, predicate))
       
        member internal this.GetQuadsWithSubjectPredicate (subject, predicate) =
            this.GetQuadsWithSubject subject
            |> Seq.where (fun q -> q.predicate = predicate)
        member internal this.GetQuadsWithObjectPredicate (object, predicate) =
            this.GetQuadsWithObject object
            |> Seq.where (fun q -> q.predicate = predicate)
        member internal this.GetQuadsWithSubjectObject (subject, object) =
            let subjectMatch = this.GetQuadsWithSubject subject
            let objectMatch = this.GetQuadsWithObject object
            if (Seq.length subjectMatch < (Seq.length objectMatch)) then
                subjectMatch |> Seq.where (fun q -> q.predicate = object)
            else
                objectMatch |> Seq.where (fun q -> q.subject  = subject)
        
        member internal this.GetQuadsWithIdSubject (id: GraphElementId, subject: GraphElementId) : Quad seq =
            match this.GraphSubjectIndex.TryGetValue {Graph = id; Subject = subject} with
            | true, quads -> quads
                            |> Seq.map (fun e -> this.GetQuadListEntry e)
            | false, _ -> []

       member internal this.GetQuadsWithIdPredicate (id: GraphElementId, predicate: GraphElementId) : Quad seq =
            match this.GraphPredicateIndex.TryGetValue {Graph = id; Subject = predicate} with
            | true, quads -> quads
                            |> Seq.map (fun e -> this.GetQuadListEntry e)
            | false, _ -> []
       
       member internal this.GetQuadsWithIdObject (id: GraphElementId, object: GraphElementId) : Quad seq =
            match this.GraphObjectIndex.TryGetValue {Graph = id; Subject = object} with
            | true, quads -> quads
                            |> Seq.map (fun e -> this.GetQuadListEntry e)
            | false, _ -> []
            

        member internal this.GetQuadsMentioningResource (resource : GraphElementId) : Quad seq =
            let resourceQuads = this.ResourceGraphMap[resource]
                                |> Seq.collect (fun g -> Seq.append
                                                             (this.GetQuadsWithIdSubject (g, resource))
                                                             (this.GetQuadsWithIdObject(g, resource) ))
            let predicateQuads = this.GetQuadsWithIdPredicate (resource, resource)
            Seq.append resourceQuads predicateQuads
        
        member internal this.GetQuadsWithIdObjectPredicate (id: GraphElementId, object: GraphElementId, predicate: GraphElementId) =
            let predicateMatch = this.GetQuadsWithIdPredicate (id, predicate)
            let objectMatch = this.GetQuadsWithIdObject (id, object)
            if  (Seq.length predicateMatch) < (Seq.length objectMatch) then
                predicateMatch 
                    |> Seq.where (fun q -> q.obj = predicate)
            else
                objectMatch
                |> Seq.where (fun q -> q.subject = object)
        
        member internal this.GetQuadsWithIdSubjectPredicate (id: GraphElementId, subject: GraphElementId, predicate: GraphElementId) : Quad seq =
            let subjectMatch = this.GetQuadsWithIdSubject (id, subject)
            let predicateMatch = this.GetQuadsWithIdObject (id, predicate)
            if  (Seq.length subjectMatch) < (Seq.length predicateMatch) then
                subjectMatch 
                    |> Seq.where (fun q -> q.obj = predicate)
            else
                predicateMatch
                |> Seq.where (fun q -> q.subject = subject)
            
        member internal this.GetQuadsWithIdSubjectObject (id: GraphElementId, subject: GraphElementId, object: GraphElementId) : Quad seq =
            let subjectMatch = this.GetQuadsWithIdSubject (id, subject)
            let objectMatch = this.GetQuadsWithIdObject (id, object)
            if  (Seq.length subjectMatch) < (Seq.length objectMatch) then
                subjectMatch 
                    |> Seq.where (fun q -> q.obj = object)
            else
                objectMatch
                |> Seq.where (fun q -> q.subject = subject)
        
        member internal this.GetQuadsWithId (graphName: GraphElementId) : Quad seq =
             this.TripleIdIndex.[graphName]
                |> Seq.map (fun e -> this.GetQuadListEntry e)
        
       member internal this.GetTriplesWithIdSubject (id: GraphElementId, subject: GraphElementId) =
            this.GetQuadsWithIdSubject (id, subject) 
                |> Seq.map _.GetTriple
        member internal this.GetTriplesWithIdPredicate (id: GraphElementId, predicate: GraphElementId) =
            this.GetQuadsWithIdPredicate (id, predicate) 
                |> Seq.map _.GetTriple

       member internal this.GetTriplesWithIdObject (id: GraphElementId, obj: GraphElementId) : Triple seq =
            this.GetQuadsWithIdObject (id, obj)
                |> Seq.map _.GetTriple

       member internal this.GetTriplesWithIdSubjectPredicate (id: GraphElementId, subject: GraphElementId, predicate: GraphElementId) : Triple seq =
            this.GetQuadsWithIdSubjectPredicate (id, subject, predicate)
                |> Seq.map (_.GetTriple)
       
       member internal this.GetTriplesWithIdSubjectObject (id: GraphElementId, subject: GraphElementId, object: GraphElementId) : Triple seq =
            this.GetQuadsWithIdSubjectObject (id, subject, object)
                |> Seq.map (_.GetTriple)
                
        member internal this.GetTriplesWithIdObjectPredicate (id: GraphElementId, object: GraphElementId, predicate: GraphElementId) : Triple seq =
            this.GetQuadsWithIdObjectPredicate (id, object, predicate)
                |> Seq.map (_.GetTriple)
                
        member internal this.GetQuads : Quad seq =
            this.QuadList
            
        member internal this.GetQuadsWithTriple (subject, predicate, object) =
            this.GetQuadsWithSubjectObject (subject, object)
            |> Seq.where (fun q -> q.predicate = predicate)