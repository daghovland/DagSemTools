(*
    Copyright (C) 2024 Dag Hovland
    
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    
    Contact: hovlanddag@gmail.com
*)
open System

open System
open AlcTableau
open Tableau
open ALC
open IriTools
open System.IO


let filename = Environment.GetCommandLineArgs().[1]
let ontologyText = System.IO.File.ReadAllText(filename)

let parsedOntology = ManchesterAntlr.Parser.ParseFile(filename, Console.Error)

match parsedOntology with
    | Ontology (prefixes, version, kb) ->
        let state = ReasonerService.init kb
        let reasoner_result = ReasonerService.is_consistent state
        printfn $"reasoner_result = {reasoner_result}"
    

exit 100