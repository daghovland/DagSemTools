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
        
        public TurtleListener()
        {
            TripleTable = new TripleTable();
            _graphName = FSharpOption<IriReference>.None;
            _iriGrammarVisitor = new IriGrammarVisitor(TripleTable);
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
        (_iriGrammarVisitor.Visit(context.verb()), context.rdfobject().Select(rdfObj =>  _iriGrammarVisitor.Visit(rdfObj)));
        
    }

