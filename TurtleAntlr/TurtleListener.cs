using AlcTableau;
using AlcTableau.TurtleAntlr;
using IriTools;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Rdf;


namespace TurtleAntlr
{
    public class TurtleListener : TurtleBaseListener
    {

        private IriGrammarVisitor _iriGrammarVisitor = new IriGrammarVisitor();
        private FSharpOption<IriReference> _graphName = FSharpOption<IriReference>.None;
        private FSharpList<AbstractRdf.Triple> _triples = new FSharpList<AbstractRdf.Triple>();

        public override void ExitNamedSubjectTriples(TurtleParser.NaStatementContext context)
        {
            context.            
        }


        public Rdf.AbstractRdf.Graph GetGraph()
        {
            return new AbstractRdf.Graph(
                _graphName,
                _triples
                );
        }
    }
}
