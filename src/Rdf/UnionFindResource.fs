(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.    
    Contact: hovlanddag@gmail.com
*)

(* Union-Find is not currently used.
    It could be used efficiently during query evaluaton over the triple-store,
    but during reasoning it would be tricky. *)
namespace DagSemTools.Rdf

open DagSemTools.Rdf.Ingress

type UnionFind() =
    
    let parent = System.Collections.Generic.Dictionary<GraphElementId, GraphElementId>()
    let rank = System.Collections.Generic.Dictionary<GraphElementId, uint>()
    
    let rec _findRepresentative gel =
        match parent.TryGetValue gel with
        | (true, gelParent) -> if gelParent <> gel then
                                    parent.[gel] <- _findRepresentative gelParent
                                    
        | (false, _) ->
            parent.[gel] <- gel
        parent.[gel]
    member this.FindRepresentative gel =
        _findRepresentative gel
        
    member this.Union x y =
        let rootX = this.FindRepresentative(x)
        let rootY = this.FindRepresentative(y)
        if rootX <> rootY then
            let rankX = rank.[rootX]
            let rankY = rank.[rootY]
            if rankX < rankY then
                parent.[rootX] <- rootY
            elif rankX > rankY then
                parent.[rootY] <- rootX
            else
                parent.[rootY] <- rootX
                rank.[rootX] <- rankX + 1u
    
    member this.AreEquivalent(x, y) =
        this.FindRepresentative x = this.FindRepresentative y
