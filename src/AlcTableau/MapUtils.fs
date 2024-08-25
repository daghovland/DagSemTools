(*
    Copyright (C) 2024 Dag Hovland
    
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    
    Contact: hovlanddag@gmail.com
*)

module AlcTableau.MapUtils

open IriTools
open System.Collections.Generic

let addToList l v =
       if l |> List.contains v
       then l else l @ [v]
let mergeLists l1 l2 = l2 |> List.fold addToList l1
let mergeMaps (map1: Map<IriReference, 'T list>) (map2: Map<IriReference, 'T list>) =
    map2 |> Map.fold (fun acc key value ->
        let combinedValue = 
            match Map.tryFind key acc with
            | Some existingValue -> value |> List.fold addToList existingValue 
            | None -> value
        acc.Add(key, combinedValue)
    ) map1

let mergeThreeMaps (map1: Map<IriReference, 'T list>) (map2: Map<IriReference, 'T list>) (map3: Map<IriReference, 'T list>) =
    map1 |> mergeMaps map2 |> mergeMaps map3
    
let mergeMapList map (maps : Map<IriReference, 'T list> list) =
    maps |> List.fold (fun acc map -> mergeMaps acc map) map

/// Adds the list value to the list at key in the map
let addToMapList (map: Map<'U, 'T list>) key value =
    let orig_values = map.GetValueOrDefault(key, [])
    map.Add(key, mergeLists orig_values value)    

/// Takes as input a map from K to list of V and returns a map form V to list of K
let invert_assertions_map (map : Map<'K, 'V list>) =
    map |> Map.toSeq
    |> Seq.fold (fun concept_map (iri, concepts) -> concepts |> List.fold
                                                                (fun acc concept -> addToMapList acc concept [iri])
                                                                concept_map
                ) Map.empty
