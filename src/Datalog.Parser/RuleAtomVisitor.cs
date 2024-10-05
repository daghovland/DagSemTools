using DagSemTools;
using DagSemTools.TurtleAntlr;
using IriTools;

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
    public override Datalog.RuleAtom VisitTypeAtom(DatalogParser.TypeAtomContext context)
    {
        var subject = context.term();
        var predicate = Datalog.ResourceOrVariable
            .NewResource(_predicateVisitor.ResourceVisitor.Datastore
                .AddResource(Rdf.Ingress.Resource
                    .NewIri(new IriReference(Namespaces.RdfType))));
        var @class = context.predicate();

        return Datalog.RuleAtom.NewPositiveTriple(
            new Datalog.TriplePattern(
                _predicateVisitor.Visit(subject),
                predicate,
                _predicateVisitor.Visit(@class)
            )
        );
    }

    public override Datalog.RuleAtom VisitNegativeRuleAtom(DatalogParser.NegativeRuleAtomContext context)
    {
        return Datalog.RuleAtom.NewNotTriple(context.GetChild(0).Accept(this));
    }

    /// <inheritdoc />
    public override Datalog.RuleAtom VisitTripleAtom(DatalogParser.TripleAtomContext context)
    {
        var subject = context.term(0);
        var predicate = context.predicate();
        var @object = context.term(1);

        return Datalog.RuleAtom.NewPositiveTriple(
            new Datalog.TriplePattern(
                _predicateVisitor.Visit(subject),
                _predicateVisitor.Visit(predicate),
                _predicateVisitor.Visit(@object)
            )
        );
    }
}