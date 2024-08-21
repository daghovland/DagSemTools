
using IriTools;
using ManchesterAntlr;
using Microsoft.FSharp.Collections;

namespace AlcTableau.ManchesterAntlr;

public class DataRangeVisitor : ManchesterBaseVisitor<AlcTableau.DataRange.Datarange>
{
    DatatypeRestrictionVisitor _datatypeRestrictionVisitor = new DatatypeRestrictionVisitor();
    public IriGrammarVisitor IriGrammarVisitor { get; init; }
    public DataRangeVisitor()
    {
        IriGrammarVisitor = new IriGrammarVisitor();
    }
    public DataRangeVisitor(IriGrammarVisitor iriGrammarVisitor)
    {
        IriGrammarVisitor = iriGrammarVisitor;
    }

    public DataRangeVisitor(Dictionary<string, IriReference> prefixes)
    {
        IriGrammarVisitor = new IriGrammarVisitor(prefixes);
    }
    public override DataRange.Datarange VisitSingleDataDisjunction(ManchesterParser.SingleDataDisjunctionContext context)
    => Visit(context.dataConjunction());

    public override DataRange.Datarange VisitSingleDataConjunction(ManchesterParser.SingleDataConjunctionContext context)
        => Visit(context.dataPrimary());

    public override DataRange.Datarange VisitPositiveDataPrimary(ManchesterParser.PositiveDataPrimaryContext context)
        => Visit(context.dataAtomic());


    public override DataRange.Datarange VisitDataTypeAtomic(ManchesterParser.DataTypeAtomicContext context)
        => Visit(context.datatype());

    public override DataRange.Datarange VisitDatatypeInteger(ManchesterParser.DatatypeIntegerContext context)
        => DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#integer");

    public override DataRange.Datarange VisitDatatypeString(ManchesterParser.DatatypeStringContext context)
        => DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#string");

    public override DataRange.Datarange VisitDatatypeDecimal(ManchesterParser.DatatypeDecimalContext context)
        => DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#decimal");

    public override DataRange.Datarange VisitDatatypeFloat(ManchesterParser.DatatypeFloatContext context)
        => DataRange.Datarange.NewDatatype("https://www.w3.org/2001/XMLSchema#float");

    public override DataRange.Datarange VisitDatatypeIri(ManchesterParser.DatatypeIriContext context)
        => DataRange.Datarange.NewDatatype(IriGrammarVisitor.Visit(context.rdfiri()));

    public override DataRange.Datarange VisitDatatypeRestriction(ManchesterParser.DatatypeRestrictionContext context)
    {
        var groundType = Visit(context.datatype());
        var restrictions = context.datatype_restriction()
            .Select<ManchesterParser.Datatype_restrictionContext, System.Tuple<DataRange.facet, string>>(_datatypeRestrictionVisitor.Visit);
        var fsharp_restriction = ListModule.OfSeq(restrictions);
        return DataRange.Datarange.NewRestriction(groundType, fsharp_restriction);
    }
}