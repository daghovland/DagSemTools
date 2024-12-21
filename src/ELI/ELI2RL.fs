(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

(* Translation from ELI to RL, inspired by https://arxiv.org/abs/2008.02232 *)

namespace DagSemTools.ELI

open DagSemTools.Datalog
open DagSemTools.ELI.Axioms
open DagSemTools.Ingress
open DagSemTools.OwlOntology
open DagSemTools.Rdf
open IriTools

module ELI2RL =

    let GetTypeTriplePattern (resources: ResourceManager) className varName =
        { TriplePattern.Subject = ResourceOrVariable.Variable varName
          Predicate = (ResourceOrVariable.Resource(resources.AddResource(Iri(IriReference Namespaces.RdfType))))
          Object = ResourceOrVariable.Resource resources.ResourceMap.[Iri className] }

    let GetRoleTriplePattern (resources: ResourceManager) role subjectVar objectVar =
        { TriplePattern.Subject = ResourceOrVariable.Variable subjectVar
          Predicate = (ResourceOrVariable.Resource role)
          Object = ResourceOrVariable.Variable objectVar }

    (* Called from translateELI below to handle object properties.
        Assumes objProp is not inverse property.
    *)
    let GetObjPropTriplePattern (resources: ResourceManager) objProp subjectVar objectVar =
        match objProp with
        | NamedObjectProperty(FullIri objProp) ->
            [ GetRoleTriplePattern resources (resources.AddResource(Iri objProp)) subjectVar objectVar ]
        | AnonymousObjectProperty anObjProp ->
            [ GetRoleTriplePattern resources (resources.ResourceMap.[AnonymousBlankNode anObjProp]) subjectVar objectVar ]
        | InverseObjectProperty _ -> failwith "Invalid or useless construct detected: Double inverse detected."
        | ObjectPropertyChain propertyExpressions -> failwith "existential on property chain not yet supported. Sorry"

    (* Algorithm 1 from https://arxiv.org/abs/2008.02232 *)
    let rec translateELI (resources: ResourceManager) concept varName clause =
        match concept with
        | ComplexConcept.AtomicConcept(FullIri atomicClass) -> [ GetTypeTriplePattern resources atomicClass varName ]
        | Intersection clauses ->
            clauses
            |> List.mapi (fun i -> fun clauseConcept -> translateELI resources clauseConcept varName i)
            |> List.concat
        | SomeValuesFrom(role, concept) ->
            let newVar = $"{varName}_{clause}"
let roleTriples =
    match role with
    | InverseObjectProperty role -> GetObjPropTriplePattern resources role newVar varName
    | role -> GetObjPropTriplePattern resources role varName newVar
            let conceptTriples = translateELI resources concept newVar 1
            roleTriples @ conceptTriples
        | Top -> []

    let translateSimpleSubclassAxiom
        (resources: ResourceManager)
        (subConcept: ComplexConcept)
        (superConcept: Class)
        : Rule =
        let (FullIri superConceptIri) = superConcept

        { Head = GetTypeTriplePattern resources (superConceptIri) "X"
          Body = (translateELI resources subConcept "X" 1) |> List.map RuleAtom.PositiveTriple }

    let GenerateAxiomRL (resources: ResourceManager) (axiom: Formula) : DagSemTools.Datalog.Rule list =
        match axiom with
        | ConceptInclusion(subConcepts, superConcepts) ->
            subConcepts
            |> List.map (fun subConcept ->
                superConcepts
                |> List.map (fun superConcept -> translateSimpleSubclassAxiom resources subConcept superConcept))
            |> List.concat

    let GenerateTBoxRL (resources: ResourceManager) (axioms) =
        axioms |> Seq.map (GenerateAxiomRL resources) |> Seq.concat
