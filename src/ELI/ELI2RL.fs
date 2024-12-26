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

    let GetTypeTriplePattern (resources: ResourceManager) varName className =
        { TriplePattern.Subject = ResourceOrVariable.Variable varName
          Predicate = (ResourceOrVariable.Resource(resources.AddResource(Iri(IriReference Namespaces.RdfType))))
          Object = ResourceOrVariable.Resource resources.ResourceMap.[Iri className] }

    let GetRoleTriplePattern (resources: ResourceManager) role subjectVar objectVar =
        { TriplePattern.Subject = ResourceOrVariable.Variable subjectVar
          Predicate = (ResourceOrVariable.Resource role)
          Object = ResourceOrVariable.Variable objectVar }

    (* 
        Called from translateELI below to handle object properties.
        Assumes objProp is not inverse property.
    *)
    let GetObjPropTriplePattern (resources: ResourceManager) objProp subjectVar objectVar =
        match objProp with
        | NamedObjectProperty(FullIri objProp) ->
            GetRoleTriplePattern resources (resources.AddResource(Iri objProp)) subjectVar objectVar
        | AnonymousObjectProperty anObjProp ->
            GetRoleTriplePattern resources (resources.ResourceMap.[AnonymousBlankNode anObjProp]) subjectVar objectVar
        | InverseObjectProperty _ -> failwith "Invalid or useless construct detected: Double inverse detected."
        | ObjectPropertyChain propertyExpressions -> failwith "existential on property chain not yet supported. Sorry"

    (* Algorithm 1 from https://arxiv.org/abs/2008.02232 *)
    let rec translateELI (resources: ResourceManager) concept varName clause =
        match concept with
        | ComplexConcept.AtomicConcept(FullIri atomicClass) -> [ GetTypeTriplePattern resources varName atomicClass ]
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
            [roleTriples] @ conceptTriples
        | ComplexConcept.Top -> []

    let translateSimpleSubclassAxiom
        (resources: ResourceManager)
        (subConcept: ComplexConcept)
        (superConcept: Class)
        : Rule =
        let (FullIri superConceptIri) = superConcept

        { Head = GetTypeTriplePattern resources "X" (superConceptIri)
          Body = (translateELI resources subConcept "X" 1) |> List.map RuleAtom.PositiveTriple }

    (* The first case of Table 2 in https://arxiv.org/pdf/2008.02232:
       A_1 and ... and A_n <= bottom *) 
    // TODO: First datalog engine must handle "false" in rule head
    // let translateEmptyIntersection
    //     (resources: ResourceManager)
    //     (subConcept: ComplexConcept)
    //     (subConcepts: Class list)
    //     : Rule =
    //     { Head = False
    //       Body = subConcepts }
    
    (* The second case of Table 2 in https://arxiv.org/pdf/2008.02232:
       A_1 and ... and A_n <= A *) 
    let getAtomicNormalizedRule (resources : ResourceManager) (subConceptIntersection) (FullIri conceptName) =
        [{Head = GetTypeTriplePattern resources "X" conceptName
          Body = subConceptIntersection
                           |> List.map (fun (FullIri name) -> name)
                           |> List.map (GetTypeTriplePattern resources "X")
                           |> List.map PositiveTriple
                           }]

    (* The third case of Table 2 in https://arxiv.org/pdf/2008.02232:
       A_1(X) and ... and A_n(X) and R(X,Y) -> A(Y) *) 
    let getUniversalNormalizedRule (resources : ResourceManager) (subConceptIntersection) (objectProperty) (FullIri conceptName) =
        [{Head = GetTypeTriplePattern resources "Y" conceptName
          Body = subConceptIntersection
                           |> Seq.map (fun (FullIri name) -> name)
                           |> Seq.map (GetTypeTriplePattern resources "X")
                           |> Seq.map PositiveTriple
                           |> Seq.append [(PositiveTriple (GetObjPropTriplePattern resources objectProperty "X" "Y"))]
                           |> Seq.toList
                           }]
    let GenerateAxiomRL (resources: ResourceManager) (axiom: Formula) : DagSemTools.Datalog.Rule list =
        match axiom with
        | DirectlyTranslatableConceptInclusion(subConcepts, superConcepts) ->
            subConcepts
            |> List.map (fun subConcept ->
                superConcepts
                |> List.map (fun superConcept -> translateSimpleSubclassAxiom resources subConcept superConcept))
            |> List.concat
        | NormalizedConceptInclusion(subConceptIntersection, superConcept) ->
            match superConcept with
            | Bottom -> failwith "todo"
            | AtomicNamedConcept concept -> getAtomicNormalizedRule resources subConceptIntersection concept
            | AllValuesFrom(objectPropertyExpression, iri) -> getUniversalNormalizedRule resources subConceptIntersection objectPropertyExpression iri

    let GenerateTBoxRL (resources: ResourceManager) (axioms) =
        axioms |> Seq.map (GenerateAxiomRL resources) |> Seq.concat
