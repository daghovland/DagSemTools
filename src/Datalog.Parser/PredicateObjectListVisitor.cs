using DagSemTools.Rdf;

namespace DagSemTools.Datalog.Parser;

using static DatalogParser;

internal class PredicateObjectListVisitor : DatalogBaseVisitor<Func<uint, List<Rdf.Ingress.Triple>>>
{
    private ResourceVisitor _resourceVisitor;

    internal PredicateObjectListVisitor(ResourceVisitor resourceVisitor)
    {
        _resourceVisitor = resourceVisitor;
    }

    /// <summary>
    /// Visits the grammar rule predicateObjectList used in https://www.w3.org/TR/rdf12-turtle/#grammar-production-blankNodePropertyList
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Func<uint, List<Rdf.Ingress.Triple>> VisitPredicateObjectList(
        PredicateObjectListContext context) =>
        (node) =>
            context.verbObjectList()
                .SelectMany(vo => VisitVerbObjectList(vo)(node))
                .ToList();

    /// <summary>
    /// Used for the aggregate calculation of a annotation. 
    /// </summary>
    /// <param name="_reifierNodeId"></param>
    /// <param name="_triples"></param>
    /// <param name="_tripleIds"></param>
    private struct AnnotationStatus(uint _reifierNodeId, IEnumerable<Rdf.Ingress.Triple> _triples, IEnumerable<uint> _tripleIds)
    {
        internal uint reifierNodeId = _reifierNodeId;
        internal IEnumerable<Rdf.Ingress.Triple> triples = _triples;
        internal IEnumerable<uint> tripleIds = _tripleIds;
    }

    private void HandleAnnotation((AnnotationContext? annot, Rdf.Ingress.Triple triple) rdfobj)
    {
        if (rdfobj.annot != null && rdfobj.annot.children != null)
        {
            var reifications = rdfobj.annot.children
                .Aggregate(seed: new AnnotationStatus(_resourceVisitor.Datastore.NewAnonymousBlankNode(), new List<Rdf.Ingress.Triple>(), new List<uint>()),
                    func: (aggr, child) => child switch
                    {
                        PredicateObjectListContext predobj => HandlePredicateObjectReification(predobj, aggr),
                        ReifierContext reif => HandleReification(reif, rdfobj.triple, aggr),
                        _ => aggr
                    });
            reifications.triples.ToList().ForEach(_resourceVisitor.Datastore.AddTriple);
            reifications.tripleIds.ToList().ForEach(tripleId => _resourceVisitor.Datastore.AddReifiedTriple(rdfobj.triple, tripleId));
        }
    }
    private AnnotationStatus HandlePredicateObjectReification(PredicateObjectListContext predobj, AnnotationStatus aggr)
    {
        var newTriples = VisitPredicateObjectList(predobj)(aggr.reifierNodeId);
        return new AnnotationStatus(aggr.reifierNodeId, aggr.triples.Concat(newTriples), aggr.tripleIds);
    }
    private AnnotationStatus HandleReification(ReifierContext reif, Rdf.Ingress.Triple triple, AnnotationStatus aggr)
    {
        var reifier = _resourceVisitor.Visit(reif);
        return new AnnotationStatus(reifier, aggr.triples, aggr.tripleIds.Append(reifier));
    }


    /// <summary>
    /// Visits the grammar rule verbObjectList which is a merging of predicateObjectList and objectLists in https://www.w3.org/TR/rdf12-turtle
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Func<uint, List<Rdf.Ingress.Triple>> VisitVerbObjectList(
        VerbObjectListContext context) =>
        (node) =>
        {
            var predicate = _resourceVisitor.Visit(context.verb());
            var tripes = context.annotatedObject()
                .Select(rdfObj => (annot: rdfObj.annotation(), obj: _resourceVisitor.Visit(rdfObj.rdfobject())))
                .Select(obj => (annot: obj.annot, triple: new Rdf.Ingress.Triple(node, predicate, obj.obj)))
                .ToList();
            tripes
                .ForEach(HandleAnnotation);
            return tripes.Select(triple => triple.triple).ToList();
        };

}