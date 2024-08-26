using AlcTableau;
using IriTools;
using Microsoft.FSharp.Collections;
using AlcTableau.ManchesterAntlr;

namespace TurtleAntlr
{
    public class TurtleListener : TurtleBaseListener
    {
        readonly Dictionary<string, IriReference> prefixes = new();

        private IriGrammarVisitor iriGrammarVisitor = new IriGrammarVisitor();
        
        private FSharpList<ALC.prefixDeclaration> CreatePrefixList()
        {
            var prefixList = new List<ALC.prefixDeclaration>();
            foreach (var kvp in prefixes)
            {
                var prefix = ALC.prefixDeclaration.NewPrefixDefinition(kvp.Key, kvp.Value);
                prefixList.Add(prefix);
            }
            return ListModule.OfSeq(prefixList);
        }
    
        public ALC.OntologyDocument GetOntology ()
        {
            return ALC.OntologyDocument.NewOntology(
                CreatePrefixList(),
                ALC.ontologyVersion.UnNamedOntology,
                System.Tuple.Create(ListModule.Empty<ALC.TBoxAxiom>(), ListModule.Empty<ALC.ABoxAssertion>())
            );
        }
    }
}
