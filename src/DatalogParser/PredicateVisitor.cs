using AlcTableau;
using AlcTableau.Rdf;
using AlcTableau.TurtleAntlr;

namespace DatalogAntlr;

/// <inheritdoc />
public class PredicateVisitor : DatalogBaseVisitor<Datalog.ResourceOrVariable>
{
    private ResourceVisitor _resourceVisitor;

    /// <inheritdoc />
    public PredicateVisitor(ResourceVisitor resourceVisitor)
    {
        _resourceVisitor = resourceVisitor;
    }

    /// <inheritdoc />
    public override Datalog.ResourceOrVariable VisitIri(DatalogParser.IriContext context)
    {
        var resource = _resourceVisitor.Visit(context);
        return Datalog.ResourceOrVariable.NewResource(resource);
    }

    /// <inheritdoc />
    public override Datalog.ResourceOrVariable VisitLiteral(DatalogParser.LiteralContext context)
    {
        return Datalog.ResourceOrVariable.NewResource(_resourceVisitor.Visit(context));
    }

    /// <inheritdoc />
    public override Datalog.ResourceOrVariable VisitVariable(DatalogParser.VariableContext context)
    {
        return Datalog.ResourceOrVariable.NewVariable(context.GetText());
    }
}