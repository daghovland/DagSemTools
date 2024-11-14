(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)


namespace DagSemTools.DL2Datalog

open DagSemTools.AlcTableau.ALC
open DagSemTools.Datalog
open DagSemTools.Rdf
open DagSemTools
open DagSemTools.Resource
open IriTools

(* This is an attempt at implementing the algorithm on p. 195 of "An introduction to description logic" *)
module Translator =
    
    (* X_conceptName (s) :- conceptName(s) *)
    let CreateDLConceptRule (resourceManager : ResourceManager) (conceptName : IriReference)  : Rule =
        let rdfTypeResource = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri (IriReference Namespaces.RdfType) )))
        {Rule.Head = {
                      Subject = ResourceOrVariable.Variable "s"
                      Predicate = rdfTypeResource
                      Object = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.DLTranslatedConceptName conceptName )))
                      }
         Rule.Body = [
                      (RuleAtom.PositiveTriple {
                        Subject = ResourceOrVariable.Variable "s"
                        Predicate = rdfTypeResource
                        Object = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri conceptName )))
                        })
                      ]
            }
        
    let CreateInclusionRule (resourceManager : ResourceManager) subConceptName superConceptName =
        let rdfTypeResource = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri (IriReference Namespaces.RdfType) )))
        {
         Rule.Head = {
                              Subject = ResourceOrVariable.Variable "s"
                              Predicate = rdfTypeResource
                              Object = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.DLTranslatedConceptName superConceptName )))
                              }
         Rule.Body = [
                              (RuleAtom.PositiveTriple {
                                Subject = ResourceOrVariable.Variable "s"
                                Predicate = rdfTypeResource
                                Object = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.DLTranslatedConceptName subConceptName )))
                                })
                              ]
        }   
    
    let GetConceptName (concept : Concept) =
        match concept with
        | ConceptName conceptName -> conceptName
        | _ -> failwith "Inclusion with complex concepts are not supported when translating DL to Datalog. Sorry"
    
    (* X_conceptName (s) :- X_conceptName2(s) *)
    let CreateAxiomRule (resourceManager : ResourceManager) (axiom : TBoxAxiom)  : Rule seq =
        match axiom with
        | Inclusion (subConcept, superConcept) ->
            let subConceptName = GetConceptName subConcept
            let superConceptName = GetConceptName superConcept
            [CreateInclusionRule resourceManager subConceptName superConceptName ]
        | Equivalence (subConcept, superConcept) ->
            let subConceptName = GetConceptName subConcept
            let superConceptName = GetConceptName superConcept
            [
             CreateInclusionRule resourceManager subConceptName superConceptName
             CreateInclusionRule resourceManager superConceptName subConceptName
            ]
    
    
    (* X_(role some conceptName) (s) :- role some conceptName (s)  *)
    let CreateDLExistentialRule (resourceManager : ResourceManager) (role: Role) (concept : Concept)  : Rule =
        let rdfTypeResource = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri (IriReference Namespaces.RdfType) )))
        let conceptName = match concept with
                          | ConceptName conceptName -> conceptName
                          | _ -> failwith "Existential constraints with complex concepts are not supported. Sorry"
        match role with
            | Role.Iri roleIri ->
                {Rule.Head = {
                              Subject = ResourceOrVariable.Variable "s"
                              Predicate = rdfTypeResource
                              Object = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.DLTranslatedExistential (roleIri, conceptName) )))
                              }
                 Rule.Body = [
                              (RuleAtom.PositiveTriple {
                                Subject = ResourceOrVariable.Variable "t"
                                Predicate = rdfTypeResource
                                Object = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri conceptName)))
                                })
                              (RuleAtom.PositiveTriple {
                                Subject = ResourceOrVariable.Variable "s"
                                Predicate = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri roleIri )))
                                Object = ResourceOrVariable.Variable "t"
                                })
                              ]
                }
            | Role.Inverse roleIri ->
                {Rule.Head = {
                              Subject = ResourceOrVariable.Variable "s"
                              Predicate = rdfTypeResource
                              Object = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.DLTranslatedExistential (roleIri, conceptName) )))
                              }
                 Rule.Body = [
                              (RuleAtom.PositiveTriple {
                                Subject = ResourceOrVariable.Variable "t"
                                Predicate = rdfTypeResource
                                Object = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri conceptName )))
                                })
                              (RuleAtom.PositiveTriple {
                                Subject = ResourceOrVariable.Variable "t"
                                Predicate = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri roleIri )))
                                Object = ResourceOrVariable.Variable "s"
                                })
                              ]
                }
                
    let Tbox2Rules (tbox : TBox) (resourceManager : ResourceManager) : Rule seq =
        let conceptNameAxioms = tbox
                                |> Seq.collect (GetAxiomConceptNames)
                                |> Seq.map (CreateDLConceptRule resourceManager)
        let existentialAxioms = tbox
                                |> Seq.collect (GetAxiomExistentials)
                                |> Seq.map (fun (role, concept) -> CreateDLExistentialRule resourceManager role concept)
        let inclusionAxioms = tbox
                                |> Seq.collect (CreateAxiomRule resourceManager)
        Seq.append inclusionAxioms (Seq.append conceptNameAxioms existentialAxioms)
        
    
    let Ontology2Rules (resourceManager : ResourceManager) (ontologyDoc : OntologyDocument) : Rule seq =
        let (prefixes, version, (tbox, abox)) = ontologyDoc.TryGetOntology()
        Tbox2Rules tbox resourceManager 
        