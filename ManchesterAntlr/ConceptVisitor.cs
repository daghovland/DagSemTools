using AlcTableau.ManchesterAntlr;
using IriTools;

namespace ManchesterAntlr;
using AlcTableau;

public class ConceptVisitor : ManchesterBaseVisitor<ALC.Concept>
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
    public override ALC.Concept VisitStart(ManchesterParser.StartContext context) =>
        Visit(context.GetChild(0));
    
    public override ALC.Concept VisitIriPrimary(ManchesterParser.IriPrimaryContext context)
    {
        var iri = _iriGrammarVisitor.Visit(context.rdfiri());
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
            _iriGrammarVisitor.Visit(context.rdfiri()),
            Visit(context.primary()));

    public override ALC.Concept VisitExistentialRestriction(ManchesterParser.ExistentialRestrictionContext context) =>
        ALC.Concept.NewExistential(
            _iriGrammarVisitor.Visit(context.rdfiri()),
            Visit(context.primary()));
    
    
}