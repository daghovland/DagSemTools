using DagSemTools;
using DagSemTools.Rdf;
using DagSemTools.TurtleAntlr;

namespace DagSemTools.Datalog.Parser;

/// <inheritdoc />
public class PredicateVisitor : DatalogBaseVisitor<Datalog.ResourceOrVariable>
{
    internal ResourceVisitor ResourceVisitor { get; }

    /// <inheritdoc />
    public PredicateVisitor(ResourceVisitor resourceVisitor)
    {
        ResourceVisitor = resourceVisitor;
    }

    /// <inheritdoc />
    public override Datalog.ResourceOrVariable VisitIri(DatalogParser.IriContext context)
    {
        var resource = ResourceVisitor.Visit(context);
        return Datalog.ResourceOrVariable.NewResource(resource);
    }

    /// <inheritdoc />
    public override Datalog.ResourceOrVariable VisitLiteral(DatalogParser.LiteralContext context)
    {
        return Datalog.ResourceOrVariable.NewResource(ResourceVisitor.Visit(context));
    }

    /// <inheritdoc />
    public override Datalog.ResourceOrVariable VisitVariable(DatalogParser.VariableContext context)
    {
        return Datalog.ResourceOrVariable.NewVariable(context.GetText());
    }
}