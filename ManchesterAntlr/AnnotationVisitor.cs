using AlcTableau;
using IriTools;

namespace ManchesterAntlr;

public class AnnotationVisitor : ManchesterBaseVisitor<Func<IriReference, ALC.ABoxAssertion>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    public AnnotationVisitor(ConceptVisitor conceptVisitor)
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
        var value = context.STRING().GetText();
        return (individual) => ALC.ABoxAssertion.NewLiteralAnnotationAssertion(individual, propertyIri, value);
    }
    
}