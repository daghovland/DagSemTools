using System.Collections;
using AlcTableau.ManchesterAntlr;
using IriTools;
using Microsoft.FSharp.Collections;

namespace ManchesterAntlr;
using AlcTableau;

public class FrameVisitor : ManchesterBaseVisitor<(List<ALC.TBoxAxiom>, List<ALC.ABoxAssertion>)>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    private ClassAssertionVisitor ClassAssertionVisitor { get; init; }
    private IndividualAssertionVisitor IndividualAssertionVisitor { get; init; }

    public FrameVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
        ClassAssertionVisitor = new ClassAssertionVisitor(conceptVisitor);
        IndividualAssertionVisitor = new IndividualAssertionVisitor(conceptVisitor);
    }
    
    public override (List<ALC.TBoxAxiom>, List<ALC.ABoxAssertion>) VisitClassFrame(ManchesterParser.ClassFrameContext context)
    {
        var classIri = ConceptVisitor.IriGrammarVisitor.Visit(context.rdfiri());
        var classConcept = ALC.Concept.NewConceptName(classIri);
        var frame = context.annotatedList()
            .SelectMany(ClassAssertionVisitor.Visit)
            .Select(classProperty => classProperty(classConcept))
            .ToList();
        return (frame, []);
    }
    
    public override (List<ALC.TBoxAxiom>, List<ALC.ABoxAssertion>) VisitIndividualFrame(ManchesterParser.IndividualFrameContext context)
    {
        var individualIri = ConceptVisitor.IriGrammarVisitor.Visit(context.rdfiri());
        var frame = context.individualFrameList()
            .SelectMany(IndividualAssertionVisitor.Visit)
            .Select(assertion => assertion(individualIri))
            .ToList();
        return ([], frame);
    }
        
}