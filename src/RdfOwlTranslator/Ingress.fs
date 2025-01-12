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
    
    let createAnnotationValue (individuals : Map<GraphElementId, Individual>) resId res =
        match individuals.TryGetValue resId with
        | true, individual -> match res with 
                                | Iri i -> AnnotationValue.IndividualAnnotation (NamedIndividual (FullIri i))
                                | AnonymousBlankNode bn -> AnnotationValue.IndividualAnnotation (Individual.AnonymousIndividual bn)
        | false, _ -> match res with
                                | Iri i -> AnnotationValue.IriAnnotation (FullIri i)
                                | AnonymousBlankNode bn -> failwith "Annotations with blank nodes that are not individuals is not allowed"
                                
    let tryGetIndividual gel =
        match gel with
        | GraphLiteral x -> failwith $"Invalid OWL Ontology: Literal {x} attempted used as an individual. Only IRIs and blank nodes can be individuals"
        | NodeOrEdge res ->
            match res with
            | Iri iri -> NamedIndividual (FullIri iri)
            | AnonymousBlankNode bn -> AnonymousIndividual bn
        
    let handleLiteralError x = failwith $"Invalid OWL Ontology: {x} attempted used as a literal. IRIs and blank nodes cannot be literals"
    let tryGetLiteral gel =
        match gel with
        | NodeOrEdge _ -> handleLiteralError gel
        | GraphLiteral res -> res
        
    let GetResourceInfoForErrorMessage (tripleTable : TripleTable) (resources : GraphElementManager) subject : string =
           tripleTable.GetTriplesMentioning subject
            |> Seq.map resources.GetResourceTriple
            |> Seq.map _.ToString()
            |> String.concat ". \n"
 
    (* Extracts an RDF list. See Table 3 in https://www.w3.org/TR/owl2-mapping-to-rdf/
        Assumes head is the head of some rdf list in the triple-table
        The requirements in the specs includes non-circular lists, so blindly assumes this is true
     *)
    let rec _GetRdfListElements (tripleTable : TripleTable) (resources : GraphElementManager) listId acc  =
        let rdfNilId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.RdfNil)))
        let rdfFirstId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.RdfFirst)))
        let rdfRestId = resources.AddNodeResource(RdfResource.Iri (new IriReference(Namespaces.RdfRest)))
    
        if (listId = rdfNilId) then
            acc
        else
            let head = match tripleTable.GetTriplesWithSubjectPredicate(listId, rdfFirstId) |> Seq.toList with
                        | [] -> failwith $"Invalid list defined at {resources.GetGraphElement(listId)}: {GetResourceInfoForErrorMessage tripleTable resources listId}"
                        | [headElement] -> headElement.obj
                        | _ -> failwith $"Invalid list defined at {resources.GetGraphElement(listId)}: {GetResourceInfoForErrorMessage tripleTable resources listId}"
            let rest = match tripleTable.GetTriplesWithSubjectPredicate(listId, rdfRestId) |> Seq.toList with
                            | [] -> failwith $"Invalid list defined at {resources.GetGraphElement(listId)}"
                            | [headElement] -> headElement.obj
                            | _ -> failwith $"Invalid list defined at {resources.GetGraphElement(listId)}"
            if (Seq.contains (head, rest) acc) then
                failwith $"Invalid cyclic list defined at {resources.GetGraphElement(listId)}: {GetResourceInfoForErrorMessage tripleTable resources listId}"
            else
                _GetRdfListElements tripleTable resources rest ((head, rest) :: acc)     
    let GetRdfListElements  (tripleTable : TripleTable) (resources : GraphElementManager) listId=
        _GetRdfListElements tripleTable resources listId []
        |> List.map fst
        
    (* This function sorts the graph element IDs corresponding to the different class expressions *)
    let sortClassExpressionIds unorderedClassExpressions =
        unorderedClassExpressions
        |> Seq.map (fun (el, preds, clExpr) -> (el, preds))
        |> Map.ofSeq
        |> DependencyGraph.TopologicalSort
    
    (* This function sorts the graph element IDs corresponding to the different class expressions *)
    let sortClassExpressions unorderedClassExpressions =
        let idExprMap = unorderedClassExpressions |> Seq.map (fun (el, _, clExpr) -> (el, clExpr)) |> Map.ofSeq
        unorderedClassExpressions
        |> sortClassExpressionIds
        |> Seq.map (fun clId -> idExprMap.[clId])
    