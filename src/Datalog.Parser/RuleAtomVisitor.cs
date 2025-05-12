using DagSemTools;
using DagSemTools.Datalog.Parser;
using IriTools;

namespace DagSemTools.Datalog.Parser;

internal class RuleAtomVisitor : DatalogBaseVisitor<RuleAtom>
{
    internal TriplePatternVisitor TriplePatternVisitor { get; }
    internal RuleAtomVisitor(PredicateVisitor predicateVisitor)
    {
        TriplePatternVisitor = new TriplePatternVisitor(predicateVisitor);
    }

    public override RuleAtom VisitYesRuleAtom(DatalogParser.YesRuleAtomContext context)
    {
        return RuleAtom.NewPositiveTriple(TriplePatternVisitor.Visit(context.positiveRuleAtom()));
    }
    public override RuleAtom VisitNegativeRuleAtom(DatalogParser.NegativeRuleAtomContext context)
    {
        return RuleAtom.NewNotTriple(TriplePatternVisitor.Visit(context.positiveRuleAtom()));
    }

}