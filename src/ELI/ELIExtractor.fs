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
        | ObjectMinQualifiedCardinality(cardinality, role, cls) when cardinality = 1I ->
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
   
   
    (* Helper function for the case exist R . C in subconcept positions in the function below
       In  https://www.ijcai.org/Proceedings/09/Papers/336.pdf, this is the last case before section 4.2
     *)
    let subConceptSomeValuesFrom  objectPropertyExpression classExpression concept =
        ([], [classExpression],
                 [NormalizedConceptInclusion ([classExpression |> conceptRepresentative],
                                              AllValuesFrom (InverseObjectProperty objectPropertyExpression, concept |> conceptRepresentative))])
   
    (* This is a combination of the function st(C) and the normalization in Section 4.2 of https://arxiv.org/pdf/2008.02232 *)
    (* Originally called Structural Transformation (sec 2.3) in https://www.ijcai.org/Proceedings/09/Papers/336.pdf *)
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
                        | ObjectMaxQualifiedCardinality(cardinality, _objectPropertyExpression, classExpression) when cardinality = 0I ->
                            ([], [classExpression], [NormalizedConcept.Bottom])
                        | ObjectMaxQualifiedCardinality(cardinality, objectPropertyExpression, classExpression) when cardinality = 1I ->
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
                        | ObjectMaxCardinality(cardinality, objectPropertyExpression) when cardinality = 0I ->
                            ([], [], [NormalizedConcept.Bottom])
                        | ObjectMaxCardinality(cardinality, objectPropertyExpression) when cardinality = 1I ->
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
                subConceptSomeValuesFrom objectPropertyExpression classExpression concept
            | ObjectMinQualifiedCardinality(cardinality, objectPropertyExpression, classExpression) when cardinality = 1I ->
                subConceptSomeValuesFrom objectPropertyExpression classExpression concept
            | ObjectMinQualifiedCardinality(_card, _objProp, _clExpr) ->
                logger.Warning "Invalid OWL 2 RL: ObjectMinQualifiedCardinality only allowed with cardinality 1"
                ([], [], [])
            | ObjectExactQualifiedCardinality(cardinality, objectPropertyExpression, classExpression) when cardinality = 1I ->
                logger.Warning "Invalid OWL 2 RL: ObjectExactQualifiedCardinality not allowed. Only treating as ObjectMinQualifiedCardinality"
                subConceptSomeValuesFrom objectPropertyExpression classExpression concept
            | ObjectExactQualifiedCardinality(_card, _objProp, _clExpr) ->
                logger.Warning "Invalid OWL 2 RL: ObjectExactQualifiedCardinality not allowed in OWL 2 RL"
                ([], [], [])
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
                                        logger.Warning "ObjectAllValuesFrom is not allowed on the subconcept part of an inclusion in OWL 2 RL"
                                        ([],[],[])
                                    | ObjectHasSelf objectPropertyExpression -> failwith "todo"
                                    | ObjectMaxQualifiedCardinality(i, objectPropertyExpression, classExpression) ->
                                        logger.Warning "ObjectMaxQualifiedCardinality is not allowed on the subconcept part of an inclusion in OWL 2 RL"
                                        ([],[],[])
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
                                    | ObjectExactQualifiedCardinality _ -> failwith "this is a bug, shold have been matched above"
                                    | ObjectMinQualifiedCardinality _ -> failwith "this is a bug, shold have been matched above"
                         (positiveOccurrences, negativeOccurrences,  (subConcepts |> List.map (fun subConcept ->
                            Formula.NormalizedConceptInclusion (subConcept, NormalizedConcept.AtomicNamedConcept (conceptRepresentative concept)))))
        (positiveOccurrences |> Seq.map (conceptPositiveOccurenceNormalization logger) |> List.concat) @
        (negativeOccurrences |> Seq.map (conceptNegativeOccurenceNormalization logger) |> List.concat) @
        inclusionFormulas
        
    
    (* This is an implementation of the normalization procedure in https://www.ijcai.org/Proceedings/09/Papers/336.pdf
       I also used Section 4.2 of https://arxiv.org/pdf/2008.02232 *)
    let SubClassAxiomNormalization (logger: ILogger) (axiom: ClassAxiom) =
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
