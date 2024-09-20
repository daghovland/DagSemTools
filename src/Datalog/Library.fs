(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace AlcTableau

open AlcTableau.Rdf

module Datalog =

    [<StructuralComparison>]
    [<StructuralEquality>]
    type ResourceOrVariable = 
        | Resource of RDFStore.ResourceId
        | Variable of string

    
    [<StructuralComparison>]
    [<StructuralEquality>]
    type ResourceOrWildcard = 
        | Resource of RDFStore.ResourceId
        | Wildcard
    
    [<StructuralComparison>]
    [<StructuralEquality>]
    type TriplePattern = 
        {Subject: ResourceOrVariable; Predicate: ResourceOrVariable; Object: ResourceOrVariable}

    [<StructuralComparison>]
    [<StructuralEquality>]
    type TripleWildcard = 
        {Subject: ResourceOrWildcard; Predicate: ResourceOrWildcard; Object: ResourceOrWildcard}

    let ConstantTriplePattern (triple : RDFStore.Triple) : TriplePattern = 
        {Subject = ResourceOrVariable.Resource triple.subject; Predicate = ResourceOrVariable.Resource triple.predicate; Object = ResourceOrVariable.Resource triple.object}
    
    /// Generate all 8 possible triple patterns with wildcards for a given triple pattern
    /// Duplicate patterns are ok since these are used as a key in a dictionary
    let WildcardTriplePattern (triple : TriplePattern) : TripleWildcard list = 
        let resourceList = [triple.Subject; triple.Predicate; triple.Object]
        let rec generatePatterns (triple: ResourceOrVariable list) : ResourceOrWildcard list list = 
              match triple with
              | [] -> [[]]
              | head :: tail -> 
                let rest = generatePatterns tail
                match head with
                | Variable _ -> 
                     rest |> List.map (fun triplePart -> Wildcard :: triplePart)
                | ResourceOrVariable.Resource r -> 
                    rest |> List.collect (fun triplePart -> [Resource r :: triplePart; Wildcard :: triplePart])
        generatePatterns resourceList |> List.map (fun triplePart ->
            {Subject = List.item 0 triplePart; Predicate = List.item 1 triplePart; Object = List.item 2 triplePart})
        
    [<StructuralComparison>]
    [<StructuralEquality>]
    type Rule = 
        {Head: TriplePattern; Body: TriplePattern list}

   
    
    type DatalogProgram(Rules: Rule list) =
        let mutable Rules = Rules
        let RuleMap = new System.Collections.Generic.Dictionary<TripleWildcard, TriplePattern list>()    
        member this.AddRule(rule: Rule)  =
            Rules <- rule :: Rules
            // TODO add to dictionary
            
        

        