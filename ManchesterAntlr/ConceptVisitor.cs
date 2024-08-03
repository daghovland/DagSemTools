using AlcTableau.ManchesterAntlr;
using IriTools;

namespace ManchesterAntlr;
using AlcTableau;

public class ConceptVisitor : ManchesterBaseVisitor<ALC.Concept>
{
    public IriGrammarVisitor IriGrammarVisitor { get; init; }
    public ConceptVisitor()
    {
        IriGrammarVisitor = new IriGrammarVisitor();
    }
    public ConceptVisitor(IriGrammarVisitor iriGrammarVisitor)
    {
        IriGrammarVisitor = iriGrammarVisitor;
    }
    

    public ConceptVisitor(Dictionary<string, IriReference> prefixes)
    {
        IriGrammarVisitor = new IriGrammarVisitor(prefixes);
    }
    public override ALC.Concept VisitIriPrimary(ManchesterParser.IriPrimaryContext context)
    {
        var iri = IriGrammarVisitor.Visit(context.rdfiri());
        return ALC.Concept.NewConceptName(iri);
    }

    public override ALC.Concept VisitParenthesizedPrimary(ManchesterParser.ParenthesizedPrimaryContext context) =>
        Visit(context.description());
    public override ALC.Concept VisitActualDisjunction(ManchesterParser.ActualDisjunctionContext context) =>
        ALC.Concept.NewDisjunction(Visit(context.description()), Visit(context.conjunction()));
    
    public override ALC.Concept VisitSingleDisjunction(ManchesterParser.SingleDisjunctionContext context) =>
        Visit(context.conjunction());

    public override ALC.Concept VisitActualConjunction(ManchesterParser.ActualConjunctionContext context)
    {
        var conjunction = Visit(context.conjunction());
        var primary = Visit(context.primary());
        return ALC.Concept.NewConjunction(conjunction, primary);
    }
    public override ALC.Concept VisitSingleConjunction(ManchesterParser.SingleConjunctionContext context) =>
        Visit(context.primary());
    
    public override ALC.Concept VisitUniversalRestriction(ManchesterParser.UniversalRestrictionContext context) =>
        ALC.Concept.NewUniversal(
            IriGrammarVisitor.Visit(context.rdfiri()),
            Visit(context.primary()));

    public override ALC.Concept VisitExistentialRestriction(ManchesterParser.ExistentialRestrictionContext context) =>
        ALC.Concept.NewExistential(
            IriGrammarVisitor.Visit(context.rdfiri()),
            Visit(context.primary()));
    
    
}