(*
 Copyright (C) 2024 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.RdfOwlTranslator

open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools.Ingress
open DagSemTools.OwlOntology
open IriTools

module Ingress =
    
    let createSubClassAxiom subclass superclass = 
        ClassAxiom.SubClassOf ([], (ClassName subclass), (ClassName superclass))
    
    let createAnnotationValue (individuals : Map<ResourceId, Individual>) (resId : ResourceId) (res: Resource) =
        match individuals.TryGetValue resId with
        | true, individual -> match res with 
                                | Iri i -> AnnotationValue.IndividualAnnotation (NamedIndividual (FullIri i))
                                | AnonymousBlankNode bn -> AnnotationValue.IndividualAnnotation (Individual.AnonymousIndividual bn)
                                | _ -> failwith "Only IRIs and blank nodes are valid individuals. This is a bug"
        | false, _ -> match res with
                                | Iri i -> AnnotationValue.IriAnnotation (FullIri i)
                                | AnonymousBlankNode bn -> failwith "Annotations with blank nodes that are not individuals is not allowed"
                                | _ -> AnnotationValue.LiteralAnnotation res 
                                
    let tryGetIndividual res = 
        match res with
        | Resource.Iri iri -> NamedIndividual (FullIri iri)
        | Resource.AnonymousBlankNode bn -> AnonymousIndividual bn
        | x -> failwith $"Invalid OWL Ontology: {x} attempted used as an individual. Only IRIs and blank nodes can be individuals"
        
    
           
    let GetResourceInfoForErrorMessage (tripleTable : TripleTable) (resources : ResourceManager) (subject: ResourceId) : string =
           tripleTable.GetTriplesMentioning subject
            |> Seq.map resources.GetResourceTriple
            |> Seq.map _.ToString()
            |> String.concat ". \n"
 
    (* Extracts an RDF list. See Table 3 in https://www.w3.org/TR/owl2-mapping-to-rdf/
        Assumes head is the head of some rdf list in the triple-table
        The requirements in the specs includes non-circular lists, so blindly assumes this is true
     *)
    let rec _GetRdfListElements (tripleTable : TripleTable) (resources : ResourceManager) listId acc  =
        let rdfNilId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfNil)))
        let rdfFirstId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfFirst)))
        let rdfRestId = resources.AddResource(Resource.Iri (new IriReference(Namespaces.RdfRest)))
    
        if (listId = rdfNilId) then
            acc
        else
            let head = match tripleTable.GetTriplesWithSubjectPredicate(listId, rdfFirstId) |> Seq.toList with
                        | [] -> failwith $"Invalid list defined at {resources.GetResource(listId)}: {GetResourceInfoForErrorMessage tripleTable resources listId}"
                        | [headElement] -> headElement.obj
                        | _ -> failwith $"Invalid list defined at {resources.GetResource(listId)}: {GetResourceInfoForErrorMessage tripleTable resources listId}"
            let rest = match tripleTable.GetTriplesWithSubjectPredicate(listId, rdfRestId) |> Seq.toList with
                            | [] -> failwith $"Invalid list defined at {resources.GetResource(listId)}"
                            | [headElement] -> headElement.obj
                            | _ -> failwith $"Invalid list defined at {resources.GetResource(listId)}"
            if (Seq.contains (head, rest) acc) then
                failwith $"Invalid cyclic list defined at {resources.GetResource(listId)}: {GetResourceInfoForErrorMessage tripleTable resources listId}"
            else
                _GetRdfListElements tripleTable resources rest ((head, rest) :: acc)     
    let GetRdfListElements  (tripleTable : TripleTable) (resources : ResourceManager) listId=
        _GetRdfListElements tripleTable resources listId []
        |> List.map fst