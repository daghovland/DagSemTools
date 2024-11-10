using DagSemTools.Datalog.Parser;
using IriTools;

namespace DagSemTools.Datalog.Parser;

internal class TriplePatternVisitor : DatalogBaseVisitor<TriplePattern>
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
    public override TriplePattern VisitTripleAtom(DatalogParser.TripleAtomContext context)
    {
        var subject = context.term(0);
        var predicate = context.relation();
        var @object = context.term(1);

        return new TriplePattern(
            _predicateVisitor.Visit(subject),
            _predicateVisitor.Visit(predicate),
            _predicateVisitor.Visit(@object)
        );
    }

    /// <inheritdoc />
    public override TriplePattern VisitTypeAtom(DatalogParser.TypeAtomContext context)
    {
        var subject = context.term();
        var predicate = ResourceOrVariable
            .NewResource(_predicateVisitor.ResourceVisitor.Datastore
                .AddResource(Rdf.Ingress.Resource
                    .NewIri(new IriReference(Rdf.Namespaces.RdfType))));
        var @class = context.relation();

        return new TriplePattern(
                _predicateVisitor.Visit(subject),
                predicate,
                _predicateVisitor.Visit(@class)
            );
    }


}