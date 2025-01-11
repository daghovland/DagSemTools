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
    
   *)
  let topologicalSortKahn (graph: Map<int, list<int>>) =
    let inDegreeFolder acc _ neighbors  =
        neighbors |> List.fold (fun acc neighbor -> 
            acc |> Map.add neighbor (acc.[neighbor] + 1)) acc
        
    let mutable inDegree : Map<int, int> = 
        graph 
        |> Map.fold inDegreeFolder (graph |> Map.map (fun node _ -> 0))

    let queue  = 
        inDegree
        |> Map.filter (fun _ degree -> degree = 0)
        |> Map.keys
        |> Queue
       
    let mutable result = []

    while queue.Count > 0 do
        let node = queue.Dequeue()
        result <- node :: result

        for neighbor in graph.[node] do
            let newDegree = inDegree.[neighbor] - 1
            inDegree <- inDegree.Add(neighbor, newDegree)
            if newDegree = 0 then
                queue.Enqueue neighbor

    if List.length result = graph.Count then
        List.rev result
    else
        failwith "Graph contains a cycle"
