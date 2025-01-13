(*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

module Tests


open System
open Xunit
open DagSemTools.Ingress.DependencyGraph
open Faqt

[<Fact>]
let ``Empty graph should return an empty list`` () =
    let graph = Map.empty<int, list<int>>
    let result = TopologicalSort graph
    result.Should().BeEmpty()

[<Fact>]
let ``Single-node graph should return the single node`` () =
    let graph = Map.ofList [ 1, [] ]
    let result = TopologicalSort graph
    result.Should().Be([1])

[<Fact>]
let ``Simple acyclic graph should return correct topological order`` () =
    let graph = Map.ofList [
        1, [2; 3]
        2, [4]
        3, [4]
        4, []
    ]
    let result = TopologicalSort graph
    result.Should().HaveLength(4).And.BeSupersetOf([1;2;3;4]) |> ignore
    result.Head.Should().Be(4)
    
[<Fact>]
let ``Complex acyclic graph should return correct topological order`` () =
    let graph = Map.ofList [
        5, [11;7]
        7, [11; 8]
        3, [8; 10]
        11, [2; 9; 10]
        8, [9]
        2, [8;3]
        9, [10]
        10, []
    ]
    let result = TopologicalSort graph
    result.Should().Be([10;9;8;3;2;11;7;5])

[<Fact>]
let ``Graph with no edges should return nodes in any order`` () =
    let graph = Map.ofList [
        1, []
        2, []
        3, []
    ]
    let result = TopologicalSort graph
    Assert.True(Set.ofList result = Set.ofList [1; 2; 3])

[<Fact>]
let ``Graph with a cycle should raise an exception`` () =
    let graph = Map.ofList [
        1, [2]
        2, [3]
        3, [1]
    ]
    let ex = Assert.Throws<Exception>(fun () -> TopologicalSort graph |> ignore)
    Assert.Contains("cycle", ex.Message.ToLower())