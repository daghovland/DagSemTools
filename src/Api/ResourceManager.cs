using DagSemTools.Ingress;
using DagSemTools.Rdf;
using IriTools;
using Microsoft.FSharp.Core;

namespace DagSemTools.Api;

internal class ResourceManager(GraphElementManager ElementManager)
{

    internal RdfLiteral CreateRdfStringLiteral(string value) => 
        new RdfLiteral(ElementManager, DagSemTools.Ingress.RdfLiteral.NewLiteralString(value));
    
    internal IriResource CreateIriResource(string value) => 
        new IriResource(ElementManager, new IriReference(value));
    
    /// <summary>
    /// Creates a triple with IRIs on all three places
    /// </summary>
    /// <param name="elementManager"></param>
    /// <param name="subject"></param>
    /// <param name="predicate"></param>
    /// <param name="object"></param>
    internal Triple CreateTriple(IriReference subject, IriReference predicate, IriReference @object)
    => new(ElementManager, subject, predicate, @object);

    
    internal Resource GetBlankNodeOrIriResource(uint resourceId)
    {
        var resource = ElementManager.GetGraphNode(resourceId);
        if (!FSharpOption<RdfResource>.get_IsSome(resource))
            throw new ArgumentException($"Resource {resource} is not an Iri or a blank node"); ;

        switch (resource.Value)
        {
            case { IsIri: true } r:
                return new IriResource(ElementManager, new IriReference(r.iri));
            case { IsAnonymousBlankNode: true } r:
                return new BlankNodeResource($"{r.anon_blankNode}");
            default: throw new Exception($"BUG: Resource {resource.ToString()} is a resource but not an Iri or a blank node");
        }
    }


    internal IriResource GetApiIriResource(uint resourceId)
    {
        var resource = GetBlankNodeOrIriResource(resourceId);
        if (resource is IriResource r)
            return r;
        throw new ArgumentException($"Resource {resource.ToString()} is not an Iri");
    }
    internal GraphElement GetResource(uint resourceId)
    {
        var resource = ElementManager.GetGraphElement(resourceId);
        if (resource.IsNodeOrEdge)
        {
            var r = resource.resource;
            if (r.IsIri)
                return new IriResource(ElementManager, new IriReference(r.iri));
            if (r.IsAnonymousBlankNode)
                return new BlankNodeResource($"{r.anon_blankNode}");
            throw new Exception("BUG: Resource that is neither Iri nor Blank Node !!");
        }

        if (!resource.IsGraphLiteral) throw new Exception("BUG: Resource that is neither resource or literal!!");
        var lit = resource.literal;
        return new RdfLiteral(ElementManager, lit);
    }


    internal Triple EnsureApiTriple(DagSemTools.Rdf.Ingress.Triple triple) =>
        new(ElementManager,
            GetBlankNodeOrIriResource(triple.subject),
            GetApiIriResource(triple.predicate).Iri,
            GetResource(triple.obj));



    internal Quad EnsureApiQuad(DagSemTools.Rdf.Ingress.Quad quad) =>
        new(ElementManager,
            GetApiIriResource(quad.tripleId),
            GetBlankNodeOrIriResource(quad.subject),
            GetApiIriResource(quad.predicate).Iri,
            GetResource(quad.obj));

}