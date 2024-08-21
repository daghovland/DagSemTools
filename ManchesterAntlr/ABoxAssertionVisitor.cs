using AlcTableau;
using IriTools;

namespace ManchesterAntlr;

public class ABoxAssertionVisitor : ManchesterBaseVisitor<Func<IriReference, ALC.ABoxAssertion>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    public ABoxAssertionVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
    }

    public override Func<IriReference, ALC.ABoxAssertion> VisitObjectAnnotation(ManchesterParser.ObjectAnnotationContext context)
    {
        var propertyIri = ConceptVisitor.IriGrammarVisitor.Visit(context.rdfiri(0));
        var value = ConceptVisitor.IriGrammarVisitor.Visit(context.rdfiri(1));
        return (individual) => ALC.ABoxAssertion.NewObjectAnnotationAssertion(individual, propertyIri, value);
    }

    public override Func<IriReference, ALC.ABoxAssertion> VisitLiteralAnnotation(ManchesterParser.LiteralAnnotationContext context)
    {
        var propertyIri = ConceptVisitor.IriGrammarVisitor.Visit(context.rdfiri());
        var value = context.literal().GetText();
        return (individual) => ALC.ABoxAssertion.NewLiteralAnnotationAssertion(individual, propertyIri, value);
    }

    public override Func<IriReference, ALC.ABoxAssertion> VisitPositiveFact(
        ManchesterParser.PositiveFactContext context) =>
        Visit(context.propertyFact());
    public override Func<IriReference, ALC.ABoxAssertion> VisitNegativeFact(
        ManchesterParser.NegativeFactContext context) =>
        individual => ALC.ABoxAssertion.NewNegativeAssertion(Visit(context.propertyFact())(individual));

    public override Func<IriReference, ALC.ABoxAssertion> VisitObjectPropertyFact(ManchesterParser.ObjectPropertyFactContext context)
    {
        var propertyIri = AlcTableau.ALC.Role.NewIri(ConceptVisitor.IriGrammarVisitor.Visit(context.role));
        var value = ConceptVisitor.IriGrammarVisitor.Visit(context.@object);
        return (individual) => ALC.ABoxAssertion.NewRoleAssertion(individual, value, propertyIri);
    }

    public override Func<IriReference, ALC.ABoxAssertion> VisitDataPropertyFact(ManchesterParser.DataPropertyFactContext context)
    {
        var propertyIri = ConceptVisitor.IriGrammarVisitor.Visit(context.property);
        var value = context.value.GetText();
        return (individual) => ALC.ABoxAssertion.NewLiteralAssertion(individual, propertyIri, value);
    }

}