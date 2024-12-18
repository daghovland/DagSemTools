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
    
    let GetTypeTriplePattern (resources : ResourceManager) className varName =
        { TriplePattern.Subject = ResourceOrVariable.Resource resources.ResourceMap.[Iri className]
          Predicate = (ResourceOrVariable.Resource  (resources.AddResource(Iri (IriReference Namespaces.RdfType))));
          Object = ResourceOrVariable.Variable varName }
    
    let GetRoleTriplePattern (resources : ResourceManager) role subjectVar objectVar =
        { TriplePattern.Subject = ResourceOrVariable.Variable subjectVar
          Predicate = (ResourceOrVariable.Resource  (resources.AddResource(Iri role)));
          Object = ResourceOrVariable.Variable objectVar }
    
    (* Algorithm 1 from https://arxiv.org/abs/2008.02232 *)
    let rec translateELI (resources : ResourceManager) concept varName clause =
        match concept with
        | ELIClass.ClassName (FullIri atomicClass) -> [GetTypeTriplePattern resources atomicClass varName]
        | Intersection clauses -> clauses
                                |> List.mapi (fun i -> fun clauseConcept -> translateELI resources clauseConcept varName i)
                                |> List.concat
        | SomeValuesFrom (role, concept) ->
            let newVar = $"{varName}_{clause}"
            let roleTriples = match role with
                                | InverseObjectProperty (NamedObjectProperty (FullIri objProp)) -> [GetRoleTriplePattern resources objProp newVar varName]
                                | NamedObjectProperty (FullIri objProp) -> [GetRoleTriplePattern resources objProp varName newVar]
            let conceptTriples = translateELI resources concept newVar 1
            roleTriples @ conceptTriples
        
    let GenerateRL (axiom : ELIAxiom) : DagSemTools.Datalog.Rule list  =
                match axiom with
                SubClassAxiom (subConcept, superConcept) -> {Head = GetTypeTriplePattern resources}
                []

