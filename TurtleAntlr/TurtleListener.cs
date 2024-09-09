using AlcTableau.TurtleAntlr;
using IriTools;
using Microsoft.FSharp.Core;
using Rdf;
namespace AlcTableau.TurtleAntlr;

public class TurtleListener : TurtleBaseListener
{

    private IriGrammarVisitor _iriGrammarVisitor;
    private ResourceVisitor _resourceVisitor;
    private FSharpOption<IriReference> _graphName;
    public TripleTable TripleTable { get; init; }

    public TurtleListener(uint init_size)
    {
        TripleTable = new TripleTable(init_size);
        _graphName = FSharpOption<IriReference>.None;
        _iriGrammarVisitor = new IriGrammarVisitor();
        _resourceVisitor = new ResourceVisitor(TripleTable, _iriGrammarVisitor);
    }

    /// <summary>
    /// Used to transform <iri> into iri in the handlers below
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string GetStringExcludingFirstAndLast(string input)
    {
        if (input.Length > 2)
        {
            return input.Substring(1, input.Length - 2);
        }
        return string.Empty;
    }

    /// <summary>
    /// Used to transform prefix: into prefix in the methods below
    /// </summary>
    /// <param name="context"></param>
    public static string GetStringExcludingLastColon(string prefixNs)
    {
        if (prefixNs.Length >= 1 && prefixNs[^1] == ':')
        {
            return prefixNs.Substring(0, prefixNs.Length - 1);
        }
        throw new Exception($"Invalid prefix {prefixNs}. Prefix should end with ':'");
    }
    public override void ExitBase(TurtleParser.BaseContext context)
    {
        var iriString = GetStringExcludingFirstAndLast(context.ABSOLUTEIRIREF().GetText());
        var iri = new IriReference(iriString);
        _iriGrammarVisitor.SetBase(iri);
    }

    public override void ExitPrefixId(TurtleParser.PrefixIdContext context)
    {
        var prefix = GetStringExcludingLastColon(context.PNAME_NS().GetText());
        var iri = _iriGrammarVisitor.Visit(context.iri());
        _iriGrammarVisitor.AddPrefix(prefix, iri);
    }
    public override void ExitSparqlPrefix(TurtleParser.SparqlPrefixContext context)
    {
        var prefix = GetStringExcludingLastColon(context.PNAME_NS().GetText());
        var iri = _iriGrammarVisitor.Visit(context.iri());
        _iriGrammarVisitor.AddPrefix(prefix, iri);
    }

    public override void ExitNamedSubjectTriples(TurtleParser.NamedSubjectTriplesContext context)
    {
        var curSubject = _resourceVisitor.Visit(context.subject());
        var triples = VisitPredicateObjectList(context.predicateObjectList())
            .SelectMany(predicateObject => predicateObject.objects
                .Select(obj => new RDFStore.Triple(curSubject, predicateObject.verb, obj)));
        triples.ToList().ForEach(triple => TripleTable.AddTriple(triple));
    }

    public IEnumerable<(UInt32 verb, IEnumerable<UInt32> objects)> VisitPredicateObjectList(TurtleParser.PredicateObjectListContext context)
    =>
        context.verbObjectList().Select(VisitVerbObjectList);

    public (UInt32, IEnumerable<UInt32>) VisitVerbObjectList(TurtleParser.VerbObjectListContext context) =>
    (_resourceVisitor.Visit(context.verb()), context.rdfobject().Select(rdfObj => _resourceVisitor.Visit(rdfObj)));

}

