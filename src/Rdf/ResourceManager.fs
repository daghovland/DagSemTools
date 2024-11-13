namespace DagSemTools.Rdf

open DagSemTools.Rdf.Ingress
open DagSemTools.Resource
open System.Collections.Generic

type ResourceManager(resourceMap: Dictionary<Resource, ResourceId>,
                 resourceList: Resource array,
                 resourceCount: uint) =
    
    let mutable ResourceList = resourceList
    member val ResourceCount = resourceCount with get, set
    
    member this.GetResource (resourceId: ResourceId) =
        if resourceId >= this.ResourceCount then
            invalidArg "resourceId" "Resource Id out of range"
        ResourceList.[int resourceId]
    
    member val ResourceMap = resourceMap with get, set
    
    member this.GetIriResourceIds() =
        this.ResourceMap
            |> Seq.choose (fun res ->
            match res.Key with
            | Iri _ -> Some res.Value
            | _ -> None)
    new(init_rdf_size : uint) =
        let init_resources = max 10 (int init_rdf_size / 10)
        let init_triples = max 10 (int init_rdf_size / 60)
        ResourceManager(new Dictionary<Resource, ResourceId>(),
                    Array.zeroCreate init_resources,
                    0u
                    )
    member this.doubleResourceListSize () =
        ResourceList <- doubleArraySize ResourceList
    
    member this.AddResource resource =
        if this.ResourceMap.ContainsKey resource then
            this.ResourceMap[resource]
        else
            let nextResourceIndex = this.ResourceCount + 1u
            if nextResourceIndex > uint32(ResourceList.Length) then
                    this.doubleResourceListSize()
            ResourceList.[int(this.ResourceCount)] <- resource
            this.ResourceMap.Add (resource, this.ResourceCount) |> ignore
            this.ResourceCount <- nextResourceIndex
            nextResourceIndex - 1u
        
    member this.NewAnonymousBlankNode() =
        this.AddResource(Resource.AnonymousBlankNode this.ResourceCount);

    member this.GetResourceTriple(triple: Triple) =
        {
          TripleResource.subject = ResourceList.[int triple.subject]
          TripleResource.predicate = ResourceList.[int triple.predicate]
          TripleResource.obj = ResourceList.[int triple.obj]
        }
        
