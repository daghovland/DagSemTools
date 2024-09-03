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
        
        public TurtleListener()
        {
            var tripleTable = StoreManager.init();
            _graphName = FSharpOption<IriReference>.None;
            _iriGrammarVisitor = new IriGrammarVisitor(tripleTable);
        }


        public override void ExitNamedSubjectTriples(TurtleParser.NamedSubjectTriplesContext context)
        {
            var subject = _iriGrammarVisitor.Visit(context.subject()));
            var triples = VisitPredicateObjectList(context.predicateObjectList())
                .SelectMany(predicateObject => predicateObject.objects
                    .Select(obj => new RDFStore.Triple(){subject = subject, predicate = predicateObject.verb, @object = obj}));
            var tripleTable = _iriGrammarVisitor.tripleTable; 
                StoreManager.AddResource() Triples(_iriGrammarVisitor.tripleTable, triples);
        }
        
        public IEnumerable<(UInt32 verb, IEnumerable<UInt32> objects)> VisitPredicateObjectList(TurtleParser.PredicateObjectListContext context)
        =>
            context.verbObjectList().Select(VisitVerbObjectList);
        
        public (UInt32, IEnumerable<UInt32>) VisitVerbObjectList(TurtleParser.VerbObjectListContext context) =>
        (_iriGrammarVisitor.Visit(context.verb()), context.rdfObject().Select(VisitRdfObject));
        
        public IriReference VisitRdfObject(TurtleParser.RdfObjectContext context) =>
        context.rdfLiteral() != null ? VisitRdfLiteral(context.rdfLiteral()) : _iriGrammarVisitor.Visit(context.iri());
        
        public override void ExitPredicateObjectList(TurtleParser.PredicateObjectListContext context)
        {
            
            var predicate = _iriGrammarVisitor.Visit(context.predicate());
            var obj = _iriGrammarVisitor.Visit(context.objectList());
            var triple = RDFStore.Triple.NewTriple(predicate, obj);
            _iriGrammarVisitor.tripleTable = StoreManager.AddTriple(_iriGrammarVisitor.tripleTable, triple);
        }
        
        public RDFStore.TripleTable GetGraph() => _iriGrammarVisitor.tripleTable;

    }
}
