(*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.OWL2ALC
open DagSemTools.AlcTableau
open DagSemTools.AlcTableau.ALC
open DagSemTools.OwlOntology
open OwlOntology
open ALC
open Serilog

module Translator =
    let internal translateIri (logger : ILogger) owlIri =
        match owlIri with
        | FullIri iri -> iri
    let internal translateClass (logger : ILogger) (cls : ClassExpression) : Concept =
        match cls with
        | ClassName clsName -> ConceptName (translateIri logger clsName)
        | _ -> failwith "todo"
        
    let internal translateClassAxiom (logger : ILogger) classAxiom =
        match classAxiom with
        | SubClassOf (annot, subclass, superclass) -> Inclusion (translateClass logger subclass,
                                                                 translateClass logger superclass)
        | _ -> failwith "todo"
    let internal translateAxiom (logger : ILogger) (ax : Axiom) =
        match ax with
        | AxiomClassAxiom classAxiom -> translateClassAxiom logger classAxiom |> Some
        | AxiomDeclaration decl -> logger.Warning "Declarations are not yet translated into DL"
                                   None
    let translate (logger : ILogger) (ontology: DagSemTools.OwlOntology.Ontology) : ALC.OntologyDocument =
        let tboxAxioms = ontology.Axioms
                        |> Seq.choose (translateAxiom logger)
                        |> Seq.toList
        OntologyDocument.Ontology ([], ontologyVersion.UnNamedOntology, (tboxAxioms,[]))
        
    