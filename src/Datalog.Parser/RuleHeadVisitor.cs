using DagSemTools;
using DagSemTools.Datalog.Parser;
using IriTools;

namespace DagSemTools.Datalog.Parser;

internal class RuleHeadVisitor : DatalogBaseVisitor<RuleHead>
{
    internal TriplePatternVisitor TriplePatternVisitor { get; }
    internal RuleHeadVisitor(PredicateVisitor predicateVisitor)
    {
        TriplePatternVisitor = new TriplePatternVisitor(predicateVisitor);
    }

    public override RuleHead VisitContradictionHead(DatalogParser.ContradictionHeadContext context)
    {
        return RuleHead.Contradiction;
    }
    public override RuleHead VisitNormalRuleHead(DatalogParser.NormalRuleHeadContext context)
    {
        var triplePattern = TriplePatternVisitor.Visit(context) ??
                            throw new Exception($"Head is missing in proper rule  at line {context.Start.Line}, position {context.Start.Column}");
        return RuleHead.NewNormalHead(triplePattern);
    }


}