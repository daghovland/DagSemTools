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

        var predicateResource = _predicateVisitor.Visit(predicate);
        var subjectResource = _predicateVisitor.Visit(subject);
        var objectResource = _predicateVisitor.Visit(@object);
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
        var predicate = ResourceOrVariable
            .NewResource(_predicateVisitor.ResourceVisitor.Datastore
                .AddResource(Resource
                    .NewIri(new IriReference(Namespaces.RdfType))));
        var @class = context.relation();

        return new TriplePattern(
                _predicateVisitor.Visit(subject),
                predicate,
                _predicateVisitor.Visit(@class)
            );
    }


}