using DagSemTools.TurtleAntlr;
using IriTools;

namespace DagSemTools.Datalog.Parser;

internal class TriplePatternVisitor : DatalogBaseVisitor<Datalog.TriplePattern>
{
    private readonly PredicateVisitor _predicateVisitor;

    internal TriplePatternVisitor(PredicateVisitor predicateVisitor)
    {
        _predicateVisitor = predicateVisitor;
    }

    /// <summary>
    /// Visit a triple atom. Eitheor of the form [subject, predicate, object] or predicat [subject, object]
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Datalog.TriplePattern VisitTripleAtom(DatalogParser.TripleAtomContext context)
    {
        var subject = context.term(0);
        var predicate = context.predicate();
        var @object = context.term(1);

        return new Datalog.TriplePattern(
            _predicateVisitor.Visit(subject),
            _predicateVisitor.Visit(predicate),
            _predicateVisitor.Visit(@object)
        );
    }

    /// <inheritdoc />
    public override Datalog.TriplePattern VisitTypeAtom(DatalogParser.TypeAtomContext context)
    {
        var subject = context.term();
        var predicate = Datalog.ResourceOrVariable
            .NewResource(_predicateVisitor.ResourceVisitor.Datastore
                .AddResource(Rdf.Ingress.Resource
                    .NewIri(new IriReference(Namespaces.RdfType))));
        var @class = context.predicate();

        return new Datalog.TriplePattern(
                _predicateVisitor.Visit(subject),
                predicate,
                _predicateVisitor.Visit(@class)
            );
    }


}