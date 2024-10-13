using DagSemTools;
using DagSemTools.Datalog.Parser;
using IriTools;

namespace DagSemTools.Datalog.Parser;

/// <inheritdoc />
internal class RuleAtomVisitor : DatalogBaseVisitor<RuleAtom>
{
    internal TriplePatternVisitor TriplePatternVisitor { get; }
    /// <inheritdoc />
    internal RuleAtomVisitor(PredicateVisitor predicateVisitor)
    {
        TriplePatternVisitor = new TriplePatternVisitor(predicateVisitor);
    }



    /// <summary>
    /// Visit a rule atom that does not have NOT in front of it
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override RuleAtom VisitYesRuleAtom(DatalogParser.YesRuleAtomContext context)
    {
        return RuleAtom.NewPositiveTriple(TriplePatternVisitor.Visit(context.positiveRuleAtom()));
    }

    /// <summary>
    /// Visit a rule atom that has NOT in front of it
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override RuleAtom VisitNegativeRuleAtom(DatalogParser.NegativeRuleAtomContext context)
    {
        return RuleAtom.NewNotTriple(TriplePatternVisitor.Visit(context.positiveRuleAtom()));
    }

}