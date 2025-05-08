/*
 Copyright (C) 2024 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*/

using DagSemTools.Ingress;
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

        Term predicateResource = _predicateVisitor.Visit(predicate) ??
                                 throw new Exception($"Predicate is null  at line {context.Start.Line}, position {context.Start.Column}");
        Term subjectResource = _predicateVisitor.Visit(subject) ??
                               throw new Exception($"Subject is null  at line {context.Start.Line}, position {context.Start.Column}");
        Term objectResource = _predicateVisitor.Visit(@object) ??
                              throw new Exception($"Object is null at line {context.Start.Line}, position {context.Start.Column}"); ;
        return new TriplePattern(
            subjectResource,
            predicateResource,
            objectResource
        );
    }

    /// <inheritdoc />
    public override TriplePattern VisitTypeAtom(DatalogParser.TypeAtomContext context)
    {
        var subject = context.term();
        var predicate = Term
            .NewResource(_predicateVisitor.ResourceVisitor.Datastore
                .AddNodeResource(RdfResource
                    .NewIri(new IriReference(Namespaces.RdfType))));
        var @class = context.relation();

        return new TriplePattern(
                _predicateVisitor.Visit(subject),
                predicate,
                _predicateVisitor.Visit(@class)
            );
    }


}