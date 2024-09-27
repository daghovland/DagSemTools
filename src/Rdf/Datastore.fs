namespace AlcTableau.Rdf
open AlcTableau.Rdf.Ingress
open Ingress

open System
open System.Collections.Generic

type Datastore(triples: TripleTable,
               resources: ResourceManager) =
    member val Triples = triples with get, set
    member val Resources = resources with get, set
    
    new(init_rdf_size : uint) =
        let init_resources : uint = uint ( max 10 (int init_rdf_size / 10) )
        let init_triples = uint ( max 10 (int init_rdf_size / 60) )
        Datastore(new TripleTable(init_triples),
                  new ResourceManager(init_resources))
        
    member this.AddTriple (triple: Triple) =
        this.Triples.AddTriple triple
        
    member this.AddResource (resource: Resource) : ResourceId =
        this.Resources.AddResource resource
    
    member this.GetResource (resourceId: ResourceId) : Resource =
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(resourceId, this.Resources.ResourceCount);
        this.Resources.ResourceList.[int resourceId]
        
    member this.GetResourceId (resource : Resource) =
        this.Resources.ResourceMap.[resource]
    member this.NewAnonymousBlankNode() =
        this.Resources.NewAnonymousBlankNode()
    member this.GetResourceTriple (triple: Triple) =
        this.Resources.GetResourceTriple triple
        
    member this.GetTriplesWithSubject (subject: ResourceId) : Triple seq =
        this.Triples.GetTriplesWithSubject subject
    
    member this.GetTriplesWithObject (object: ResourceId) : Triple seq =
        this.Triples.GetTriplesWithObject object
    
    member this.GetTriplesWithPredicate (predicate: ResourceId) : Triple seq =
        this.Triples.GetTriplesWithPredicate predicate
    
    member this.GetTriplesWithSubjectPredicate (subject: ResourceId, predicate: ResourceId) =
        this.Triples.GetTriplesWithSubjectPredicate (subject, predicate)
    
    member this.GetTriplesWithObjectPredicate (object: ResourceId, predicate: ResourceId) =
        this.Triples.GetTriplesWithObjectPredicate (object, predicate)
        
    member this.GetTriplesWithSubjectObject (subject: ResourceId, object: ResourceId) : Triple seq =
        this.Triples.GetTriplesWithSubjectObject (subject, object)

