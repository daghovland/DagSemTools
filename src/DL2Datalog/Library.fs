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
open DagSemTools.Rdf.Ingress
open IriTools
open Microsoft.FSharp.Quotations

module Translator =
    
    (* X_conceptName (s) :- conceptName(s) *)
    let CreateDLConceptRule (resourceManager : ResourceManager) (conceptName : IriReference)  : Rule =
        {Rule.Head = {
                      Subject = ResourceOrVariable.Variable "s"
                      Predicate = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri (IriReference Namespaces.RdfType) )))
                      Object = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.DLTranslatedConcept conceptName )))
                      }
         Rule.Body = [
                      (RuleAtom.PositiveTriple {
                        Subject = ResourceOrVariable.Variable "s"
                        Predicate = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri (IriReference Namespaces.RdfType) )))
                        Object = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri conceptName )))
                        })
                      ]
            }
    (* X_(role some conceptName) (s) :- role some conceptName (s)  *)
    let CreateDLExistentialRule (resourceManager : ResourceManager) (role: IriReference) (conceptName : IriReference)  : Rule =
        {Rule.Head = {
                      Subject = ResourceOrVariable.Variable "s"
                      Predicate = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri (IriReference Namespaces.RdfType) )))
                      Object = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.DLTranslatedExistential (role, conceptName) )))
                      }
         Rule.Body = [
                      (RuleAtom.PositiveTriple {
                        Subject = ResourceOrVariable.Variable "s"
                        Predicate = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri (IriReference Namespaces.RdfType) )))
                        Object = (ResourceOrVariable.Resource ( resourceManager.AddResource (Resource.Iri conceptName )))
                        })
                      ]
            }
    
    
    
    (* This is an implementation of the algorithm on p. 195 of "An introduction to description logic" *)
    let Tbox2Rules (tbox : TBox) (resourceManager : ResourceManager) : Rule seq =
        tbox
        |> Seq.collect (GetAxiomConceptNames)
        |> Seq.map (CreateDLConceptRule resourceManager)
        
    
    let Ontology2Rules (resourceManager : ResourceManager) (ontologyDoc : OntologyDocument) : Rule seq =
        let (prefixes, version, (tbox, abox)) = ontologyDoc.TryGetOntology()
        Tbox2Rules tbox resourceManager 
        