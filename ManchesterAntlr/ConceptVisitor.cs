using AlcTableau.ManchesterAntlr;
using IriTools;

namespace ManchesterAntlr;
using AlcTableau;

public class ConceptVisitor : ConceptBaseVisitor<ALC.Concept>
{
    private readonly IriGrammarVisitor _iriGrammarVisitor;
    public ConceptVisitor()
    {
        _iriGrammarVisitor = new IriGrammarVisitor();
    }
    public ConceptVisitor(Dictionary<string, IriReference> prefixes)
    {
        _iriGrammarVisitor = new IriGrammarVisitor(prefixes);
    }
    public override ALC.Concept VisitStart(ConceptParser.StartContext context) =>
        Visit(context.GetChild(0));
    
    public override ALC.Concept VisitIriPrimary(ConceptParser.IriPrimaryContext context)
    {
        var iri = _iriGrammarVisitor.Visit(context.rdfiri());
        return ALC.Concept.NewConceptName(iri);
    }

    public override ALC.Concept VisitParenthesizedPrimary(ConceptParser.ParenthesizedPrimaryContext context) =>
        Visit(context.description());
    public override ALC.Concept VisitActualDisjunction(ConceptParser.ActualDisjunctionContext context) =>
        ALC.Concept.NewDisjunction(Visit(context.description()), Visit(context.conjunction()));
    
    public override ALC.Concept VisitSingleDisjunction(ConceptParser.SingleDisjunctionContext context) =>
        Visit(context.conjunction());

    public override ALC.Concept VisitActualConjunction(ConceptParser.ActualConjunctionContext context)
    {
        var conjunction = Visit(context.conjunction());
        var primary = Visit(context.primary());
        return ALC.Concept.NewConjunction(conjunction, primary);
    }
    public override ALC.Concept VisitSingleConjunction(ConceptParser.SingleConjunctionContext context) =>
        Visit(context.primary());
    
    public override ALC.Concept VisitUniversalRestriction(ConceptParser.UniversalRestrictionContext context) =>
        ALC.Concept.NewUniversal(
            _iriGrammarVisitor.Visit(context.rdfiri()),
            Visit(context.primary()));

    public override ALC.Concept VisitExistentialRestriction(ConceptParser.ExistentialRestrictionContext context) =>
        ALC.Concept.NewExistential(
            _iriGrammarVisitor.Visit(context.rdfiri()),
            Visit(context.primary()));
    
    
}