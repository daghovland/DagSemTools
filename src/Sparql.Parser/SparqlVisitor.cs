using DagSemTools.Parser;
using DagSemTools.Rdf;
using IriTools;

namespace DagSemTools.Sparql.Parser;

/// <summary>
/// 
/// </summary>
public class SparqlVisitor(IVisitorErrorListener errorListener) : SparqlBaseVisitor<Query.SelectQuery>
{

    private Dictionary<string, IriReference> _prefixes;
    private IriReference? _baseIriReference;
    Dictionary<string, IriReference> _prefixes = new Dictionary<string, IriReference>();
    
    
}