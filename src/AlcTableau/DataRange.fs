(*
    Copyright (C) 2024 Dag Hovland
    
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.AlcTableau

open System
open IriTools

module DataRange =
    type Datatype = IriReference
    
    type facet =
        | GreaterThan
        | GreaterThanOrEqual
        | LessThan
        | LessThanOrEqual
        | Length
        | MinLength
        | MaxLength
        | Pattern
        | LangRange
    
    [<CustomEquality>]
    [<CustomComparison>]
    type Datarange = 
        | Datatype of Datatype
        | Union of Left: Datarange * Right: Datarange
        | Intersection of Left: Datarange * Right: Datarange
        | Complement of Datarange
        | OneOf of string list
        | Restriction of Datarange * (facet * string) list
        override this.Equals(obj) =
            match obj with
            | :? Datarange as other ->
                match this, other with
                | Datatype iri1, Datatype iri2 -> iri1 = iri2
                | Union (left1, right1), Union (left2, right2) -> left1 = left2 && right1 = right2
                | Intersection (left1, right1), Intersection (left2, right2) -> left1 = left2 && right1 = right2
                | Complement datarange1, Complement datarange2 -> datarange1 = datarange2
                | OneOf literalList1, OneOf literalList2 -> literalList1 = literalList2
                | Restriction (datatype1, facets1), Restriction (datatype2, facets2) -> datatype1 = datatype2 && facets1 = facets2
                | _ -> false
            | _ -> false
        override this.GetHashCode() =
            match this with
            | Datatype iri -> hash iri
            | Union (left, right) -> hash (left, "union", right)
            | Intersection (left, right) -> hash (left, "intersection", right)
            | Complement datarange -> hash ("copmlement", datarange)
            | OneOf literalList1 -> hash ("oneof", literalList1)
            | Restriction (datatype, facets) -> hash ("restriction", datatype, facets)

        interface IComparable with
            member this.CompareTo(obj) =
                match obj with
                | :? Datarange as other ->
                    match this, other with
                    | Datatype iri1, Datatype iri2 -> compare iri1 iri2
                    | Union (left1, right1), Union (left2, right2) -> 
                        let leftCompare = compare left1 left2
                        if leftCompare <> 0 then leftCompare else compare right1 right2
                    | Intersection (left1, right1), Intersection (left2, right2) -> 
                        let leftCompare = compare left1 left2
                        if leftCompare <> 0 then leftCompare else compare right1 right2
                    | Complement concept1, Complement concept2 -> compare concept1 concept2
                    | OneOf l, OneOf l1 -> compare l l1
                    | Restriction (datatype1, facets1), Restriction (datatype2, facets2) -> 
                        let datatypeCompare = compare datatype1 datatype2
                        if datatypeCompare <> 0 then datatypeCompare else compare facets1 facets2
                    | OneOf _l, _ -> -1
                    | _, OneOf _l -> 1
                    | Datatype _, _ -> -1
                    | _, Datatype _ -> 1
                    | Union _, _ -> -1
                    | _, Union _ -> 1
                    | Intersection _, _ -> -1
                    | _, Intersection _ -> 1
                    | Complement _, _ -> -1
                    | _, Complement _ -> 1
                    
            
                | _ -> invalidArg "obj" "Cannot compare Datarange with other types."
        