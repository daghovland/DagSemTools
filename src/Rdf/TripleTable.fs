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

type TripleTable(tripleList: Triple array,
                 tripleCount: TripleListIndex,
                 threeKeysIndex: Dictionary<Triple, TripleListIndex>,
                 predicateIndex: Dictionary<GraphElementId, TripleListIndex list>,
                 subjectPredicateIndex: Dictionary<GraphElementId, Dictionary<GraphElementId, TripleListIndex list>>,
                 objectPredicateIndex: Dictionary<GraphElementId, Dictionary<GraphElementId, TripleListIndex list>>) =
        
    let mutable TripleList = tripleList
    member val TripleCount = tripleCount with get, set
    
    member this.GetTriples() : Triple seq =
        seq {
            let mutable index = 0u
            while index < this.TripleCount do
                yield this.GetTripleListEntry index
                index <- index + 1u
        }
            
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
                    new Dictionary<GraphElementId, TripleListIndex list>(),
                    new Dictionary<GraphElementId, Dictionary<GraphElementId, TripleListIndex list>>(),
                    new Dictionary<GraphElementId, Dictionary<GraphElementId, TripleListIndex list>>()
                    )
        
        
    member this.doubleTripleListSize () =
        TripleList <- doubleArraySize TripleList
    member this.GetTripleListEntry (index: TripleListIndex) : Triple =
        TripleList.[int index]
    
    member this.AddPredicateIndex (predicate: GraphElementId, tripleIndex: TripleListIndex) =
        if this.PredicateIndex.ContainsKey predicate then
            let existList = this.PredicateIndex.[predicate]
            this.PredicateIndex.[predicate] <- tripleIndex :: existList
        else
            this.PredicateIndex.Add(predicate, [tripleIndex]) |> ignore
            
    member this.AddSubjectPredicateIndex (subject: GraphElementId, predicate: GraphElementId, tripleIndex: TripleListIndex) =
        let existSubjectMap = match  (this.SubjectPredicateIndex.TryGetValue subject) with 
                                |    true, subjMap -> subjMap
                                |    false, _ -> new Dictionary<GraphElementId, TripleListIndex list>()
        let existSubjectPredicateList = match (existSubjectMap.TryGetValue predicate) with
                                        | true, subjPredList -> subjPredList
                                        | false,_ -> []
        existSubjectMap.[predicate] <- tripleIndex :: existSubjectPredicateList
        this.SubjectPredicateIndex.[subject] <- existSubjectMap
        
    member this.AddObjectPredicateIndex (obj: GraphElementId, predicate: GraphElementId, tripleIndex: TripleListIndex) =
        let existObjectMap = match  (this.ObjectPredicateIndex.TryGetValue obj) with 
                                |    true, objMap -> objMap
                                |    false, _ -> new Dictionary<GraphElementId, TripleListIndex list>()
        let existSubjectPredicateList = match (existObjectMap.TryGetValue predicate) with
                                        | true, objPredList -> objPredList
                                        | false, _ -> []
        existObjectMap.[predicate] <- tripleIndex :: existSubjectPredicateList
        this.ObjectPredicateIndex.[obj] <- existObjectMap
            
    member this.AddTriple (triple : Ingress.Triple) =
            if this.ThreeKeysIndex.ContainsKey triple then
                ()
            else
                let nextTripleCount = this.TripleCount + 1u
                if nextTripleCount > uint32(TripleList.Length) then
                        this.doubleTripleListSize()   
                this.AddSubjectPredicateIndex(triple.subject, triple.predicate, this.TripleCount)
                this.AddObjectPredicateIndex(triple.obj, triple.predicate, this.TripleCount)
                this.AddPredicateIndex(triple.predicate, this.TripleCount) 
                TripleList.[int(this.TripleCount)] <- triple
                this.ThreeKeysIndex.Add(triple, this.TripleCount) |> ignore
                this.TripleCount <- nextTripleCount
                ()
            
        member this.Contains (triple : Triple) : bool =
            this.ThreeKeysIndex.ContainsKey triple
        member this.GetTriplesWithSubject (subject: GraphElementId) : Triple seq =
            match  (this.SubjectPredicateIndex.TryGetValue subject) with 
                                |    true, subjMap -> subjMap |> Seq.collect (fun x -> x.Value) |> Seq.map this.GetTripleListEntry
                                |    false, _ -> []
            
        member this.GetTriplesWithObject (obj: GraphElementId) : Triple seq =
            match (this.ObjectPredicateIndex.TryGetValue obj) with
                                |    true, objectIndex -> objectIndex |> Seq.collect (fun x -> x.Value) |> Seq.map (fun e -> this.GetTripleListEntry e)
                                |    false, _ -> []
            
        member this.GetTriplesWithPredicate (predicate: GraphElementId) : Triple seq =
            match (this.PredicateIndex.TryGetValue predicate) with
                | true, predMap -> predMap |> Seq.map (fun e -> this.GetTripleListEntry e)
                | false, _ -> []
            
        member this.GetPredicates() : GraphElementId seq =
            this.PredicateIndex.Keys 
        member this.GetTriplesWithSubjectPredicate (subject: GraphElementId, predicate: GraphElementId) =
            match  (this.SubjectPredicateIndex.TryGetValue subject) with 
                                |    true, subjMap -> match subjMap.TryGetValue predicate with
                                                        |    true, subjPredList -> subjPredList |> Seq.map (fun e -> this.GetTripleListEntry e)
                                                        |    false, _ -> []
                                |    false, _ -> []
            
            
        member this.GetTriplesWithObjectPredicate (obj: GraphElementId, predicate: GraphElementId) =
            match  (this.ObjectPredicateIndex.TryGetValue obj) with 
                                |    true, objMap -> match objMap.TryGetValue predicate with
                                                        |    true, objPredList -> objPredList |> Seq.map (fun e -> this.GetTripleListEntry e)
                                                        |    false, _ -> []
                                |    false, _ -> []
            
        member this.GetTriplesWithSubjectObject (subject: GraphElementId, object: GraphElementId) : Triple seq =
            this.GetTriplesWithSubject subject
                |> Seq.where (fun triple ->  triple.obj = object)
        
       
       member this.GetTriplesMentioning resource =
           Seq.concat [this.GetTriplesWithSubject(resource)
                       this.GetTriplesWithPredicate(resource)
                       this.GetTriplesWithObject(resource)]
           