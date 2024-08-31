using AlcTableau;
using AlcTableau.TurtleAntlr;
using IriTools;
using Microsoft.FSharp.Collections;


namespace TurtleAntlr
{
    public class TurtleListener : TurtleBaseListener
    {

        private IriGrammarVisitor _iriGrammarVisitor = new IriGrammarVisitor();


        public override void ExitStatement(TurtleParser.StatementContext context)
        {
            
        }


        public ALC.OntologyDocument GetOntology()
        {
            return ALC.OntologyDocument.NewOntology(
                _iriGrammarVisitor.CreatePrefixList(),
                ALC.ontologyVersion.UnNamedOntology,
                System.Tuple.Create(ListModule.Empty<ALC.TBoxAxiom>(), ListModule.Empty<ALC.ABoxAssertion>())
            );
        }
    }
}
