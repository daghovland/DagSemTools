using AlcTableau.TurtleAntlr;
using IriTools;
using Microsoft.FSharp.Core;
using Rdf;
namespace AlcTableau.TurtleAntlr;

public class TurtleListener : TurtleBaseListener
{

    private IriGrammarVisitor _iriGrammarVisitor;
    private FSharpOption<IriReference> _graphName;
    public TripleTable TripleTable { get; init; }

    public TurtleListener(uint init_size)
    {
        TripleTable = new TripleTable(init_size);
        _graphName = FSharpOption<IriReference>.None;
        _iriGrammarVisitor = new IriGrammarVisitor(TripleTable);
    }

    public static string GetStringExcludingFirstAndLast(string input)
    {
        if (input.Length > 2)
        {
            return input.Substring(1, input.Length - 2);
        }
        return string.Empty;
    }
    public override void ExitBase(TurtleParser.BaseContext context)
    {
        var iriString = GetStringExcludingFirstAndLast(context.IRIREF().GetText());
        var iri = new IriReference(iriString);
        _iriGrammarVisitor.SetBase(iri);
    }
    
    public override void ExitPrefixId(TurtleParser.PrefixIdContext context)
    {
        var prefix = context.PNAME_NS().GetText();
        var iriString = GetStringExcludingFirstAndLast(context.IRIREF().GetText());
        var iri = new IriReference(iriString);
        _iriGrammarVisitor.AddPrefix(prefix, iri);
    }
    public override void ExitSparqlPrefix(TurtleParser.SparqlPrefixContext context)
    {
        var prefix = context.PNAME_LN().GetText();
        var iriString =  context.IRIREF().GetText()[1..-1];
        var iri = new IriReference(iriString);
        _iriGrammarVisitor.AddPrefix(prefix, iri);
    }
    
    public override void ExitNamedSubjectTriples(TurtleParser.NamedSubjectTriplesContext context)
    {
        var subject = _iriGrammarVisitor.Visit(context.subject());
        var triples = VisitPredicateObjectList(context.predicateObjectList())
            .SelectMany(predicateObject => predicateObject.objects
                .Select(obj => new RDFStore.Triple(subject, predicateObject.verb, obj)));
        triples.ToList().ForEach(triple => TripleTable.AddTriple(triple));
    }

    public IEnumerable<(UInt32 verb, IEnumerable<UInt32> objects)> VisitPredicateObjectList(TurtleParser.PredicateObjectListContext context)
    =>
        context.verbObjectList().Select(VisitVerbObjectList);

    public (UInt32, IEnumerable<UInt32>) VisitVerbObjectList(TurtleParser.VerbObjectListContext context) =>
    (_iriGrammarVisitor.Visit(context.verb()), context.rdfobject().Select(rdfObj => _iriGrammarVisitor.Visit(rdfObj)));

}

