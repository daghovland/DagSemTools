using AlcTableau;

namespace ManchesterAntlr;

public class FacetVisitor : ManchesterBaseVisitor<DataRange.facet>
{
    public override DataRange.facet VisitFacetLength(ManchesterParser.FacetLengthContext context) => DataRange.facet.Length;
    public override DataRange.facet VisitFacetMinLength(ManchesterParser.FacetMinLengthContext context) => DataRange.facet.MinLength;
    public override DataRange.facet VisitFacetMaxLength(ManchesterParser.FacetMaxLengthContext context) => DataRange.facet.MaxLength;

    public override DataRange.facet VisitFacetLangRange(ManchesterParser.FacetLangRangeContext context) =>
        DataRange.facet.LangRange;
    public override DataRange.facet VisitFacetPattern(ManchesterParser.FacetPatternContext context) => DataRange.facet.Pattern;
    public override DataRange.facet VisitFacetGreaterThan(ManchesterParser.FacetGreaterThanContext context) => DataRange.facet.GreaterThan;
    public override DataRange.facet VisitFacetLessThan(ManchesterParser.FacetLessThanContext context) => DataRange.facet.LessThan;
    public override DataRange.facet VisitFacetGreaterThanEqual(ManchesterParser.FacetGreaterThanEqualContext context) => DataRange.facet.GreaterThanOrEqual;
    public override DataRange.facet VisitFacetLessThanEqual(ManchesterParser.FacetLessThanEqualContext context) => DataRange.facet.LessThanOrEqual;

}