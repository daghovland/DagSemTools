using AlcTableau.ManchesterAntlr;
using IriTools;
using Microsoft.FSharp.Collections;

namespace ManchesterAntlr;
using AlcTableau;

public class IndividualAssertionVisitor : ManchesterBaseVisitor<IEnumerable<Func<IriReference, ALC.ABoxAssertion>>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    public IndividualAssertionVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
    }

    public override IEnumerable<Func<IriReference, ALC.ABoxAssertion>> VisitTypes(ManchesterParser.TypesContext context)
    =>
        context.descriptionAnnotatedList().description().Select(ConceptVisitor.Visit)
            .Select<ALC.Concept, Func<IriReference, ALC.ABoxAssertion>>(
                concept => (
                    (individual) => ALC.ABoxAssertion.NewConceptAssertion(individual, concept)));
    
}