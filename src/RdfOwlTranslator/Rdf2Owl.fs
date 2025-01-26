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
open DagSemTools.Ingress.Namespaces
open IriTools
open Serilog

type Rdf2Owl (triples : TripleTable,
              resourceManager : GraphElementManager,
              logger : ILogger) =
    
    let tripleTable = triples
    let resources = resourceManager
    
    let rdfTypeId = resources.AddNodeResource(RdfResource.Iri (new IriReference(RdfType)))
    let owlOntologyId = resources.AddNodeResource(RdfResource.Iri(new IriReference(OwlOntology)))
    let versionPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlVersionIri)))
    let importsPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlImport)))
    let owlClassId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlClass)))
    let owlRestrictionId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlRestriction)))
    let owlOnPropertyId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlOnProperty)))
    let owlOnClassId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlOnClass)))
    let owlQualifiedCardinalityId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlQualifiedCardinality)))
    let owlAxiomId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlAxiom)))
    let owlMembersId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlMembers)))
    let owlAnnPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlAnnotatedProperty)))
    let owlAnnSourceId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlAnnotatedSource)))
    let owlAnnTargetId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlAnnotatedTarget)))
    let owlInvObjPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(OwlObjectInverseOf)))
    let subClassPropId = resources.AddNodeResource(RdfResource.Iri (new IriReference(RdfsSubClassOf)))
    let rdfNilId = resources.AddNodeResource(RdfResource.Iri (new IriReference(RdfNil)))
    let rdfFirstId = resources.AddNodeResource(RdfResource.Iri (new IriReference(RdfFirst)))
    let rdfRestId = resources.AddNodeResource(RdfResource.Iri (new IriReference(RdfRest)))
        
    let getResourceClass resourceId : Class option =
        resources.GetNamedResource resourceId |> Option.map Class.FullIri

    let tryGetResourceIri resourceId =
        match resources.GetNamedResource resourceId with
        | Some c -> c
        | None -> failwith $"Invalid resource {resourceId} used compulsory IRI position"

    let tryGetResourceClass resourceId =
        match getResourceClass resourceId with
        | Some c -> c
        | None -> failwith $"Invalid resource {resourceId} used on class position"

    (* Creates entities for use in declaration axioms  *)
    let createDeclaration (declarationType) resourceId  : Entity =
        let resourceIri = resources.GetNamedResource resourceId
        declarationType (Iri.FullIri resourceIri.Value)
    
    (* This is the set RIND in Table 8 of https://www.w3.org/TR/owl2-mapping-to-rdf/ *)
    let getReificationBlankNodes =
        [
            OwlAxiom
            OwlAnnotation
            OwlAllDisjointClasses
            OwlAllDisjointProperties
            OwlAllDifferent
            OwlNegativePropertyAssertion
        ]
            |> Seq.map (fun iri -> resources.AddNodeResource(RdfResource.Iri (new IriReference(iri))))
            |> Seq.collect (fun typeId -> tripleTable.GetTriplesWithObjectPredicate(typeId, rdfTypeId)
                                          |> Seq.map _.subject
                                          |> Seq.choose (fun res -> match resources.GetResource(res) with
                                                                         | Some (AnonymousBlankNode _) -> Some res
                                                                         | _ -> None))
   
    
    let extractOntologyVersionIri (ontologyIri : IriReference)  =
        let ontologyIriId = resources.AddNodeResource(RdfResource.Iri ontologyIri)
        let ontologyVersionTriples  = tripleTable.GetTriplesWithSubjectPredicate(ontologyIriId, versionPropId)
        if (ontologyVersionTriples |> Seq.length > 1) then
            logger.Warning "Multiple ontology version IRIs provided in file!"
        ontologyVersionTriples |> Seq.tryHead |> Option.bind (fun tr -> (resources.GetNamedResource tr.subject)) 
     
    let extractOntologyImports  (ontologyIri : IriReference) =
        let ontologyIriId = resources.AddNodeResource(RdfResource.Iri ontologyIri)
        let ontologyImportTriples = tripleTable.GetTriplesWithSubjectPredicate(ontologyIriId, importsPropId)
        ontologyImportTriples |> Seq.choose (fun tr -> (resources.GetNamedResource tr.obj)) 
    
    let extractOntologyIri =
        let ontologyTypeTriples = tripleTable.GetTriplesWithObjectPredicate(owlOntologyId, rdfTypeId)
        if (ontologyTypeTriples |> Seq.length > 1) then
            logger.Warning "Multiple ontology IRIs provided in file!"
        ontologyTypeTriples |> Seq.tryHead |> Option.bind (fun tr -> (resources.GetNamedResource tr.subject)) 
    
     
    let extractOntologyName  =
        match extractOntologyIri with
        | None -> (UnNamedOntology, [])
        | Some iri ->
                        let imports = extractOntologyImports iri |> Seq.toList
                        let version = match extractOntologyVersionIri  iri with
                                        | None -> NamedOntology iri
                                        | Some versionIri -> VersionedOntology (iri, versionIri)
                        (version, imports)
   
    let getEntityDeclarations (es : Entity seq) : Axiom seq =
        es |> Seq.map (fun ent -> AxiomDeclaration ([], ent))
    
    member this.extractOntology  =
        let (oName, imports) = extractOntologyName 
        let RIND = getReificationBlankNodes
        let classExpressionParser = new ClassExpressionParser(tripleTable, resources, logger)
        let axiomParser = classExpressionParser.getAxiomParser()
        let tripleAxioms = tripleTable.GetTriples() |> Seq.choose axiomParser.extractAxiom
        let axioms = [tripleAxioms  ] |> Seq.concat |> Seq.toList
        Ontology (imports, oName, [], axioms)
    
