using DagSemTools;
using DagSemTools.TurtleAntlr;
using IriTools;

namespace DagSemTools.Datalog.Parser;

/// <inheritdoc />
public class RuleAtomVisitor : DatalogBaseVisitor<Datalog.RuleAtom>
{
    private readonly PredicateVisitor _predicateVisitor;
    internal TriplePatternVisitor TriplePatternVisitor { get; }
    /// <inheritdoc />
    public RuleAtomVisitor(PredicateVisitor predicateVisitor)
    {
        _predicateVisitor = predicateVisitor;
        TriplePatternVisitor = new TriplePatternVisitor(predicateVisitor);
    }



    /// <summary>
    /// Visit a rule atom that does not have NOT in front of it
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Datalog.RuleAtom VisitYesRuleAtom(DatalogParser.YesRuleAtomContext context)
    {
        return Datalog.RuleAtom.NewPositiveTriple(TriplePatternVisitor.Visit(context.positiveRuleAtom()));
    }

    /// <summary>
    /// Visit a rule atom that has NOT in front of it
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Datalog.RuleAtom VisitNegativeRuleAtom(DatalogParser.NegativeRuleAtomContext context)
    {
        return Datalog.RuleAtom.NewNotTriple(TriplePatternVisitor.Visit(context.positiveRuleAtom()));
    }

}