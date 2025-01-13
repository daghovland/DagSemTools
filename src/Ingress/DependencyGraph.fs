(*
 Copyright (C) 2024 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Ingress

(* Helper function(s) for dependency graphs and sorting.
    Used currently by DagSemTools.RdfOwlTranslator, but might be relevant also other places  *)
module DependencyGraph =
  open System.Collections.Generic
  
  (* 
    This is an attempt at Kahn's algorithm for topological sorting.
    Sources: 
    * https://en.wikipedia.org/wiki/Topological_sorting
    * The Art of Computer Programming, Vol. 1, Algorithm T in Chapter "Links and Lists"
    
    The input is a map of already integer-encoded elements as keys which maps to the list of successors of that element
    The output is the reverse sorted list. (Since some applications need the reverse order)
   *)
  let ReverseTopologicalSort graph =
    
    (* This function is called during preprocessing and calculates the number of predecessors *)
    let NumPredecessorCalculator acc _ successors  =
        successors |> List.fold (fun acc successor -> 
            acc |> Map.add successor (acc.[successor] + 1u)) acc
        
    let mutable GetNumPredecessors  = 
        graph 
        |> Map.fold NumPredecessorCalculator (graph |> Map.map (fun node _ -> 0u))

    (* This queue is filled with elements that do not have predecessors,
        or where the predecessors have also been handled *)
    let ReadyElementQueue  = 
        GetNumPredecessors
        |> Map.filter (fun _ numPredecessors -> numPredecessors = 0u)
        |> Map.keys
        |> Queue
       
    let mutable sortedList = []

    while ReadyElementQueue.Count > 0 do
        let node = ReadyElementQueue.Dequeue()
        sortedList <- node :: sortedList

        for successor in graph.[node] do
            let remainingPredecessors = GetNumPredecessors.[successor] - 1u
            GetNumPredecessors <- GetNumPredecessors.Add(successor, remainingPredecessors)
            if remainingPredecessors = 0u then
                ReadyElementQueue.Enqueue successor

    if List.length sortedList = graph.Count then
        List.rev sortedList
    else
        failwith "Graph contains a cycle"

  (* The input is a map of already integer-encoded elements as keys which maps to the list of successors of that element
    The output is the reverse sorted list. *)
  let TopologicalSort graph =
      graph
      |> ReverseTopologicalSort
      |> List.rev