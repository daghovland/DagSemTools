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
open Serilog

module ELI2RL =

    let internal GetTypeTriplePattern (resources: GraphElementManager) varName className =
        { TriplePattern.Subject = Term.Variable varName
          Predicate = Term.Resource(resources.AddNodeResource(Iri(IriReference Namespaces.RdfType)))
          Object = Term.Resource (resources.AddNodeResource(Iri className)) }

    
    let internal GetAnonymousTypeTriplePattern (resources: GraphElementManager) varName=
        { TriplePattern.Subject = Term.Variable varName
          Predicate = Term.Resource(resources.AddNodeResource(Iri(IriReference Namespaces.RdfType)))
          Object = Term.Resource (resources.CreateUnnamedAnonResource()) }
    
    let internal GetRoleTriplePattern (resources: GraphElementManager) role subjectVar objectVar =
        { TriplePattern.Subject = Term.Variable subjectVar
          Predicate = (Term.Resource role)
          Object = Term.Variable objectVar }

    let internal GetRoleValueTriplePattern (resources: GraphElementManager) role subjectVar (objectValue : Individual) =
        let obj = match objectValue with
                    | NamedIndividual (FullIri name) -> resources.AddNodeResource(Iri name) 
                    | AnonymousIndividual anonId -> resources.GetOrCreateNamedAnonResource($"{anonId}")
        { TriplePattern.Subject = Term.Variable subjectVar
          Predicate = (Term.Resource role)
          Object = Term.Resource obj }

    
    (* 
        Called from translateELI below to handle object properties.
        Assumes objProp is not inverse property.
    *)
    let GetNonInvObjPropTriplePattern (resources: GraphElementManager) objProp subjectVar objectVar =
        match objProp with
        | NamedObjectProperty(FullIri objProp) ->
            GetRoleTriplePattern resources (resources.AddNodeResource(Iri objProp)) subjectVar objectVar
        | AnonymousObjectProperty anObjProp ->
            GetRoleTriplePattern resources (resources.AddNodeResource(AnonymousBlankNode anObjProp)) subjectVar objectVar
        | InverseObjectProperty prop -> failwith $"Invalid or useless construct detected: Double inverse detected on {prop}."
        | ObjectPropertyChain propertyExpressions -> failwith "existential on property chain not yet supported. Sorry"

    let rec GetObjPropTriplePattern (resources: GraphElementManager) role varName newVar =
                match role with
                | InverseObjectProperty role -> GetObjPropTriplePattern resources role newVar varName
                | role -> GetNonInvObjPropTriplePattern resources role varName newVar
    
    (* Algorithm 1 from https://arxiv.org/abs/2008.02232 *)
    let rec internal translateELI (resources: GraphElementManager) concept varName clause =
        match concept with
        | ComplexConcept.AtomicConcept(FullIri atomicClass) -> [ GetTypeTriplePattern resources varName atomicClass ]
        | Intersection clauses ->
            clauses
            |> List.mapi (fun i -> fun clauseConcept -> translateELI resources clauseConcept varName i)
            |> List.concat
        | SomeValuesFrom(role, concept) ->
            let newVar = $"{varName}_{clause}"

            let roleTriples =
                 GetObjPropTriplePattern resources role varName newVar

            let conceptTriples = translateELI resources concept newVar 1
            [roleTriples] @ conceptTriples
        | ComplexConcept.Top -> []

    let internal translateSimpleSubclassAxiom
        (resources: GraphElementManager)
        (subConcept: ComplexConcept)
        (superConcept: Class)
        : Rule =
        let (FullIri superConceptIri) = superConcept

        { Head = NormalHead ( GetTypeTriplePattern resources "X" (superConceptIri))
          Body = (translateELI resources subConcept "X" 1) |> List.map RuleAtom.PositiveTriple }

    
    (* 
        Called from translateELI below to handle ObjectHasValue.
    *)
    let internal GetObjValueTriplePattern (resources: GraphElementManager) objProp subjectVar (objectIndividual : Individual) =
        match objProp with
        | NamedObjectProperty(FullIri objProp) ->
            GetRoleValueTriplePattern resources (resources.AddNodeResource(Iri objProp)) subjectVar objectIndividual
        | AnonymousObjectProperty anObjProp ->
            GetRoleValueTriplePattern resources (resources.AddNodeResource(AnonymousBlankNode anObjProp)) subjectVar objectIndividual
        | InverseObjectProperty _ -> failwith "Inverse hasValue not yet supported. Sorry."
        | ObjectPropertyChain propertyExpressions -> failwith "existential on property chain not yet supported. Sorry"
    
    (* The first case of Table 2 in https://arxiv.org/pdf/2008.02232:
       A_1 and ... and A_n <= bottom *) 
    let internal translateEmptyIntersection
        (resources: GraphElementManager)
        (subConcepts: Class list)
        : Rule =
        { Head = Contradiction
          Body = subConcepts
                           |> List.map (fun (FullIri name) -> name)
                           |> List.map (GetTypeTriplePattern resources "X")
                           |> List.map PositiveTriple
                           }
    
    (* The second case of Table 2 in https://arxiv.org/pdf/2008.02232:
       A_1 and ... and A_n <= A *) 
    let internal getAtomicNormalizedRule (resources : GraphElementManager) (subConceptIntersection) (FullIri conceptName) =
        [{Head = NormalHead ( GetTypeTriplePattern resources "X" conceptName )
          Body = subConceptIntersection
                           |> List.map (fun (FullIri name) -> name)
                           |> List.map (GetTypeTriplePattern resources "X")
                           |> List.map PositiveTriple
                           }]

    (* The anonymous class version of the second case of Table 2 in https://arxiv.org/pdf/2008.02232:
       A_1 and ... and A_n <= A *) 
    let internal getAtomicAnonymousNormalizedRule (resources : GraphElementManager) (subConceptIntersection) =
        [{Head = NormalHead ( GetAnonymousTypeTriplePattern resources "X" )
          Body = subConceptIntersection
                           |> List.map (fun (FullIri name) -> name)
                           |> List.map (GetTypeTriplePattern resources "X")
                           |> List.map PositiveTriple
                           }]

    
    (* The third case of Table 2 in https://arxiv.org/pdf/2008.02232:
       A_1(X) and ... and A_n(X) and R(X,Y) -> A(Y) *) 
    let getUniversalNormalizedRule (resources : GraphElementManager) (subConceptIntersection) (objectProperty) (FullIri conceptName) =
        [{Head = NormalHead ( GetTypeTriplePattern resources "Y" conceptName )
          Body = subConceptIntersection
                           |> Seq.map (fun (FullIri name) -> name)
                           |> Seq.map (GetTypeTriplePattern resources "X")
                           |> Seq.map PositiveTriple
                           |> Seq.append [(PositiveTriple (GetObjPropTriplePattern resources objectProperty "X" "Y"))]
                           |> Seq.toList
                           }]
    (* The fourth case of Table 2 in https://arxiv.org/pdf/2008.02232:
       A_1 and ... and A_n <=  <=1 R. A *) 
    let public getQualifiedAtMostOneNormalizedRule (resources : GraphElementManager) (subConceptIntersection) (objectProperty) (FullIri conceptName) =
        let sameAs = NamedObjectProperty (FullIri (IriReference Namespaces.OwlSameAs))
        [{Head = NormalHead ( GetObjPropTriplePattern resources sameAs "Y1" "Y2" )
          Body = subConceptIntersection
                           |> Seq.map (fun (FullIri name) -> name)
                           |> Seq.map (GetTypeTriplePattern resources "X")
                           |> Seq.map PositiveTriple
                           |> Seq.append [PositiveTriple (GetObjPropTriplePattern resources objectProperty "X" "Y1")
                                          PositiveTriple (GetObjPropTriplePattern resources objectProperty "X" "Y2")
                                          PositiveTriple (GetTypeTriplePattern resources "Y1" conceptName)
                                          PositiveTriple (GetTypeTriplePattern resources "Y2" conceptName)
                                          // TODO: This should be a rule that the datalog engine just uses to see if the resources are the same (perhaps just "sameResource"?)
                                          // The downside of not having it, is that reflexivity is added which is bad for performance
                                          // https://github.com/daghovland/DagSemTools/issues/76
                                          // NotTriple (GetObjPropTriplePattern resources sameAs "Y1" "Y2")
                                          ]
                           |> Seq.toList
                           }]
    (* The fourth case of Table 2 in https://arxiv.org/pdf/2008.02232:
       A_1 and ... and A_n <=  <=1 R *) 
    let internal getAtMostOneNormalizedRule (resources : GraphElementManager) (subConceptIntersection) (objectProperty) =
        let sameAs = NamedObjectProperty (FullIri (IriReference Namespaces.OwlSameAs))
        [{Head = NormalHead ( GetObjPropTriplePattern resources sameAs "Y1" "Y2" )
          Body = subConceptIntersection
                           |> Seq.map (fun (FullIri name) -> name)
                           |> Seq.map (GetTypeTriplePattern resources "X")
                           |> Seq.map PositiveTriple
                           |> Seq.append [PositiveTriple (GetObjPropTriplePattern resources objectProperty "X" "Y1")
                                          PositiveTriple (GetObjPropTriplePattern resources objectProperty "X" "Y2")
                                          NotTriple (GetObjPropTriplePattern resources sameAs "Y1" "Y2")]
                           |> Seq.toList
                           }]
    (*  A_1 and ... and A_n <=  ObjectHasValue(R, i) *) 
    let internal getObjectHasValueNormalizedRule (resources : GraphElementManager) (subConceptIntersection) (objectProperty : ObjectPropertyExpression) (individual : Individual) =
        let sameAs = NamedObjectProperty (FullIri (IriReference Namespaces.OwlSameAs))
        [{Head = NormalHead ( GetObjValueTriplePattern resources objectProperty "X" individual )
          Body = subConceptIntersection
                           |> Seq.map (fun (FullIri name) -> name)
                           |> Seq.map (GetTypeTriplePattern resources "X")
                           |> Seq.map PositiveTriple
                           |> Seq.toList
                           }]
    
    let GenerateAxiomRL (logger : ILogger) (resources: GraphElementManager) (axiom: Formula) : DagSemTools.Datalog.Rule list =
        match axiom with
        | DirectlyTranslatableConceptInclusion(subConcepts, superConcepts) ->
            subConcepts
            |> List.map (fun subConcept ->
                superConcepts
                |> List.map (fun superConcept -> translateSimpleSubclassAxiom resources subConcept superConcept))
            |> List.concat
        | NormalizedConceptInclusion(subConceptIntersection, superConcept) ->
            match superConcept with
            | Bottom -> 
                [translateEmptyIntersection resources subConceptIntersection]
            | AtomicNamedConcept concept ->
                getAtomicNormalizedRule resources subConceptIntersection concept
            | AllValuesFrom(objectPropertyExpression, qualifyingConcept) ->
                getUniversalNormalizedRule resources subConceptIntersection objectPropertyExpression qualifyingConcept
            | AtomicAnonymousConcept ->
                getAtomicAnonymousNormalizedRule resources subConceptIntersection
            | AtMostOneValueFromQualified(objectPropertyExpression, qualifyingConcept) ->
                getQualifiedAtMostOneNormalizedRule resources subConceptIntersection objectPropertyExpression qualifyingConcept
            | NormalizedConcept.ObjectHasValue(objectPropertyExpression, individual) ->
                getObjectHasValueNormalizedRule resources subConceptIntersection objectPropertyExpression individual
            | AtMostOneValueFrom objectPropertyExpression -> failwith "todo"

    let GenerateTBoxRL (logger: ILogger) (resources: GraphElementManager) (axioms) =
        axioms |> Seq.map (GenerateAxiomRL logger resources) |> Seq.concat
