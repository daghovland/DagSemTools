using AlcTableau.ManchesterAntlr;
using IriTools;
using Microsoft.FSharp.Collections;

namespace ManchesterAntlr;
using AlcTableau;

public class ClassAssertionVisitor : ManchesterBaseVisitor<IEnumerable<Func<ALC.Concept, ALC.TBoxAxiom>>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    public ClassAssertionVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
    }
    
    public override IEnumerable<Func<ALC.Concept, ALC.TBoxAxiom>> VisitSubClassOf(ManchesterParser.SubClassOfContext context)
    =>
        context.descriptionAnnotatedList().description().
            Select(ConceptVisitor.Visit)
            .Select<ALC.Concept, Func<ALC.Concept, ALC.TBoxAxiom>>(
                super => ( 
                    (ALC.Concept sub) => ALC.TBoxAxiom.NewInclusion(sub, super)));
    
    public override IEnumerable<Func<ALC.Concept, ALC.TBoxAxiom>> VisitEquivalentTo(ManchesterParser.EquivalentToContext context)
        =>
            context.descriptionAnnotatedList().description().
                Select(ConceptVisitor.Visit)
                .Select<ALC.Concept, Func<ALC.Concept, ALC.TBoxAxiom>>(
                    c => ( 
                        (ALC.Concept frameClass) => ALC.TBoxAxiom.NewEquivalence(frameClass, c)));

}