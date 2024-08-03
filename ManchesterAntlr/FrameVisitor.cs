using AlcTableau.ManchesterAntlr;
using IriTools;
using Microsoft.FSharp.Collections;

namespace ManchesterAntlr;
using AlcTableau;

public class FrameVisitor : ManchesterBaseVisitor<FSharpList<ALC.TBoxAxiom>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    private AnnotatedListVisitor AnnotatedListVisitor { get; init; }
    public FrameVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
        AnnotatedListVisitor = new AnnotatedListVisitor(conceptVisitor);
    }
    public FrameVisitor()
    {
        ConceptVisitor = new ConceptVisitor();
        AnnotatedListVisitor = new AnnotatedListVisitor(ConceptVisitor);
    }

    public override FSharpList<ALC.TBoxAxiom> VisitFrame(ManchesterParser.FrameContext context)
    {
        return Visit(context.classFrame());
    }
    
    public override FSharpList<ALC.TBoxAxiom> VisitClassFrame(ManchesterParser.ClassFrameContext context)
    {
        var classIri = ConceptVisitor.IriGrammarVisitor.Visit(context.rdfiri());
        var classConcept = ALC.Concept.NewConceptName(classIri);
        var frame = context.subClassOf()
            .SelectMany(AnnotatedListVisitor.Visit)
            .Select(subclass => subclass(classConcept));
        return ListModule.OfSeq(frame);
    }
        
}