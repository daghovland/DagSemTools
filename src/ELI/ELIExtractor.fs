(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

(* Implementation of the translation in section 4.3 of https://www.w3.org/TR/owl2-profiles/#OWL_2_RL *)

namespace DagSemTools.ELI
open DagSemTools.ELI.Axioms
open DagSemTools.OwlOntology

module ELIExtractor =
    
    let flattenOptionList (inputList) =
        List.fold
            (fun agg el -> match (agg, el) with
                                    | (Some ls, Some e) -> Some (e :: ls)
                                    | _ -> None)
            (Some [])
            inputList
    
    let rec ELIClassExtractor classExpression  =
        match classExpression with
        | ClassName className -> ELIClass.ClassName className |> Some
        | ObjectIntersectionOf classList -> classList
                                            |> List.map ELIClassExtractor
                                            |> flattenOptionList
                                            |> Option.map ELIClass.Intersection 
        | ObjectSomeValuesFrom (role, cls) -> cls
                                              |> ELIClassExtractor
                                              |> Option.map (fun clsExpr -> ELIClass.SomeValuesFrom (role, clsExpr) )
        | ObjectMinQualifiedCardinality (1, role, cls) -> cls
                                                        |> ELIClassExtractor
                                                        |> Option.map (fun clsExpr -> ELIClass.SomeValuesFrom (role, clsExpr))
        | _ -> None
    
    let rec ELISubClassExtractor subClassExpression =
        match subClassExpression with
            ObjectUnionOf expressions -> expressions
                                         |> List.map ELISubClassExtractor
                                         |> List.concat
            | expression -> [ELIClassExtractor expression]
    
    let rec ELISuperClassExtractor superClassExpression : Class list option =
        match superClassExpression with
        ObjectIntersectionOf expressions -> expressions
                                            |> List.map ELISuperClassExtractor
                                            |> flattenOptionList
                                            |> Option.map (List.concat)
        | ClassName superClass -> Some [superClass]
        | _ -> None
    
    (*
        Separates the axioms into ELI-axioms and non-ELI-axioms
        ELI-Axioms are subclass axioms  
    *)
    let ELIAxiomxtractor (axiom : Axiom) =
        match axiom with
        | AxiomClassAxiom clAxiom ->
            match clAxiom with
            | SubClassOf (_, sub, super) ->
                match (ELISubClassExtractor sub |> flattenOptionList, ELISuperClassExtractor super) with
                | (Some subExpr, Some superExpr) -> [ELIAxiom.SubClassAxiom (subExpr, superExpr)] |> Some
                | _ -> None
            | EquivalentClasses (_, classes) ->
                classes
                |> List.map ELISuperClassExtractor
                |> flattenOptionList
                |> Option.map (List.concat)
                |> Option.map (fun classNameList -> classNameList
                                                        |> List.map (fun subClass ->
                                                                        classNameList
                                                                        |> List.map (fun superClass -> ELIAxiom.SubClassAxiom ([ELIClass.ClassName subClass], [superClass]))
                                                                    )
                                                        |> List.concat
                               )
            | _ -> None
                                            
                
        | _ -> None