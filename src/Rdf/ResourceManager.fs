namespace DagSemTools.Rdf

open Ingress

open System
open System.Collections.Generic



type ResourceManager(resourceMap: Dictionary<Resource, ResourceId>,
                 resourceList: Resource array,
                 resourceCount: uint) =
    
    member val ResourceMap = resourceMap with get, set
    member val ResourceList = resourceList with get, set
    member val ResourceCount = resourceCount with get, set
    
    new(init_rdf_size : uint) =
        let init_resources = max 10 (int init_rdf_size / 10)
        let init_triples = max 10 (int init_rdf_size / 60)
        ResourceManager(new Dictionary<Resource, ResourceId>(),
                    Array.zeroCreate init_resources,
                    0u
                    )
    member this.doubleResourceListSize () =
        this.ResourceList <- doubleArraySize this.ResourceList
    
    member this.AddResource resource =
        if this.ResourceMap.ContainsKey resource then
            this.ResourceMap[resource]
        else
            let nextResourceIndex = this.ResourceCount + 1u
            if nextResourceIndex > uint32(this.ResourceList.Length) then
                    this.doubleResourceListSize()
            this.ResourceList.[int(this.ResourceCount)] <- resource
            this.ResourceMap.Add (resource, this.ResourceCount) |> ignore
            this.ResourceCount <- nextResourceIndex
            nextResourceIndex - 1u
        
    member this.NewAnonymousBlankNode() =
        this.AddResource(Ingress.Resource.AnonymousBlankNode this.ResourceCount);

    member this.GetResourceTriple(triple: Triple) =
        {
          TripleResource.subject = this.ResourceList.[int triple.subject]
          TripleResource.predicate = this.ResourceList.[int triple.predicate]
          TripleResource.obj = this.ResourceList.[int triple.object]
        }
        
