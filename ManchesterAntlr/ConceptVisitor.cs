namespace ManchesterAntlr;
using AlcTableau;

public class ConceptVisitor : ConceptBaseVisitor<ALC.Concept>
{
    public override ALC.Concept VisitIriPrimary(ConceptParser.IriPrimaryContext context)
    {
        return base.VisitIriPrimary(context);
    }
}