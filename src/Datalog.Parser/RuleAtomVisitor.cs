using DagSemTools;
using DagSemTools.TurtleAntlr;

namespace DagSemTools.Datalog.Parser;

/// <inheritdoc />
public class RuleAtomVisitor : DatalogBaseVisitor<Datalog.RuleAtom>
{
    private readonly PredicateVisitor _predicateVisitor;

    /// <inheritdoc />
    public RuleAtomVisitor(PredicateVisitor predicateVisitor)
    {
        _predicateVisitor = predicateVisitor;
    }

    /// <inheritdoc />
    public override Datalog.RuleAtom VisitRuleAtom(DatalogParser.RuleAtomContext context)
    {
        var subject = context.resource(0);
        var predicate = context.resource(1);
        var @object = context.resource(2);

        return Datalog.RuleAtom.NewPositiveTriple(
            new Datalog.TriplePattern(
                _predicateVisitor.Visit(subject),
                _predicateVisitor.Visit(predicate),
                _predicateVisitor.Visit(@object)
            )
        );
    }
}