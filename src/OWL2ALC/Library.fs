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
open DagSemTools.AlcTableau.DataRange
open DagSemTools.OwlOntology
open IriTools
open OwlOntology
open ALC
open Serilog

module Translator =
    let internal translateIri (logger : ILogger) owlIri =
        match owlIri with
        | FullIri iri -> iri
    let rec internal translateRole (logger : ILogger) (role : ObjectPropertyExpression) : Role =
        match role with
        | NamedObjectProperty (FullIri iri) -> Role.Iri iri
        | AnonymousObjectProperty i -> failwith "todo"
        | InverseObjectProperty objectPropertyExpression -> match translateRole logger objectPropertyExpression with
                                                            | Role.Iri iri -> Role.Inverse iri
                                                            | Role.Inverse iri -> Role.Iri iri
        | ObjectPropertyChain objectPropertyExpressions -> failwith "todo"
    let internal translateIndividual (logger : ILogger) (ind : Individual)  : IriReference =
        match ind with
        | NamedIndividual (FullIri iri) -> iri
        | AnonymousIndividual i -> failwith "todo"
    
    let rec private translateList logger translateElement binaryOperator  clsList =
        match clsList with
        | [] -> failwith "empty list should not occur. This is a bug I think"
        | [singleClass] -> translateElement logger singleClass
        | [class1; class2] -> binaryOperator (translateElement logger class1, translateElement logger class2)
        | classExpressions -> binaryOperator (translateElement logger (classExpressions |> List.head),
                                           translateList logger translateElement binaryOperator (classExpressions |> List.tail))
   
    let rec private translateUnion logger clsList =
        translateList logger translateClass Disjunction clsList
    and private translateIntersection logger clsList =
        translateList logger translateClass Conjunction clsList
    and internal translateClass (logger : ILogger) cls =
        match cls with
        | ClassName clsName -> ConceptName (translateIri logger clsName)
        | ObjectUnionOf clsList  -> translateUnion logger clsList
        | ObjectIntersectionOf clsList  -> translateIntersection logger clsList
        | ObjectComplementOf cls -> Negation (translateClass logger cls)
        | ObjectSomeValuesFrom (role, cls) -> Existential (translateRole logger role, translateClass logger cls)  
        | ObjectAllValuesFrom (role, cls) -> Universal (translateRole logger role, translateClass logger cls)  
        | _ -> failwith "todo"
        
    let rec internal translateClassAxiom (logger : ILogger) classAxiom =
        match classAxiom with
        | SubClassOf (annot, subclass, superclass) -> [Inclusion (translateClass logger subclass,
                                                                 translateClass logger superclass)]
        | EquivalentClasses(tuples, classExpressions) -> classExpressions
                                                            |> Seq.map (translateClass logger)
                                                            |> DagSemTools.Ingress.Monad.pairs 
                                                            |> List.map (fun (cls1, cls2) -> (Equivalence (cls1, cls2)))
        | DisjointClasses(tuples, classExpressions) -> failwith "todo"
        | DisjointUnion(tuples, iri, classExpressions) -> failwith "todo"
        
    let rec internal translateAssertion (logger : ILogger) assertion =
        match assertion with
        | ObjectPropertyAssertion(annotations, objectPropertyExpression, left, right) ->
            RoleAssertion (translateIndividual logger left,
                           translateIndividual logger right,
                           translateRole logger objectPropertyExpression)
        | SameIndividual(_annots, individuals) -> failwith "todo"
        | DifferentIndividuals(_annots, individuals) -> failwith "todo"
        | ClassAssertion(_annots, classExpression, individual) -> failwith "todo"
        | NegativeObjectPropertyAssertion(_annots, objectPropertyExpression, left, right) ->
            NegativeRoleAssertion  (translateIndividual logger left,
                                   translateIndividual logger right,
                                   translateRole logger objectPropertyExpression)
        | DataPropertyAssertion(_annots, FullIri dprop, individual, graphElement) ->
          LiteralAssertion (translateIndividual logger individual, dprop, graphElement.ToString())
        | NegativeDataPropertyAssertion(_annots, FullIri dprop, individual, graphElement) ->
            NegativeAssertion (LiteralAssertion (translateIndividual logger individual, dprop, graphElement.ToString()))
        
    type DLAxiom =
        TBOX of TBoxAxiom
        | ABOX of ABoxAssertion
    let internal translateAxiom (logger : ILogger) (ax : Axiom) =
        match ax with
        | AxiomClassAxiom classAxiom -> translateClassAxiom logger classAxiom |> Seq.map TBOX 
        | AxiomDeclaration decl -> logger.Warning "Declarations are not yet translated into DL"
                                   []
        | AxiomObjectPropertyAxiom objectPropertyAxiom -> failwith "todo"
        | AxiomDataPropertyAxiom dataPropertyAxiom -> failwith "todo"
        | AxiomDatatypeDefinition(tuples, iri, dataRange) -> failwith "todo"
        | AxiomHasKey(tuples, classExpression, objectPropertyExpressions, iris) -> failwith "todo"
        | AxiomAssertion assertion -> translateAssertion logger assertion |> ABOX |> Seq.singleton
        | AxiomAnnotationAxiom annotationAxiom -> failwith "todo"
    let translate (logger : ILogger) (ontology: DagSemTools.OwlOntology.Ontology) : ALC.OntologyDocument =
        let (tboxAxioms, aboxAxioms) = ontology.Axioms
                                        |> Seq.collect (translateAxiom logger)
                                        |> Seq.toList
                                        |> List.fold (fun (tboxAcc, aboxAcc) axiom ->
                                            match axiom with
                                            | TBOX x -> (x :: tboxAcc, aboxAcc)
                                            | ABOX x -> (tboxAcc, x :: aboxAcc))
                                            ([], [])

        OntologyDocument.Ontology ([], ontologyVersion.UnNamedOntology, (tboxAxioms, aboxAxioms))
        
    