(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.Rdf

module Query =
  
        
    [<CustomComparison>]
    [<CustomEquality>]
    type Term = 
        | Resource of Ingress.GraphElementId
        | Variable of string
        member this.ToString (manager : GraphElementManager) : string =
            match this with
            | Resource res -> (manager.GetGraphElement res).ToString()
            | Variable vName -> $"?{vName}"
        member internal this.GetHashCodeForUnification : int =
            match this with
            | Resource res -> res.GetHashCode()
            | Variable vName -> 100
            
        interface System.IComparable with
            member this.CompareTo(obj) =
                match obj with
                | null -> 1  // null is less than any value
                | :? Term as other -> 
                    match this, other with
                    | Resource r1, Resource r2 -> compare r1 r2
                    | Variable v1, Variable v2 -> compare v1 v2
                    | Resource _, Variable _ -> -1  // Resources come before variables
                    | Variable _, Resource _ -> 1
                | _ -> 1

        override this.Equals(obj) =
            match obj with
            | null -> false
            | :? Term as other -> (this :> System.IComparable).CompareTo(other) = 0
            | _ -> false
            
        override this.GetHashCode() =
            match this with
            | Resource res -> hash res
            | Variable v -> hash v
         
    
    [<StructuralComparison>]
    [<StructuralEquality>]
    type TriplePattern =
        {Subject: Term; Predicate: Term; Object: Term}
        member private this.GeneralToString(termToStringFunction : Term -> string ) : string  =
            $"[{termToStringFunction this.Subject}, {termToStringFunction this.Predicate}, {termToStringFunction this.Object}]"
        override this.ToString() =
            this.GeneralToString(fun t -> t.ToString())
        member this.ToString(manager : GraphElementManager) =
            this.GeneralToString(fun (t : Term) ->t.ToString(manager))
        member this.GetVariables() =
            [this.Subject; this.Predicate; this.Object]
            |> Seq.choose (fun r -> match r with
                                    | Variable v -> Some (v)
                                    | _ -> None)
        
    [<StructuralComparison>]
    [<StructuralEquality>]
    type QuadPattern =
        {Triple: TriplePattern; Graph: Term}
        override this.ToString() =
            $"{this.Triple.ToString()} {this.Graph.ToString()}"
        member this.ToString(manager : GraphElementManager) =
            $"{this.Triple.ToString(manager)} {this.Graph.ToString(manager)}"
        member this.GetVariables() =
            let variables = this.Triple.GetVariables() |> List.ofSeq
            match this.Graph with
            | Variable v -> v :: variables
            | _ -> variables

    [<StructuralComparison>]
    [<StructuralEquality>]
    type GraphPattern =
        | TriplePattern of TriplePattern  
        | QuadPattern  of QuadPattern
        member this.ToString(manager : GraphElementManager) =
            match this with
            | TriplePattern tp -> tp.ToString(manager)
            | QuadPattern qp -> qp.ToString(manager)
        member this.GetVariables() =
            match this with
            | TriplePattern tp -> tp.GetVariables()
            | QuadPattern qp -> qp.GetVariables()
    
    
    [<StructuralComparison>]
    [<StructuralEquality>]
    (* The projection is the list of variable names used in the select clause, without the question mark.
       The Basic Graph Pattern is a list of Triple Patterns *)
    type SelectQuery =
        {Projection: string list; BasicGraphPattern: TriplePattern list }