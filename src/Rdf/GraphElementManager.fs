namespace DagSemTools.Rdf

open System.Runtime.Versioning
open DagSemTools.Rdf.Ingress
open DagSemTools.Ingress
open System.Collections.Generic

type GraphElementManager(resourceMap: Dictionary<GraphElement, GraphElementId>,
                 resourceList: GraphElement array,
                 resourceCount: uint) =
    
    let mutable ResourceList = resourceList
    let mutable anonResourceCount = 0
    let mutable anonResourceMap : Map<string, GraphElementId> = Map.empty
    let equalityHandler : UnionFind = new UnionFind()
    member val ResourceCount = resourceCount with get, set
    
    
    member this.GetGraphElement (resourceId: GraphElementId) =
        if resourceId >= this.ResourceCount then
            invalidArg "resourceId" "Resource Id out of range"
        ResourceList.[int resourceId]
    
    member this.GetResource (resourceId: GraphElementId) =
        match this.GetGraphElement resourceId with
        | NodeOrEdge r -> Some r
        | GraphLiteral _ -> None
    
    member this.GetNamedResource (resourceId: GraphElementId) =
        match this.GetResource resourceId with
        | Some (Iri i) -> Some i
        | _ -> None
    
    
    (* This should be called wheneer the context or file or RDF dataset that is loaded changes. Then blank node names will not overlap *)
    member this.ResetBlankNodesMap() =
        anonResourceMap <- Map.empty
        
    member val GraphElementMap = resourceMap with get, set
    
    member this.GetIriResourceIds() =
        this.GraphElementMap
            |> Seq.choose (fun res ->
                match res.Key with
                | GraphLiteral _ -> None
                | NodeOrEdge node ->
                    match node with
                    |  Iri _ -> Some res.Value
                    | _ -> None)
    new(init_rdf_size : uint) =
        let init_resources = max 10 (int init_rdf_size / 10)
        let init_triples = max 10 (int init_rdf_size / 60)
        GraphElementManager(new Dictionary<GraphElement, GraphElementId>(),
                    Array.zeroCreate init_resources,
                    0u
                    )
    member this.doubleResourceListSize () =
        ResourceList <- doubleArraySize ResourceList
    
    member this.AddResource resource =
        if this.GraphElementMap.ContainsKey resource then
            this.GraphElementMap[resource]
        else
            let nextResourceIndex = this.ResourceCount + 1u
            if nextResourceIndex > uint32(ResourceList.Length) then
                    this.doubleResourceListSize()
            ResourceList.[int(this.ResourceCount)] <- resource
            this.GraphElementMap.Add (resource, this.ResourceCount) |> ignore
            this.ResourceCount <- nextResourceIndex
            nextResourceIndex - 1u
    member this.AddLiteralResource literalResource =
        this.AddResource (GraphLiteral literalResource)
    member this.AddNodeResource literalResource =
        this.AddResource (NodeOrEdge literalResource)
    member this.CreateUnnamedAnonResource() =
        anonResourceCount <- anonResourceCount + 1
        let newAnonResource = AnonymousBlankNode((uint) anonResourceCount)
        this.AddNodeResource(newAnonResource)
    member this.GetOrCreateNamedAnonResource(name) =
        match anonResourceMap.TryGetValue(name) with
        | false, _ ->
                        let anonResource = this.CreateUnnamedAnonResource()
                        anonResourceMap <- anonResourceMap.Add(name, anonResource)
                        anonResource
        | true, anonResource -> anonResource
    
    
    
    member this.GetResourceTriple(triple: Triple) =
        {
          TripleResource.subject = ResourceList.[int triple.subject]
          TripleResource.predicate = ResourceList.[int triple.predicate]
          TripleResource.obj = ResourceList.[int triple.obj]
        }
     
    
