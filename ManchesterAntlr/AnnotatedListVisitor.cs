using AlcTableau.ManchesterAntlr;
using IriTools;
using Microsoft.FSharp.Collections;

namespace ManchesterAntlr;
using AlcTableau;

public class AnnotatedListVisitor : ManchesterBaseVisitor<IEnumerable<Func<ALC.Concept, ALC.TBoxAxiom>>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    public AnnotatedListVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
    }
    
    public override IEnumerable<Func<ALC.Concept, ALC.TBoxAxiom>> VisitSubClassOf(ManchesterParser.SubClassOfContext context)
    =>
        context.description().
            Select(ConceptVisitor.Visit)
            .Select<ALC.Concept, Func<ALC.Concept, ALC.TBoxAxiom>>(
                c => ( 
                    (ALC.Concept super) => ALC.TBoxAxiom.NewInclusion(c, super)));
    
    public override IEnumerable<Func<ALC.Concept, ALC.TBoxAxiom>> VisitEquivalentTo(ManchesterParser.EquivalentToContext context)
        =>
            context.description().
                Select(ConceptVisitor.Visit)
                .Select<ALC.Concept, Func<ALC.Concept, ALC.TBoxAxiom>>(
                    c => ( 
                        (ALC.Concept super) => ALC.TBoxAxiom.NewEquivalence(c, super)));

}