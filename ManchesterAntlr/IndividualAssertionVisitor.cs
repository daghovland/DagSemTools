using AlcTableau.ManchesterAntlr;
using IriTools;
using Microsoft.FSharp.Collections;

namespace ManchesterAntlr;
using AlcTableau;

public class IndividualAssertionVisitor : ManchesterBaseVisitor<IEnumerable<Func<IriReference, ALC.ABoxAssertion>>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    public AnnotationVisitor AnnotationVisitor { get; init; }
    public IndividualAssertionVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
        AnnotationVisitor = new AnnotationVisitor(conceptVisitor);
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
                    fact => (individual => ALC.ABoxAssertion.NewRoleAssertion(
                individual, 
                ConceptVisitor.IriGrammarVisitor.Visit(fact.rdfiri(1)), 
                ConceptVisitor.IriGrammarVisitor.Visit(fact.rdfiri(0))
                )));
    
    public override IEnumerable<Func<IriReference, ALC.ABoxAssertion>> VisitIndividualAnnotations(ManchesterParser.IndividualAnnotationsContext context)
        =>
            context.annotationAnnotatedList().annotation()
                .Select<ManchesterParser.AnnotationContext, Func<IriReference, ALC.ABoxAssertion>>(
                    AnnotationVisitor.Visit);
                    
}