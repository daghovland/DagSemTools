namespace DagSemTools.Rdf

open DagSemTools.Rdf.Ingress
open Ingress
open System
open DagSemTools.Ingress
open IriTools

type Datastore(triples: TripleTable,
               reifiedTriples: QuadTable,
               namedGraphs: QuadTable,
               resources: GraphElementManager) =
    member val Triples = triples with get, set
    member val ReifiedTriples = reifiedTriples with get, set
    member val NamedGraphs = namedGraphs with get, set
    member val Resources = resources with get, set
    
    new(init_rdf_size : uint) =
        let init_resources : uint = uint ( max 10 (int init_rdf_size / 10) )
        let init_triples = uint ( max 10 (int init_rdf_size / 60) )
        Datastore(new TripleTable(init_triples),
                  new QuadTable(init_triples),
                  new QuadTable(init_triples),
                  new GraphElementManager(init_resources))
        
    
    new(elementManager : GraphElementManager, init_triples : uint) =
        Datastore(new TripleTable(init_triples),
                  new QuadTable(init_triples),
                  new QuadTable(init_triples),
                  elementManager)
    member this.AddTriple (triple: Triple) =
        this.Triples.AddTriple triple
    
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
    
    member this.GetGraphElement (resourceId: GraphElementId) : GraphElement =
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(resourceId, this.Resources.ResourceCount);
        this.Resources.GetGraphElement(resourceId)
        
    member this.GetGraphNode (resourceId: GraphElementId) : RdfResource option =
        match this.GetGraphElement(resourceId) with
        | NodeOrEdge node -> Some node
        | GraphLiteral _ -> None
        
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
        
    member this.GetTriplesWithSubject (subject: GraphElementId) : Triple seq =
        this.Triples.GetTriplesWithSubject subject
    member this.GetTriplesWithSubject (graphid: GraphElementId, subject: GraphElementId)  =
        this.NamedGraphs.GetQuadsWithIdSubject (graphid, subject)
    
    member this.GetTriplesWithObject (object: GraphElementId) : Triple seq =
        this.Triples.GetTriplesWithObject object
    member this.GetTriplesWithObject (graphid: GraphElementId, object: GraphElementId)  =
        this.NamedGraphs.GetQuadsWithIdObject (graphid, object)
    member this.GetTriplesWithPredicate (predicate: GraphElementId) : Triple seq =
        this.Triples.GetTriplesWithPredicate predicate
    
    member this.GetTriplesWithPredicate (graphid: GraphElementId, predicate: GraphElementId)  =
        this.NamedGraphs.GetQuadsWithIdPredicate (graphid, predicate)
    
    member this.GetTriplesWithSubjectPredicate (subject: GraphElementId, predicate: GraphElementId) =
        this.Triples.GetTriplesWithSubjectPredicate (subject, predicate)
    
    member this.GetTriplesWithSubjectPredicate (graphId : GraphElementId, subject: GraphElementId, predicate: GraphElementId) =
        this.NamedGraphs.GetQuadsWithIdSubjectPredicate(graphId, subject, predicate)
    member this.GetTriplesWithObjectPredicate (object: GraphElementId, predicate: GraphElementId) =
        this.Triples.GetTriplesWithObjectPredicate (object, predicate)
    member this.GetTriplesWithObjectPredicate (graphId : GraphElementId, object: GraphElementId, predicate: GraphElementId) =
        this.NamedGraphs.GetQuadsWithIdObjectPredicate(graphId, object, predicate)
    
        
    member this.GetTriplesWithSubjectObject (subject: GraphElementId, object: GraphElementId)  =
        this.Triples.GetTriplesWithSubjectObject (subject, object)
    member this.GetTriplesWithSubjectObject (graphId: GraphElementId, subject: GraphElementId, object: GraphElementId)  =
        this.NamedGraphs.GetQuadsWithIdSubjectPredicate (graphId, subject, object)
        
    member this.ContainsTriple (triple : Triple) : bool =
            this.Triples.Contains triple
        
    member this.GetReifiedTriplesWithId(id: GraphElementId) : Quad seq =
        this.ReifiedTriples.GetQuadsWithId id
    member this.GetReifiedTriplesWithSubject(subject: GraphElementId) : Quad seq =
        this.ReifiedTriples.GetQuadsWithSubject subject
    member this.GetReifiedTriplesWithPredicate(predicate: GraphElementId) : Quad seq =
        this.ReifiedTriples.GetQuadsWithPredicate predicate
    member this.GetReifiedTriplesWithObject(obj: GraphElementId) : Quad seq =
        this.ReifiedTriples.GetQuadsWithObject obj
    member this.GetReifiedTriplesWithSubjectPredicate(subject: GraphElementId, predicate: GraphElementId) : Quad seq =
        this.ReifiedTriples.GetQuadsWithSubjectPredicate (subject, predicate)

    member this.GetResourceInfoForErrorMessage(subject: GraphElementId) : string =
           this.Triples.GetTriplesMentioning subject
            |> Seq.map this.Resources.GetResourceTriple
            |> Seq.map _.ToString()
            |> String.concat ". "
            
    