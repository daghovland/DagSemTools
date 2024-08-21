using AlcTableau;

namespace ManchesterAntlr;

public class DatatypeRestrictionVisitor : ManchesterBaseVisitor<System.Tuple<DataRange.facet, string>>
{
    private FacetVisitor _facetVisitor = new FacetVisitor();
    public override Tuple<DataRange.facet, string> VisitDatatype_restriction(ManchesterParser.Datatype_restrictionContext context)
        => new(_facetVisitor.Visit(context.facet()), context.literal().GetText());

}