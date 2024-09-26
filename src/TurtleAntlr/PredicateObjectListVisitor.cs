using AlcTableau.Rdf;

namespace AlcTableau.TurtleAntlr;

public class PredicateObjectListVisitor : TurtleBaseVisitor<Func<uint, IEnumerable<RDFStore.Triple>>>
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
    public override Func<uint, IEnumerable<RDFStore.Triple>> VisitPredicateObjectList(TurtleParser.PredicateObjectListContext context) =>
    (node) => 
       context.verbObjectList()
           .Select(VisitVerbObjectList)
            .SelectMany(predicateObject => predicateObject.objects
                .Select(obj => new RDFStore.Triple(node, predicateObject.verb, obj)));
    
    public (UInt32 verb, IEnumerable<UInt32> objects) VisitVerbObjectList(TurtleParser.VerbObjectListContext context) =>
        (_resourceVisitor.Visit(context.verb()), context.rdfobject().Select(rdfObj => _resourceVisitor.Visit(rdfObj)));

}