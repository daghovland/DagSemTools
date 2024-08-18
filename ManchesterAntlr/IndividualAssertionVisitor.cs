using AlcTableau.ManchesterAntlr;
using IriTools;
using Microsoft.FSharp.Collections;

namespace ManchesterAntlr;
using AlcTableau;

public class IndividualAssertionVisitor : ManchesterBaseVisitor<IEnumerable<Func<IriReference, ALC.ABoxAssertion>>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    public ABoxAssertionVisitor ABoxAssertionVisitor { get; init; }
    public IndividualAssertionVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
        ABoxAssertionVisitor = new ABoxAssertionVisitor(conceptVisitor);
    }

    public override IEnumerable<Func<IriReference, ALC.ABoxAssertion>> VisitIndividualTypes(ManchesterParser.IndividualTypesContext context)
    =>
        context.descriptionAnnotatedList().description().Select(ConceptVisitor.Visit)
            .Select<ALC.Concept, Func<IriReference, ALC.ABoxAssertion>>(
                concept => (
                    (individual) => ALC.ABoxAssertion.NewConceptAssertion(individual, concept)));
    
    public override IEnumerable<Func<IriReference, ALC.ABoxAssertion>> VisitIndividualFacts(ManchesterParser.IndividualFactsContext context)
    =>
        context.factAnnotatedList().fact()
            .Select<ManchesterParser.FactContext, Func<IriReference, ALC.ABoxAssertion>>(
                    ABoxAssertionVisitor.Visit);
    
    public override IEnumerable<Func<IriReference, ALC.ABoxAssertion>> VisitIndividualAnnotations(ManchesterParser.IndividualAnnotationsContext context)
        =>
            context.annotations().annotation()
                .Select<ManchesterParser.AnnotationContext, Func<IriReference, ALC.ABoxAssertion>>(
                    ABoxAssertionVisitor.Visit);
                    
}