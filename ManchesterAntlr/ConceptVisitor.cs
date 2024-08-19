using AlcTableau.ManchesterAntlr;
using IriTools;

namespace ManchesterAntlr;
using AlcTableau;

public class ConceptVisitor : ManchesterBaseVisitor<ALC.Concept>
{
    public IriGrammarVisitor IriGrammarVisitor { get; init; }
    private RoleVisitor _roleVisitor;
    public ConceptVisitor()
    : this(new IriGrammarVisitor())
    { }
    public ConceptVisitor(IriGrammarVisitor iriGrammarVisitor)
    {
        IriGrammarVisitor = iriGrammarVisitor;
        _roleVisitor = new RoleVisitor(IriGrammarVisitor);
    }
    

    public ConceptVisitor(Dictionary<string, IriReference> prefixes)
    : this(new IriGrammarVisitor(prefixes))
    {}
    
    public override ALC.Concept VisitIriPrimaryConcept(ManchesterParser.IriPrimaryConceptContext context)
    {
        var iri = IriGrammarVisitor.Visit(context.rdfiri());
        return ALC.Concept.NewConceptName(iri);
    }

    public override ALC.Concept VisitParenthesizedPrimaryConcept(ManchesterParser.ParenthesizedPrimaryConceptContext context) =>
        Visit(context.description());
    public override ALC.Concept VisitNegatedPrimaryConcept(ManchesterParser.NegatedPrimaryConceptContext context) =>
        ALC.Concept.NewNegation(Visit(context.primary()));

    public override ALC.Concept VisitConceptDisjunction(ManchesterParser.ConceptDisjunctionContext context) =>
        ALC.Concept.NewDisjunction(Visit(context.description()), Visit(context.conjunction()));
    
    public override ALC.Concept VisitConceptSingleDisjunction(ManchesterParser.ConceptSingleDisjunctionContext context) =>
        Visit(context.conjunction());

    public override ALC.Concept VisitConceptConjunction(ManchesterParser.ConceptConjunctionContext context)
    {
        var conjunction = Visit(context.conjunction());
        var primary = Visit(context.primary());
        return ALC.Concept.NewConjunction(conjunction, primary);
    }
    public override ALC.Concept VisitConceptSingleConjunction(ManchesterParser.ConceptSingleConjunctionContext context) =>
        Visit(context.primary());
    
    public override ALC.Concept VisitUniversalConceptRestriction(ManchesterParser.UniversalConceptRestrictionContext context) =>
        ALC.Concept.NewUniversal(
            _roleVisitor.Visit(context.objectPropertyExpression()),
            Visit(context.primary()));

    public override ALC.Concept VisitExistentialConceptRestriction(ManchesterParser.ExistentialConceptRestrictionContext context) =>
        ALC.Concept.NewExistential(
            _roleVisitor.Visit(context.objectPropertyExpression()),
            Visit(context.primary()));
    
}