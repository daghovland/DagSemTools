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
    private ObjectPropertyAssertionVisitor ObjectPropertyAssertionVisitor { get; init; }

    public FrameVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
        ClassAssertionVisitor = new ClassAssertionVisitor(conceptVisitor);
        IndividualAssertionVisitor = new IndividualAssertionVisitor(conceptVisitor);
        ObjectPropertyAssertionVisitor = new ObjectPropertyAssertionVisitor(conceptVisitor);
    }


    public override (List<ALC.TBoxAxiom>, List<ALC.ABoxAssertion>) VisitObjectPropertyFrame(ManchesterParser.ObjectPropertyFrameContext context)
    {
        var objectPropertyIri = ConceptVisitor.IriGrammarVisitor.Visit(context.rdfiri());
        var frame = context.objectPropertyFrameList()
            .SelectMany(ObjectPropertyAssertionVisitor.Visit)
            .Select(classProperty => classProperty(objectPropertyIri))
            .ToList();
        return (frame, []);
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
        var frameList = context.individualFrameList() ??
                        throw new Exception($"Lacking individual fram list on individual {context.rdfiri().GetText()}");
        var frame = frameList
            .SelectMany(IndividualAssertionVisitor.Visit)
            .Select(assertion => assertion(individualIri))
            .ToList();
        return ([], frame);
    }

}