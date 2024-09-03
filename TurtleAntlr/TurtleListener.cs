using AlcTableau;
using AlcTableau.TurtleAntlr;
using IriTools;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Rdf;
using System.Linq;


namespace TurtleAntlr
{
    public class TurtleListener : TurtleBaseListener
    {

        private IriGrammarVisitor _iriGrammarVisitor;
        private FSharpOption<IriReference> _graphName;
        private TripleTable _tripleTable;
        
        public TurtleListener()
        {
            _tripleTable = new TripleTable();
            _graphName = FSharpOption<IriReference>.None;
            _iriGrammarVisitor = new IriGrammarVisitor(_tripleTable);
        }


        public override void ExitNamedSubjectTriples(TurtleParser.NamedSubjectTriplesContext context)
        {
            var subject = _iriGrammarVisitor.Visit(context.subject());
            var triples = VisitPredicateObjectList(context.predicateObjectList())
                .SelectMany(predicateObject => predicateObject.objects
                    .Select(obj => new RDFStore.Triple(){subject = subject, predicate = predicateObject.verb, @object = obj}));
            triples.ToList().ForEach(triple => _tripleTable.AddTriple(triple));
        }
        
        public IEnumerable<(UInt32 verb, IEnumerable<UInt32> objects)> VisitPredicateObjectList(TurtleParser.PredicateObjectListContext context)
        =>
            context.verbObjectList().Select(VisitVerbObjectList);
        
        public (UInt32, IEnumerable<UInt32>) VisitVerbObjectList(TurtleParser.VerbObjectListContext context) =>
        (_iriGrammarVisitor.Visit(context.verb()), context.rdfobject().Select(rdfObj =>  VisitRdfObject(rdfObj)));
        
        public UInt32 VisitRdfObject(TurtleParser.RdfobjectContext context) =>
        context.rdfLiteral() != null ? VisitRdfLiteral(context.rdfLiteral()) : _iriGrammarVisitor.Visit(context.iri());
        
        public override void ExitPredicateObjectList(TurtleParser.PredicateObjectListContext context)
        {
            
            var predicate = _iriGrammarVisitor.Visit(context.predicate());
            var obj = _iriGrammarVisitor.Visit(context.objectList());
            var triple = RDFStore.Triple.NewTriple(predicate, obj);
            _iriGrammarVisitor.tripleTable = StoreManager.AddTriple(_iriGrammarVisitor.tripleTable, triple);
        }
        
        public TripleTable GetGraph() => _iriGrammarVisitor.tripleTable;

    }
}
