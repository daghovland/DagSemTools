﻿(*
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

module Reasoner =
    
    let GetBasicResources (resources : DagSemTools.Rdf.ResourceManager) =
        [Namespaces.RdfType,
         Namespaces.OwlSameAs,
         Namespaces.OwlThing,
         Namespaces.OwlNothing]
        |> List.map (fun iri -> (Namespaces.RdfType, resources.AddResource (Resource.Iri (IriReference Namespaces.RdfType))))
        |> Map.ofList 
    
    let getObjectPropertyExpressionResource (resources : ResourceManager) objProp=
        match objProp with
                 | NamedObjectProperty (FullIri iri) -> resources.AddResource (Resource.Iri iri)
                 | AnonymousObjectProperty bNode -> resources.ResourceMap.[Resource.AnonymousBlankNode bNode]
                 | InverseObjectProperty _ -> failwith "Invalid Owl Ontology: Domain of unnamed inverse object property not supported"
                 | ObjectPropertyChain _ -> failwith "Invalid Owl Ontology: Domain of object property chain not supported"
    
    let getClassExpressionResource (resources : ResourceManager) classExpr =
        match classExpr with
         | ClassName (FullIri iri) -> resources.AddResource (Resource.Iri iri)
         | AnonymousClass bNode -> resources.ResourceMap.[Resource.AnonymousBlankNode bNode]
         | _ -> failwith "Unnamed class not yet implemented for this operation. Sorry"
    let ObjectPropertyDomain2Datalog (resourceMap : Map<string, Ingress.ResourceId>) (resources : ResourceManager) objProp domExp =
        let propertyResource = getObjectPropertyExpressionResource resources objProp
        let domainClassResource = getClassExpressionResource resources domExp
        Some [
             { Rule.Head =
              {
                 TriplePattern.Subject = ResourceOrVariable.Variable "x"
                 TriplePattern.Predicate =  ResourceOrVariable.Resource resourceMap.[Namespaces.RdfType]
                 TriplePattern.Object = ResourceOrVariable.Resource domainClassResource
             };
                Rule.Body = [
              (RuleAtom.PositiveTriple {
                Subject = ResourceOrVariable.Variable "x"
                Predicate = ResourceOrVariable.Resource propertyResource
                Object = ResourceOrVariable.Variable "y"
                })
              ]
              }
        ]
        
    
    let ObjectPropertyRange2Datalog (resourceMap : Map<string, Ingress.ResourceId>) (resources : ResourceManager) objProp rangeExp =
        let propertyResource = getObjectPropertyExpressionResource resources objProp
        let rangeClassResource = getClassExpressionResource resources rangeExp
        Some [
             { Rule.Head =
              {
                 TriplePattern.Subject = ResourceOrVariable.Variable "y"
                 TriplePattern.Predicate =  ResourceOrVariable.Resource resourceMap.[Namespaces.RdfType]
                 TriplePattern.Object = ResourceOrVariable.Resource rangeClassResource
             };
                Rule.Body = [
              (RuleAtom.PositiveTriple {
                Subject = ResourceOrVariable.Variable "x"
                Predicate = ResourceOrVariable.Resource propertyResource
                Object = ResourceOrVariable.Variable "y"
                })
              ]
              }
        ]
        
    (* From Table 7 in https://www.w3.org/TR/owl2-profiles/#OWL_2_RL:
        cax-sco 	T(?c1, rdfs:subClassOf, ?c2) T(?x, rdf:type, ?c1) 	T(?x, rdf:type, ?c2) 
    *)
    let SubClass2Datalog (resourceMap : Map<string, Ingress.ResourceId>) (resources : ResourceManager) subClass superClass =
        let c1 = getClassExpressionResource resources subClass
        let c2 = getClassExpressionResource resources superClass
        Some [
             { Rule.Head =
              {
                 TriplePattern.Subject = ResourceOrVariable.Variable "x"
                 TriplePattern.Predicate =  ResourceOrVariable.Resource resourceMap.[Namespaces.RdfType]
                 TriplePattern.Object = ResourceOrVariable.Resource c2
             };
                Rule.Body = [
              (RuleAtom.PositiveTriple {
                Subject = ResourceOrVariable.Variable "x"
                Predicate = ResourceOrVariable.Resource resourceMap.[Namespaces.RdfType]
                Object = ResourceOrVariable.Resource c1
                })
              ]
              }
        ]
    let ObjectPropertyAxiom2Datalog (resourceMap : Map<string, Ingress.ResourceId>) (resources : ResourceManager) (axiom : ObjectPropertyAxiom)  =
        match axiom with
        | ObjectPropertyDomain (objProp, domExp) -> ObjectPropertyDomain2Datalog resourceMap resources objProp domExp
        | ObjectPropertyRange (objProp, rangeExp) -> ObjectPropertyRange2Datalog resourceMap resources objProp rangeExp                             
        | _ -> None
    
    let ClassAxiom2Datalog (resourceMap : Map<string, Ingress.ResourceId>) (resources : ResourceManager) (axiom : ClassAxiom)  =
        match axiom with
        | ClassAxiom.SubClassOf (_, subClass, superClass) -> SubClass2Datalog resourceMap resources subClass superClass
        | _ -> None
    let owlAxiom2Datalog (resourceMap : Map<string, Ingress.ResourceId>) (resources : ResourceManager) (axiom : Axiom)  =
        match axiom with
        | AxiomObjectPropertyAxiom propertyAxiom -> ObjectPropertyAxiom2Datalog resourceMap resources propertyAxiom
        | AxiomClassAxiom classAxiom -> ClassAxiom2Datalog resourceMap resources classAxiom
        | _ -> None
    
    
    let owl2Datalog (resources : ResourceManager) (owlOntology : Ontology) (errorOutput : TextWriter) =
        let resourceMap = GetBasicResources resources
        owlOntology.Axioms
        |> List.choose (owlAxiom2Datalog resourceMap resources)
        |> List.concat
        
        