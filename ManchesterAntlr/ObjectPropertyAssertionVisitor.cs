using AlcTableau.ManchesterAntlr;
using IriTools;
using Microsoft.FSharp.Collections;

namespace ManchesterAntlr;
using AlcTableau;

public class ObjectPropertyAssertionVisitor : ManchesterBaseVisitor<IEnumerable<Func<IriReference, ALC.TBoxAxiom>>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    public ObjectPropertyAssertionVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
    }
    
}