(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

(* Implementation of the translation in section 4.3 of https://www.w3.org/TR/owl2-profiles/#OWL_2_RL *)

namespace DagSemTools.OWL2RL2Datalog

open System.IO
open DagSemTools.Rdf
open DagSemTools.Datalog
open DagSemTools.Ingress
open DagSemTools.OwlOntology
open IriTools

module Library =

    let GetBasicResources (resources: DagSemTools.Rdf.GraphElementManager) =
        [ Namespaces.RdfType, Namespaces.OwlSameAs, Namespaces.OwlThing, Namespaces.OwlNothing ]
        |> List.map (fun iri ->
            (Namespaces.RdfType, resources.AddNodeResource(RdfResource.Iri(IriReference Namespaces.RdfType))))
        |> Map.ofList

    let rec getObjectPropertyExpressionResource
        (logger : Serilog.ILogger)
        (resources: GraphElementManager)
        objProp =
        match objProp with
        | NamedObjectProperty(FullIri iri) ->
                    { TriplePattern.Subject = Term.Variable "x"
                      Predicate = (Term.Resource (resources.AddNodeResource(RdfResource.Iri iri)))
                      Object = Term.Variable "y" } |> Some
        | AnonymousObjectProperty bNode ->
                    { TriplePattern.Subject = Term.Variable "x"
                      Predicate = Term.Resource (resources.AddNodeResource(AnonymousBlankNode bNode))
                      Object = Term.Variable "y" } |> Some
        | InverseObjectProperty innerObjProp ->
                    getObjectPropertyExpressionResource logger resources innerObjProp
                    |> Option.map (fun innerPattern ->
                    { TriplePattern.Subject = innerPattern.Object
                      Predicate = innerPattern.Predicate
                      Object = innerPattern.Subject } )
        | ObjectPropertyChain _ -> logger.Warning "Invalid Owl Ontology: Domain of object property chain not supported"
                                   None

    let rec getClassExpressionResource (logger : Serilog.ILogger) (resources: GraphElementManager) classExpr =
        match classExpr with
        | ClassName(FullIri iri) -> [resources.AddNodeResource(RdfResource.Iri iri)]
        | AnonymousClass bNode -> [resources.AddNodeResource(AnonymousBlankNode bNode)]
        | ObjectIntersectionOf classes -> classes |> List.collect (getClassExpressionResource logger resources)
        | ObjectUnionOf _ -> logger.Warning $"OWL 2 RL profile does not support union in domain or range expression {classExpr}"
                             [] 
        | ObjectComplementOf classExpression -> failwith "todo"
        | ObjectOneOf individuals -> failwith "todo"
        | ObjectSomeValuesFrom(objectPropertyExpression, classExpression) -> failwith "todo"
        | ObjectAllValuesFrom(objectPropertyExpression, classExpression) -> failwith "todo"
        | ObjectHasValue(objectPropertyExpression, individual) -> failwith "todo"
        | ObjectHasSelf objectPropertyExpression -> failwith "todo"
        | ObjectMinQualifiedCardinality(i, objectPropertyExpression, classExpression) -> failwith "todo"
        | ObjectMaxQualifiedCardinality(i, objectPropertyExpression, classExpression) -> failwith "todo"
        | ObjectExactQualifiedCardinality(i, objectPropertyExpression, classExpression) -> failwith "todo"
        | ObjectExactCardinality(i, objectPropertyExpression) -> failwith "todo"
        | ObjectMinCardinality(i, objectPropertyExpression) -> failwith "todo"
        | ObjectMaxCardinality(i, objectPropertyExpression) -> failwith "todo"
        | DataSomeValuesFrom(iris, dataRange) -> failwith "todo"
        | DataAllValuesFrom(iris, dataRange) -> failwith "todo"
        | DataHasValue(iri, graphElement) -> failwith "todo"
        | DataMinQualifiedCardinality(i, iri, dataRange) -> failwith "todo"
        | DataMaxQualifiedCardinality(i, iri, dataRange) -> failwith "todo"
        | DataExactQualifiedCardinality(i, iri, dataRange) -> failwith "todo"
        | DataMinCardinality(i, iri) -> failwith "todo"
        | DataMaxCardinality(i, iri) -> failwith "todo"
        | DataExactCardinality(i, iri) -> failwith "todo"
        

    let ObjectPropertyDomain2Datalog
        (logger : Serilog.ILogger)
        (resourceMap: Map<string, Ingress.GraphElementId>)
        (resources: GraphElementManager)
        objProp
        domExp
        =
        match getObjectPropertyExpressionResource logger resources objProp with 
        | None -> Seq.empty
        | Some bodyTriple ->
            getClassExpressionResource logger resources domExp
            |> Seq.map (fun domainClassResource ->
                { Rule.Head = NormalHead
                      { TriplePattern.Subject = Term.Variable "x"
                        TriplePattern.Predicate = Term.Resource resourceMap.[Namespaces.RdfType]
                        TriplePattern.Object = Term.Resource domainClassResource }
                  Rule.Body =
                      [ (RuleAtom.PositiveTriple bodyTriple ) ]
                }
            )

    let ObjectPropertyRange2Datalog
        (logger : Serilog.ILogger)
        (resourceMap: Map<string, Ingress.GraphElementId>)
        (resources: GraphElementManager)
        objProp
        rangeExp
        =
        match getObjectPropertyExpressionResource logger resources objProp with 
        | None -> Seq.empty
        | Some bodyTriple ->
        getClassExpressionResource logger resources rangeExp
        |> Seq.map (fun rangeClassResource ->
          { Rule.Head = NormalHead
              { TriplePattern.Subject = Term.Variable "y"
                TriplePattern.Predicate = Term.Resource resourceMap.[Namespaces.RdfType]
                TriplePattern.Object = Term.Resource rangeClassResource }
            Rule.Body =
              [ (RuleAtom.PositiveTriple
                    bodyTriple) ] } )
    (* prp-symp 	T(?p, rdf:type, owl:SymmetricProperty) T(?x, ?p, ?y) ->	T(?y, ?p, ?x)  *)
    let SymmetricObjectProperty2Datalog
        (logger : Serilog.ILogger)
        (resourceMap: Map<string, Ingress.GraphElementId>)
        (resources: GraphElementManager)
        objProp
        =
        match getObjectPropertyExpressionResource logger resources objProp with 
        | None -> Seq.empty
        | Some bodyTriple ->
        
        [ { Rule.Head = NormalHead
              { TriplePattern.Subject = bodyTriple.Object
                TriplePattern.Predicate = bodyTriple.Predicate
                TriplePattern.Object = bodyTriple.Subject }
            Rule.Body =
              [ (RuleAtom.PositiveTriple
                    bodyTriple) ] } ]




    (* From Table 7 in https://www.w3.org/TR/owl2-profiles/#OWL_2_RL:
        cax-eqc1 	T(?c1, owl:equivalentClass, ?c2) T(?x, rdf:type, ?c1) 	T(?x, rdf:type, ?c2)
        cax-eqc2 	T(?c1, owl:equivalentClass, ?c2) T(?x, rdf:type, ?c2) 	T(?x, rdf:type, ?c1) 
    *)
    let ObjectPropertyAxiom2Datalog
        (logger : Serilog.ILogger)
        (resourceMap: Map<string, Ingress.GraphElementId>)
        (resources: GraphElementManager)
        (axiom: ObjectPropertyAxiom)
        =
        match axiom with
        | ObjectPropertyDomain(objProp, domExp) -> ObjectPropertyDomain2Datalog logger resourceMap resources objProp domExp
        | ObjectPropertyRange(objProp, rangeExp) -> ObjectPropertyRange2Datalog logger resourceMap resources objProp rangeExp
        | SymmetricObjectProperty(_, objProp) -> SymmetricObjectProperty2Datalog logger resourceMap resources objProp
        | _ -> []

    let owlAxiom2Datalog logger
        (resourceMap: Map<string, Ingress.GraphElementId>)
        (resources: GraphElementManager)
        (axiom: Axiom)
        : Rule seq =
        match axiom with
        | AxiomObjectPropertyAxiom propertyAxiom -> ObjectPropertyAxiom2Datalog logger resourceMap resources propertyAxiom
        | AxiomClassAxiom classAxiom ->
            match DagSemTools.ELI.Library.Owl2Datalog logger resources classAxiom with
            | Some rules -> rules
            | None -> [] //TODO: failwith $"Axiom {axiom} not yet handled. Sorry"
        | _ -> [] //TODO: failwith $"Axiom {axiom} not yet handled. Sorry"

    let owl2Datalog logger resources (owlOntology: Ontology)  =
        let resourceMap = GetBasicResources resources
        owlOntology.Axioms
        |> Seq.map (owlAxiom2Datalog logger resourceMap resources)
        |> Seq.concat
