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
open DagSemTools.Ingress
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
        | AnonymousIndividual i -> IriReference $"https://example.com/anonymous/{i}"
    
    let rec private translateList logger translateElement binaryOperator  clsList =
        match clsList with
        | [] -> failwith "empty list should not occur. This is a bug I think"
        | [singleClass] -> translateElement logger singleClass
        | [class1; class2] -> binaryOperator (translateElement logger class1, translateElement logger class2)
        | classExpressions -> binaryOperator (translateElement logger (classExpressions |> List.head),
                                           translateList logger translateElement binaryOperator (classExpressions |> List.tail))
   
    let internal translateFacet
        (logger : ILogger)
        (prop : IriReference) =
            match prop.ToString() with
            | Namespaces.XsdMinExclusive -> facet.GreaterThan
            | Namespaces.XsdMinInclusive -> facet.GreaterThanOrEqual
            | Namespaces.XsdMaxExclusive -> facet.LessThan
            | Namespaces.XsdMaxInclusive -> facet.LessThanOrEqual
            | Namespaces.XsdLength -> facet.Length
            | Namespaces.XsdMinLength -> facet.MinLength
            | Namespaces.XsdMaxLength -> facet.MaxLength
            | Namespaces.XsdPattern -> facet.Pattern
            | Namespaces.XsdLangRange -> facet.LangRange
            | invalidRangeName -> failwith $"Invalid data range {invalidRangeName}"

    let rec private translateUnion logger clsList =
        translateList logger translateClass Disjunction clsList
    and private translateIntersection logger clsList =
        translateList logger translateClass Conjunction clsList
    and internal translateClass
        (logger : ILogger) cls =
        match cls with
        | ClassName clsName -> ConceptName (translateIri logger clsName)
        | ObjectUnionOf clsList  -> translateUnion logger clsList
        | ObjectIntersectionOf clsList  -> translateIntersection logger clsList
        | ObjectComplementOf cls -> Negation (translateClass logger cls)
        | ObjectSomeValuesFrom (role, cls) -> Existential (translateRole logger role, translateClass logger cls)  
        | ObjectAllValuesFrom (role, cls) -> Universal (translateRole logger role, translateClass logger cls)  
        | AnonymousClass i -> failwith "todo"
        | ObjectOneOf individuals -> failwith "todo"
        | ObjectHasValue(objectPropertyExpression, individual) -> failwith "todo"
        | ObjectHasSelf objectPropertyExpression -> failwith "todo"
        | ObjectMinQualifiedCardinality(i, objectPropertyExpression, classExpression) -> failwith "Qualified cardinalities are not implemented yet"
        | ObjectMaxQualifiedCardinality(i, objectPropertyExpression, classExpression) -> failwith "Qualified cardinalities are not implemented yet"
        | ObjectExactQualifiedCardinality(i, objectPropertyExpression, classExpression) -> failwith "Qualified cardinalities are not implemented yet"
        | ObjectExactCardinality(i, objectPropertyExpression) -> failwith "Number restriction are not implemented yet"
        | ObjectMinCardinality(i, objectPropertyExpression) -> failwith "Number restriction are not implemented yet"
        | ObjectMaxCardinality(i, objectPropertyExpression) -> failwith "Number restriction are not implemented yet"
        | DataSomeValuesFrom(iris, dataRange) -> failwith "Concrete domain is not implemented yet"
        | DataAllValuesFrom(iris, dataRange) -> failwith "Concrete domain is not implemented yet"
        | DataHasValue(iri, graphElement) -> failwith "Concrete domain is not implemented yet"
        | DataMinQualifiedCardinality(i, iri, dataRange) -> failwith "Number restriction are not implemented yet"
        | DataMaxQualifiedCardinality(i, iri, dataRange) -> failwith "Number restriction are not implemented yet"
        | DataExactQualifiedCardinality(i, iri, dataRange) -> failwith "Number restriction are not implemented yet"
        | DataMinCardinality(i, iri) -> failwith "Number restriction are not implemented yet"
        | DataMaxCardinality(i, iri) -> failwith "Number restriction are not implemented yet"
        | DataExactCardinality(i, iri) -> failwith "Number restriction are not implemented yet"
        
    let rec internal translateClassAxiom
        (logger : ILogger) classAxiom =
        match classAxiom with
        | SubClassOf (annot, subclass, superclass) -> [Inclusion (translateClass logger subclass,
                                                                 translateClass logger superclass)]
        | EquivalentClasses(tuples, classExpressions) -> classExpressions
                                                            |> Seq.map (translateClass logger)
                                                            |> DagSemTools.Ingress.Monad.pairs 
                                                            |> List.map (fun (cls1, cls2) -> (Inclusion (cls1, cls2)))
        | DisjointClasses(tuples, classExpressions) -> failwith "todo"
        | DisjointUnion(tuples, iri, classExpressions) -> failwith "todo"
    let rec internal translateObjectPropertyAxiom
        (logger : ILogger)
        axiom =
        match axiom with
        | ObjectPropertyDomain (prop, dom) ->
            let domainExpression = translateClass logger dom
            let role = translateRole logger prop
            Inclusion (Universal (role, Top),domainExpression)
        | ObjectPropertyRange(prop, range) ->
            let rangeExpression = translateClass logger range
            let role = translateRole logger (InverseObjectProperty prop)
            Inclusion (Universal (role, Top), rangeExpression)
        | SubObjectPropertyOf(tuples, subPropertyExpression, objectPropertyExpression) -> failwith "todo"
        | EquivalentObjectProperties(tuples, objectPropertyExpressions) -> failwith "todo"
        | DisjointObjectProperties(tuples, objectPropertyExpressions) -> failwith "todo"
        | InverseObjectProperties(tuples, objectPropertyExpression, propertyExpression) -> failwith "todo"
        | FunctionalObjectProperty(tuples, objectPropertyExpression) -> failwith "todo"
        | InverseFunctionalObjectProperty(tuples, objectPropertyExpression) -> failwith "todo"
        | ReflexiveObjectProperty(tuples, objectPropertyExpression) -> failwith "todo"
        | IrreflexiveObjectProperty(tuples, objectPropertyExpression) -> failwith "todo"
        | SymmetricObjectProperty(tuples, objectPropertyExpression) -> failwith "todo"
        | AsymmetricObjectProperty(tuples, objectPropertyExpression) -> failwith "todo"
        | TransitiveObjectProperty(tuples, objectPropertyExpression) -> failwith "todo"
    let internal translateDataPropertyAxiom
        (logger : ILogger)
        axiom =
        match axiom with
        | DataPropertyDomain (_annots, FullIri prop, domain) ->
            let domainExpression = translateClass logger domain
            Inclusion (Universal (Iri prop, Top),domainExpression)
        | SubDataPropertyOf(tuples, iri, iri1) -> failwith "todo"
        | EquivalentDataProperties(tuples, iris) -> failwith "todo"
        | DisjointDataProperties(tuples, iris) -> failwith "todo"
        | DataPropertyRange(tuples, iri, dataRange) -> failwith "todo"
        | FunctionalDataProperty(tuples, iri) -> failwith "todo"
    
    let rec internal translateDataRange
        (logger : ILogger)
        (range : DataRange) =
        match range with
        | NamedDataRange (FullIri iri) -> Datatype iri
        | DataIntersectionOf dataRanges -> translateList logger translateDataRange Intersection dataRanges
        | DataUnionOf dataRanges -> translateList logger translateDataRange Union dataRanges
        | DataComplementOf dataRange -> dataRange |> translateDataRange logger |> Complement
        | DataOneOf graphElements -> graphElements |> List.map (_.ToString()) |> OneOf
        | DatatypeRestriction(FullIri iri, tuples) ->
           Restriction (Datatype iri,
                        tuples |> List.map (fun (FullIri prop, el) ->
                                           (translateFacet logger prop, el.ToString()) )
                        )
    
    let rec internal translateAssertion (logger : ILogger) assertion =
        match assertion with
        | ObjectPropertyAssertion(annotations, objectPropertyExpression, left, right) ->
            RoleAssertion (translateIndividual logger left,
                           translateIndividual logger right,
                           translateRole logger objectPropertyExpression)
        | SameIndividual(_annots, individuals) -> failwith "todo"
        | DifferentIndividuals(_annots, individuals) -> failwith "todo"
        | ClassAssertion(_annots, classExpression, individual) ->
            ConceptAssertion (translateIndividual logger individual, translateClass logger classExpression)
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
        | AxiomObjectPropertyAxiom objectPropertyAxiom ->
            translateObjectPropertyAxiom logger objectPropertyAxiom |> TBOX |> Seq.singleton
        | AxiomDataPropertyAxiom dataPropertyAxiom ->
            translateDataPropertyAxiom logger dataPropertyAxiom |> TBOX |> Seq.singleton
        | AxiomDatatypeDefinition(_annots, (FullIri iri), dataRange) ->
            // translateDataRange logger dataRange
            failwith "Datatype definitions are not yet translated"
        | AxiomHasKey(tuples, classExpression, objectPropertyExpressions, iris) -> failwith "Owl Key Axioms are not yet translated into owl"
        | AxiomAssertion assertion -> translateAssertion logger assertion |> ABOX |> Seq.singleton
        | AxiomAnnotationAxiom annotationAxiom -> [ ] 
        
    let translateOntology (logger : ILogger) (ontology: DagSemTools.OwlOntology.Ontology)  =
     ontology.Axioms
            |> Seq.collect (translateAxiom logger)
            |> Seq.toList
            |> List.fold (fun (tboxAcc, aboxAcc) axiom ->
                match axiom with
                | TBOX x -> (x :: tboxAcc, aboxAcc)
                | ABOX x -> (tboxAcc, x :: aboxAcc))
                ([], [])

    let translateDocument (logger : ILogger) (ontology: DagSemTools.OwlOntology.OntologyDocument) : ALC.OntologyDocument =
        let (tboxAxioms, aboxAxioms) = translateOntology logger ontology.Ontology
        OntologyDocument.Ontology (ontology.Prefixes, ontology.Ontology.Version, (tboxAxioms, aboxAxioms))
        
    