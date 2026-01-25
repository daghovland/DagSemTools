(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.Rdf

open DagSemTools.Rdf.Ingress
open DagSemTools.Rdf.Query
open Ingress
open System
open DagSemTools.Ingress
open IriTools

type Datastore(reifiedTriples: QuadTable,
               namedGraphs: QuadTable,
               resources: GraphElementManager) =
    member val ReifiedTriples = reifiedTriples with get, set
    member val NamedGraphs = namedGraphs with get, set
    member val Resources = resources with get, set
    
    new(init_rdf_size : uint) =
        let init_resources : uint = uint ( max 10 (int init_rdf_size / 10) )
        let init_triples = uint ( max 10 (int init_rdf_size / 60) )
        Datastore(new QuadTable(init_triples),
                  new QuadTable(init_triples),
                  new GraphElementManager(init_resources))
        
    
    new(elementManager : GraphElementManager, init_triples : uint) =
        Datastore(new QuadTable(init_triples),
                  new QuadTable(init_triples),
                  elementManager)
    member this.AddTriple (triple: Triple) =
        this.NamedGraphs.AddQuad (getDefaultGraphTriple triple)
    
    member this.AddQuad =
        this.NamedGraphs.AddQuad 
    
    
    member this.GetDefaultTripleTable = new NamedTripleTable(this.NamedGraphs, defaultGraphElementId)
    
    member this.AddNamedGraphTriple(graph: GraphElementId, triple: Triple) =
        this.NamedGraphs.AddQuad{ tripleId = graph; subject = triple.subject; predicate = triple.predicate; obj = triple.obj}
    member this.AddReifiedTriple (triple: Triple, id: GraphElementId) =
        this.ReifiedTriples.AddQuad { tripleId = id; subject = triple.subject; predicate = triple.predicate; obj = triple.obj }
    
        
    member this.AddResource (resource: GraphElement) : GraphElementId =
        this.Resources.AddResource resource
    member this.AddLiteralResource (resource: RdfLiteral) : GraphElementId =
        this.Resources.AddLiteralResource resource
    member this.AddNodeResource (resource: RdfResource) : GraphElementId =
        this.Resources.AddNodeResource resource
    

        

        
    member this.GetGraphElementId (resource : GraphElement) =
        this.Resources.GraphElementMap.[resource]
    member this.GetRdfLiteralId (resource : RdfLiteral) =
        this.Resources.GraphElementMap.[GraphElement.GraphLiteral resource]
    member this.GetGraphNodeId (resource : RdfResource) =
        this.Resources.GraphElementMap.[GraphElement.NodeOrEdge resource]
    member this.NewAnonymousBlankNode() =
        this.Resources.CreateUnnamedAnonResource()
    member this.GetResourceTriple (triple: Triple) =
        this.Resources.GetResourceTriple triple
    member this.GetResourceQuad quad =
        this.Resources.GetResourceQuad quad
    member this.GetNamedGraph (graphId : GraphElementId) : ITripleTable
        = NamedTripleTable(this.NamedGraphs, graphId)
    member this.GetTriplesWithSubject (subject: GraphElementId) : Triple seq =
        this.NamedGraphs.GetTriplesWithIdSubject (defaultGraphElementId, subject)
    member this.GetTriplesWithSubject (graphid: GraphElementId, subject: GraphElementId)  =
        this.NamedGraphs.GetTriplesWithIdSubject (graphid, subject)
    
    member this.GetTriplesWithObject (object: GraphElementId) : Triple seq =
        this.NamedGraphs.GetTriplesWithIdObject (defaultGraphElementId, object)
    member this.GetTriplesWithObject (graphid: GraphElementId, object: GraphElementId)  =
        this.NamedGraphs.GetTriplesWithIdObject (graphid, object)
    member this.GetTriplesWithPredicate (predicate: GraphElementId) : Triple seq =
        this.NamedGraphs.GetTriplesWithIdPredicate (defaultGraphElementId, predicate)
    member this.GetTriplesWithPredicate (graphid: GraphElementId, predicate: GraphElementId)  =
        this.NamedGraphs.GetTriplesWithIdPredicate (graphid, predicate)
    
    member this.GetTriplesWithSubjectPredicate (subject: GraphElementId, predicate: GraphElementId) =
        this.NamedGraphs.GetTriplesWithIdSubjectPredicate(defaultGraphElementId, subject, predicate)
        
    member this.GetTriplesWithSubjectPredicate (graphId : GraphElementId, subject: GraphElementId, predicate: GraphElementId) =
        this.NamedGraphs.GetTriplesWithIdSubjectPredicate(graphId, subject, predicate)
    member this.GetTriplesWithObjectPredicate (object: GraphElementId, predicate: GraphElementId) =
        this.NamedGraphs.GetTriplesWithIdObjectPredicate (defaultGraphElementId, object, predicate)
    member this.GetTriplesWithObjectPredicate (graphId : GraphElementId, object: GraphElementId, predicate: GraphElementId) =
        this.NamedGraphs.GetTriplesWithIdObjectPredicate(graphId, object, predicate)
    
    member this.GetTriplesWithSubjectObject (subject: GraphElementId, object: GraphElementId)  =
        this.NamedGraphs.GetTriplesWithIdSubjectObject (defaultGraphElementId, subject, object)
    member this.GetTriplesWithSubjectObject (graphId: GraphElementId, subject: GraphElementId, object: GraphElementId)  =
        this.NamedGraphs.GetTriplesWithIdSubjectPredicate (graphId, subject, object)
        
    member this.ContainsTriple (triple : Triple) : bool  =
            this.NamedGraphs.Contains { Quad.tripleId = defaultGraphElementId
                                        Quad.subject = triple.subject
                                        predicate = triple.predicate
                                        obj = triple.obj }
    member this.ContainsQuad quad =
        this.NamedGraphs.Contains quad
    member this.GetReifiedTriplesWithId(id: GraphElementId) : Triple seq =
        this.ReifiedTriples.GetQuadsWithId (id)
        |> Seq.map _.GetTriple
    member this.GetReifiedTriplesWithSubject(subject: GraphElementId) : Quad seq =
        this.ReifiedTriples.GetQuadsWithSubject subject
    member this.GetReifiedTriplesWithPredicate(predicate: GraphElementId) : Quad seq =
        this.ReifiedTriples.GetQuadsWithPredicate predicate
    member this.GetReifiedTriplesWithObject(obj: GraphElementId) : Quad seq =
        this.ReifiedTriples.GetQuadsWithObject obj
    member this.GetReifiedTriplesWithSubjectPredicate(subject: GraphElementId, predicate: GraphElementId) : Quad seq =
        this.ReifiedTriples.GetQuadsWithSubjectPredicate (subject, predicate)

    member this.GetResourceInfoForErrorMessage(subject: GraphElementId) : string =
           this.NamedGraphs.GetQuadsMentioningResource subject
            |> Seq.map this.Resources.GetResourceQuad
            |> Seq.map _.ToString()
            |> String.concat ". "
            
    member this.GetQuads(pat: Query.QuadPattern) : Quad seq =
        let g = pat.Graph       
        let s = pat.Subject
        let p = pat.Predicate
        let o = pat.Object
        match g, s, p, o with
        | Resource gRes, Resource sRes, Resource pRes, Resource oRes ->
            let q = { Quad.tripleId = gRes; subject =  sRes; predicate = pRes; obj = oRes }
            if this.NamedGraphs.Contains q then
                seq { yield q }
            else
                Seq.empty
        | Resource gRes,  Resource sRes, Resource pRes, Variable _ ->
            this.NamedGraphs.GetQuadsWithIdSubjectPredicate (gRes, sRes, pRes)
        | Resource gRes, Resource sRes, Variable _, Resource oRes ->
            this.NamedGraphs.GetQuadsWithIdSubjectObject (gRes, sRes, oRes)
        | Resource gRes, Variable _, Resource pRes, Resource oRes ->
            this.NamedGraphs.GetQuadsWithIdObjectPredicate (gRes, oRes, pRes)
        | Resource gRes,  Resource sRes, Variable _, Variable _ ->
            this.NamedGraphs.GetQuadsWithIdSubject (gRes, sRes)
        | Resource gRes, Variable _, Resource pRes, Variable _ ->
            this.NamedGraphs.GetQuadsWithIdPredicate (gRes, pRes)
        | Resource gRes, Variable _, Variable _, Resource oRes ->
            this.NamedGraphs.GetQuadsWithIdObject (gRes, oRes)
        | Resource gRes, Variable _, Variable _, Variable _ ->
            this.NamedGraphs.GetQuadsWithId (gRes)
        | Variable _,  Resource sRes, Resource pRes, Variable _ ->
            this.NamedGraphs.GetQuadsWithSubjectPredicate (sRes, pRes)
        | Variable _, Resource sRes, Variable _, Resource oRes ->
            this.NamedGraphs.GetQuadsWithSubjectObject (sRes, oRes)
        | Variable _, Variable _, Resource pRes, Resource oRes ->
            this.NamedGraphs.GetQuadsWithObjectPredicate (oRes, pRes)
        | Variable _,  Resource sRes, Variable _, Variable _ ->
            this.NamedGraphs.GetQuadsWithSubject (sRes)
        | Variable _, Variable _, Resource pRes, Variable _ ->
            this.NamedGraphs.GetQuadsWithPredicate (pRes)
        | Variable _, Variable _, Variable _, Resource oRes ->
            this.NamedGraphs.GetQuadsWithObject (oRes)
        | Variable _, Variable _, Variable _, Variable _ ->
            this.NamedGraphs.GetQuads
        | Variable s, Resource i, Resource i1, Resource i2 ->
            this.NamedGraphs.GetQuadsWithTriple (i, i1, i2)