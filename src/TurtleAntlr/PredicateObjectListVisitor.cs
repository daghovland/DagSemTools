using AlcTableau.Rdf;

namespace AlcTableau.TurtleAntlr;

public class PredicateObjectListVisitor : TurtleBaseVisitor<Func<uint, List<RDFStore.Triple>>>
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
    public override Func<uint, List<RDFStore.Triple>> VisitPredicateObjectList(
        TurtleParser.PredicateObjectListContext context) =>
        (node) =>
            context.verbObjectList()
                .SelectMany(vo => VisitVerbObjectList(vo)(node))
                .ToList();


    /// <summary>
    /// Visits the grammar rule verbObjectList used in https://www.w3.org/TR/rdf12-turtle/#grammar-production-verbObjectList
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Func<uint, List<RDFStore.Triple>> VisitVerbObjectList(
        TurtleParser.VerbObjectListContext context) =>
        (node) =>
        {
            var predicate = _resourceVisitor.Visit(context.verb());
            return context.rdfobject()
                .Select(rdfObj => _resourceVisitor.Visit(rdfObj))
                .Select(obj => new RDFStore.Triple(node, predicate, obj))
                .ToList();
        };

}