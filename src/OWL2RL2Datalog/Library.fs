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

    let GetBasicResources (resources: DagSemTools.Rdf.ResourceManager) =
        [ Namespaces.RdfType, Namespaces.OwlSameAs, Namespaces.OwlThing, Namespaces.OwlNothing ]
        |> List.map (fun iri ->
            (Namespaces.RdfType, resources.AddResource(Resource.Iri(IriReference Namespaces.RdfType))))
        |> Map.ofList

    let getObjectPropertyExpressionResource (resources: ResourceManager) objProp =
        match objProp with
        | NamedObjectProperty(FullIri iri) -> resources.AddResource(Resource.Iri iri)
        | AnonymousObjectProperty bNode -> resources.ResourceMap.[Resource.AnonymousBlankNode bNode]
        | InverseObjectProperty _ ->
            failwith "Invalid Owl Ontology: Domain of unnamed inverse object property not supported"
        | ObjectPropertyChain _ -> failwith "Invalid Owl Ontology: Domain of object property chain not supported"

    let getClassExpressionResource (resources: ResourceManager) classExpr =
        match classExpr with
        | ClassName(FullIri iri) -> resources.AddResource(Resource.Iri iri)
        | AnonymousClass bNode -> resources.ResourceMap.[Resource.AnonymousBlankNode bNode]
        | _ -> failwith "Unnamed class not yet implemented for this operation. Sorry"

    let ObjectPropertyDomain2Datalog
        (resourceMap: Map<string, Ingress.ResourceId>)
        (resources: ResourceManager)
        objProp
        domExp
        =
        let propertyResource = getObjectPropertyExpressionResource resources objProp
        let domainClassResource = getClassExpressionResource resources domExp

        [ { Rule.Head =
              { TriplePattern.Subject = ResourceOrVariable.Variable "x"
                TriplePattern.Predicate = ResourceOrVariable.Resource resourceMap.[Namespaces.RdfType]
                TriplePattern.Object = ResourceOrVariable.Resource domainClassResource }
            Rule.Body =
              [ (RuleAtom.PositiveTriple
                    { Subject = ResourceOrVariable.Variable "x"
                      Predicate = ResourceOrVariable.Resource propertyResource
                      Object = ResourceOrVariable.Variable "y" }) ] } ]



    let ObjectPropertyRange2Datalog
        (resourceMap: Map<string, Ingress.ResourceId>)
        (resources: ResourceManager)
        objProp
        rangeExp
        =
        let propertyResource = getObjectPropertyExpressionResource resources objProp
        let rangeClassResource = getClassExpressionResource resources rangeExp

        [ { Rule.Head =
              { TriplePattern.Subject = ResourceOrVariable.Variable "y"
                TriplePattern.Predicate = ResourceOrVariable.Resource resourceMap.[Namespaces.RdfType]
                TriplePattern.Object = ResourceOrVariable.Resource rangeClassResource }
            Rule.Body =
              [ (RuleAtom.PositiveTriple
                    { Subject = ResourceOrVariable.Variable "x"
                      Predicate = ResourceOrVariable.Resource propertyResource
                      Object = ResourceOrVariable.Variable "y" }) ] } ]
    (* prp-symp 	T(?p, rdf:type, owl:SymmetricProperty) T(?x, ?p, ?y) ->	T(?y, ?p, ?x)  *)
    let SymmetricObjectProperty2Datalog
        (resourceMap: Map<string, Ingress.ResourceId>)
        (resources: ResourceManager)
        objProp
        =
        let propertyResource = getObjectPropertyExpressionResource resources objProp

        [ { Rule.Head =
              { TriplePattern.Subject = ResourceOrVariable.Variable "y"
                TriplePattern.Predicate = ResourceOrVariable.Resource propertyResource
                TriplePattern.Object = ResourceOrVariable.Variable "x" }
            Rule.Body =
              [ (RuleAtom.PositiveTriple
                    { Subject = ResourceOrVariable.Variable "x"
                      Predicate = ResourceOrVariable.Resource propertyResource
                      Object = ResourceOrVariable.Variable "y" }) ] } ]




    (* From Table 7 in https://www.w3.org/TR/owl2-profiles/#OWL_2_RL:
        cax-eqc1 	T(?c1, owl:equivalentClass, ?c2) T(?x, rdf:type, ?c1) 	T(?x, rdf:type, ?c2)
        cax-eqc2 	T(?c1, owl:equivalentClass, ?c2) T(?x, rdf:type, ?c2) 	T(?x, rdf:type, ?c1) 
    *)
    let ObjectPropertyAxiom2Datalog
        (resourceMap: Map<string, Ingress.ResourceId>)
        (resources: ResourceManager)
        (axiom: ObjectPropertyAxiom)
        =
        match axiom with
        | ObjectPropertyDomain(objProp, domExp) -> ObjectPropertyDomain2Datalog resourceMap resources objProp domExp
        | ObjectPropertyRange(objProp, rangeExp) -> ObjectPropertyRange2Datalog resourceMap resources objProp rangeExp
        | SymmetricObjectProperty(_, objProp) -> SymmetricObjectProperty2Datalog resourceMap resources objProp
        | _ -> []

    let owlAxiom2Datalog
        (resourceMap: Map<string, Ingress.ResourceId>)
        (resources: ResourceManager)
        (axiom: Axiom)
        : Rule seq =
        match axiom with
        | AxiomObjectPropertyAxiom propertyAxiom -> ObjectPropertyAxiom2Datalog resourceMap resources propertyAxiom
        | AxiomClassAxiom classAxiom ->
            match DagSemTools.ELI.Library.Owl2Datalog resources classAxiom with
            | Some rules -> rules
            | None -> [] //TODO: failwith $"Axiom {axiom} not yet handled. Sorry"
        | _ -> [] //TODO: failwith $"Axiom {axiom} not yet handled. Sorry"

    let owl2Datalog (resources: ResourceManager) (owlOntology: Ontology) (errorOutput: TextWriter) =
        let resourceMap = GetBasicResources resources
        owlOntology.Axioms
        |> Seq.map (owlAxiom2Datalog resourceMap resources)
        |> Seq.concat
