(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

(* Implementation of the translation in section 4.3 of https://www.w3.org/TR/owl2-profiles/#OWL_2_RL *)

namespace DagSemTools.ELI

open DagSemTools.AlcTableau.ALC
open DagSemTools.ELI.Axioms
open DagSemTools.OwlOntology
open DagSemTools.Ingress
open DagSemTools.Rdf
open IriTools
open Serilog

module ELIExtractor =
    let rec ELIClassExtractor classExpression =
        match classExpression with
        | ClassName className -> ComplexConcept.AtomicConcept className |> Some
        | ObjectIntersectionOf classList ->
            classList
            |> List.map ELIClassExtractor
            |> Monad.flattenOptionList
            |> Option.map ComplexConcept.Intersection
        | ObjectSomeValuesFrom(role, cls) ->
            cls
            |> ELIClassExtractor
            |> Option.map (fun clsExpr -> ComplexConcept.SomeValuesFrom(role, clsExpr))
        | ObjectMinQualifiedCardinality(1, role, cls) ->
            cls
            |> ELIClassExtractor
            |> Option.map (fun clsExpr -> ComplexConcept.SomeValuesFrom(role, clsExpr))
        | _ -> None

    let rec ELISubClassExtractor subClassExpression =
        match subClassExpression with
        | ObjectUnionOf expressions -> expressions |> List.map ELISubClassExtractor |> List.concat
        | expression -> [ ELIClassExtractor expression ]

    let rec ELISuperClassExtractor superClassExpression : Class list option =
        match superClassExpression with
        | ObjectIntersectionOf expressions ->
            expressions
            |> List.map ELISuperClassExtractor
            |> Monad.flattenOptionList
            |> Option.map (List.concat)
        | ClassName superClass -> Some [ superClass ]
        | _ -> None

    
    (* This are helpers for the  implementation of the normalization procedure in Section 4.2 of https://arxiv.org/pdf/2008.02232 *)
    
    (* This is the function A_C in Section 4.2 of https://arxiv.org/pdf/2008.02232 *) 
    let conceptRepresentative (concept : ClassExpression) =
        $"https://github.org/daghovland/DagSemTools/ConceptRepresentative/{concept.GetHashCode()}"
        |> IriReference
        |> FullIri
    (* This is a combination of the function st(C) and the normalization in Section 4.2 of https://arxiv.org/pdf/2008.02232 *)
    let rec conceptPositiveOccurenceNormalization (logger : ILogger) (concept : ClassExpression) =
        let (positiveOccurrences, negativeOccurrences, inclusionFormulas) =
            match concept with
            | ObjectComplementOf classExpression ->
                ([], [classExpression], [Formula.NormalizedConceptInclusion
                                             ([(conceptRepresentative concept)
                                               conceptRepresentative classExpression],
                                               NormalizedConcept.Bottom) ])
            | ObjectIntersectionOf classExpressions ->
                (classExpressions,
                 [],
                 classExpressions
                 |> List.map (fun classExpression ->
                     Formula.NormalizedConceptInclusion ([conceptRepresentative concept],
                                                         classExpression|> conceptRepresentative |> AtomicNamedConcept))  )
            | ObjectHasValue(objectPropertyExpression, individual) ->
                ([], [],
                 [Formula.NormalizedConceptInclusion ([conceptRepresentative concept],
                                                      NormalizedConcept.ObjectHasValue(objectPropertyExpression, individual))])

            | _ ->
                    let (positiveOccurrences, negativeOccurrences, superConcepts) =
                        match concept with 
                        | ClassName conceptName ->
                            ([], [], [NormalizedConcept.AtomicNamedConcept conceptName ])
                        | AnonymousClass i ->
                            ([], [], [NormalizedConcept.AtomicAnonymousConcept])
                        | ObjectUnionOf classExpressions ->
                            logger.Warning "Invalid OWL 2 RL: Union in the superclass position not allowed"
                            ([],[],[])
                        | ObjectOneOf individuals -> failwith "TODO"
                        | ObjectSomeValuesFrom(objectPropertyExpression, classExpression) ->
                            logger.Warning "Invalid OWL 2 RL: Existential in the superclass position not allowed"
                            ([],[],[])
                        | ObjectAllValuesFrom(objectPropertyExpression, classExpression) ->
                            ([], [classExpression], [AllValuesFrom (objectPropertyExpression,  classExpression |> conceptRepresentative)])
                        | ObjectHasSelf objectPropertyExpression -> failwith "todo"
                        | ObjectMinQualifiedCardinality(i, objectPropertyExpression, classExpression) ->
                            logger.Warning("Invalid OWL 2 RL: ObjectMinQualifiedCardinality not allowed on superConcept")
                            ([], [], [])
                        | ObjectMaxQualifiedCardinality(0, _objectPropertyExpression, classExpression) ->
                            ([], [classExpression], [NormalizedConcept.Bottom])
                        | ObjectMaxQualifiedCardinality(1, objectPropertyExpression, classExpression) ->
                            ([], [classExpression], [NormalizedConcept.AtMostOneValueFromQualified(objectPropertyExpression, classExpression |> conceptRepresentative)])
                        | ObjectMaxQualifiedCardinality(i, _objectPropertyExpression, _classExpression) ->
                            logger.Warning("Invalid OWL 2 RL: ObjectMaxQualifiedCardinality on superConcept only allowed with cardinality 0 or 1")
                            ([], [], [])
                        | ObjectExactQualifiedCardinality(i, objectPropertyExpression, classExpression) ->
                            logger.Warning("Invalid OWL 2 RL: ObjectExactQualifiedCardinality not allowed on superConcept")
                            ([], [], [])
                        | ObjectExactCardinality(i, objectPropertyExpression) ->
                            logger.Warning("Invalid OWL 2 RL: ObjectExactCardinality not allowed on superConcept")
                            ([], [], [])
                        | ObjectMinCardinality(i, objectPropertyExpression) -> 
                            logger.Warning("Invalid OWL 2 RL: ObjectMinCardinality not allowed on superConcept")
                            ([], [], [])
                        | ObjectMaxCardinality(0, objectPropertyExpression) ->
                            ([], [], [NormalizedConcept.Bottom])
                        | ObjectMaxCardinality(1, objectPropertyExpression) ->
                            ([], [], [NormalizedConcept.AtMostOneValueFrom(objectPropertyExpression)])
                        | ObjectMaxCardinality(i, _objectPropertyExpression) ->
                            logger.Warning("Invalid OWL 2 RL: ObjectMaxCardinality on superConcept only allowed with cardinality 0 or 1")
                            ([], [], [])
                        | DataSomeValuesFrom(iris, dataRange) ->
                            logger.Warning "Invalid OWL 2 RL: DataSomeValuesFrom not allowed in the superClassExpression"
                            ([],[],[])
                        | DataAllValuesFrom(iris, dataRange) -> failwith "todo"
                        | DataHasValue(iri, resource) -> failwith "todo"
                        | DataMinQualifiedCardinality(i, iri, dataRange) -> failwith "todo"
                        | DataMaxQualifiedCardinality(i, iri, dataRange) -> failwith "todo"
                        | DataExactQualifiedCardinality(i, iri, dataRange) -> failwith "todo"
                        | DataMinCardinality(i, iri) -> failwith "todo"
                        | DataMaxCardinality(i, iri) -> failwith "todo"
                        | DataExactCardinality(i, iri) -> failwith "todo"
                        | ObjectComplementOf _ -> failwith "This is a bug, should have been matched above"
                        | ObjectIntersectionOf _ -> failwith "This is a bug, should have been matched above"
                        | ObjectHasValue(objectPropertyExpression, individual) -> failwith "This is a bug, should have been matched above"
                    (positiveOccurrences, negativeOccurrences,  superConcepts |> List.map (fun superConcept ->
                        Formula.NormalizedConceptInclusion ([conceptRepresentative concept], superConcept)))
        
        (positiveOccurrences |> Seq.map (conceptPositiveOccurenceNormalization logger) |> List.concat) @
        (negativeOccurrences |> Seq.map (conceptNegativeOccurenceNormalization logger) |> List.concat) @
        inclusionFormulas
         
    and conceptNegativeOccurenceNormalization (logger : ILogger) (concept : ClassExpression) =
        let mainConceptRepresentative = concept |> conceptRepresentative |> AtomicNamedConcept
        let (positiveOccurrences, negativeOccurrences, inclusionFormulas) =
            match concept with
            | ObjectUnionOf classExpressions ->
                ([], classExpressions, classExpressions |> List.map (fun classExpression ->
                     NormalizedConceptInclusion ([classExpression |> conceptRepresentative], mainConceptRepresentative)))
            | ObjectSomeValuesFrom(objectPropertyExpression, classExpression) ->
                ([], [classExpression],
                 [NormalizedConceptInclusion ([concept |> conceptRepresentative],
                                              AllValuesFrom (InverseObjectProperty objectPropertyExpression, classExpression |> conceptRepresentative))])
            | ObjectMinQualifiedCardinality(1, objectPropertyExpression, classExpression) ->
                ([], [classExpression],
                 [NormalizedConceptInclusion ([concept |> conceptRepresentative],
                                              AllValuesFrom (InverseObjectProperty objectPropertyExpression, classExpression |> conceptRepresentative))])
            | ObjectComplementOf classExpression ->
                ([classExpression], [], [Formula.NormalizedConceptInclusion
                                             ([(conceptRepresentative concept)
                                               conceptRepresentative classExpression],
                                               NormalizedConcept.Bottom) ])
            | ObjectHasValue(objectPropertyExpression, individual) ->
                logger.Error "objectHasValue is not yet implemented. sorry"
                ([],[],[])
                                    
            | concept -> let (positiveOccurrences, negativeOccurrences, subConcepts) =
                                match concept with
                                    | ClassName conceptName ->
                                        ([], [], [ [conceptName] ])
                                    | AnonymousClass i ->
                                        ([], [], [ [conceptRepresentative concept] ])
                                    | ObjectIntersectionOf classExpressions ->
                                        ([], classExpressions, [classExpressions |> List.map conceptRepresentative])
                                    | ObjectOneOf individuals -> failwith "TODO"
                                    | ObjectAllValuesFrom(objectPropertyExpression, classExpression) ->
                                        failwith "ObjectAllValuesFrom is not allowed on the subconcept part of an inclusion in OWL 2 RL"
                                    | ObjectHasSelf objectPropertyExpression -> failwith "todo"
                                    | ObjectMinQualifiedCardinality(i, objectPropertyExpression, classExpression) ->
                                        failwith "todo"
                                    | ObjectMaxQualifiedCardinality(i, objectPropertyExpression, classExpression) -> failwith "todo"
                                    | ObjectExactQualifiedCardinality(i, objectPropertyExpression, classExpression) -> failwith "todo"
                                    | ObjectExactCardinality(i, objectPropertyExpression) -> failwith "todo"
                                    | ObjectMinCardinality(i, objectPropertyExpression) -> failwith "todo"
                                    | ObjectMaxCardinality(i, objectPropertyExpression) -> failwith "todo"
                                    | DataSomeValuesFrom(iris, dataRange) -> failwith "todo"
                                    | DataAllValuesFrom(iris, dataRange) -> failwith "todo"
                                    | DataHasValue(iri, resource) -> failwith "todo"
                                    | DataMinQualifiedCardinality(i, iri, dataRange) -> failwith "todo"
                                    | DataMaxQualifiedCardinality(i, iri, dataRange) -> failwith "todo"
                                    | DataExactQualifiedCardinality(i, iri, dataRange) -> failwith "todo"
                                    | DataMinCardinality(i, iri) -> failwith "todo"
                                    | DataMaxCardinality(i, iri) -> failwith "todo"
                                    | DataExactCardinality(i, iri) -> failwith "todo"
                                    | ObjectUnionOf _ -> failwith "this is a bug, shold have been matched above"
                                    | ObjectSomeValuesFrom _ -> failwith "this is a bug, shold have been matched above"
                                    | ObjectComplementOf _ -> failwith "this is a bug, shold have been matched above"
                                    | ObjectHasValue _ -> failwith "this is a bug, shold have been matched above"
                         (positiveOccurrences, negativeOccurrences,  (subConcepts |> List.map (fun subConcept ->
                            Formula.NormalizedConceptInclusion (subConcept, NormalizedConcept.AtomicNamedConcept (conceptRepresentative concept)))))
        (positiveOccurrences |> Seq.map (conceptPositiveOccurenceNormalization logger) |> List.concat) @
        (negativeOccurrences |> Seq.map (conceptNegativeOccurenceNormalization logger) |> List.concat) @
        inclusionFormulas
        
    
    (* This is an implementation of the normalization procedure in https://www.ijcai.org/Proceedings/09/Papers/336.pdf
       I also used Section 4.2 of https://arxiv.org/pdf/2008.02232 *)
    let internal SubClassAxiomNormalization (logger: ILogger) (axiom: ClassAxiom) =
        match axiom with
        | SubClassOf (_annot, subClassExpression, superClassExpression) ->
            [Formula.NormalizedConceptInclusion (
                                                 [(conceptRepresentative subClassExpression)],
                                                 (superClassExpression |> conceptRepresentative |> AtomicNamedConcept))]
            @ conceptPositiveOccurenceNormalization logger superClassExpression
            @ conceptNegativeOccurenceNormalization logger subClassExpression
        | DisjointClasses _ -> failwith "todo"
        | EquivalentClasses(tuples, classExpressions) -> failwith "todo"
        | DisjointUnion(tuples, iri, classExpressions) -> 
            logger.Warning $"Invalid OWL 2 RL Ontology: DisjointUnion is not allowed. {classExpressions |> List.map _.ToString()}"
            []
            
            
    (*
        Separates the axioms into ELI-axioms and non-ELI-axioms
        ELI-Axioms are subclass axioms  
    *)
    let rec ELIAxiomExtractor (logger: ILogger) (clAxiom: ClassAxiom) =
            match clAxiom with
            | SubClassOf(_, sub, super) ->
                match (ELISubClassExtractor sub |> Monad.flattenOptionList, ELISuperClassExtractor super) with
                | (Some subExpr, Some superExpr) ->
                    [ Formula.DirectlyTranslatableConceptInclusion(subExpr, superExpr) ] |> Some
                | _ -> SubClassAxiomNormalization logger clAxiom |> Some 
            | EquivalentClasses(_, classes) ->
                DagSemTools.Ingress.Monad.pairs classes
                |> List.map (fun (subConcept, superConcept) -> ELIAxiomExtractor logger (SubClassOf ([], subConcept, superConcept)))
                |> Monad.flattenOptionList
                |> Option.map List.concat
            | _ -> None
