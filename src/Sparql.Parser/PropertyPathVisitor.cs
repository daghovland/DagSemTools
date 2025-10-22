/*
 Copyright (C) 2025 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*/

using DagSemTools.Rdf;
using static DagSemTools.Rdf.Ingress;

namespace DagSemTools.Sparql.Parser;

internal class PropertyPathVisitor(TermVisitor termVisitor) : SparqlBaseVisitor<Func<Query.Term, List<Query.TriplePattern>>>
{
    private readonly PathVisitor _pathVisitor = new(termVisitor);

    public override Func<Query.Term, List<Query.TriplePattern>> VisitPropertyListPathNotEmpty(
        SparqlParser.PropertyListPathNotEmptyContext context)
        => (subject =>
            context.propertyPath()
                .Select(Visit)
                .SelectMany(f => f(subject))
                .ToList());

    public override Func<Query.Term, List<Query.TriplePattern>> VisitVerbPathObjectList(SparqlParser.VerbPathObjectListContext context)
    {
        return (subject) =>
        {
            var predicate = _pathVisitor.Visit(context.verbPath().path());
            var objs = context
                .objectListPath()
                .objectPath()
                .Select(obj => termVisitor.Visit(obj.graphNodePath().varOrTerm()));
            return objs
                .Select(obj => new Query.TriplePattern(subject, predicate, obj))
                .ToList();
        };

    }

    public override Func<Query.Term, List<Query.TriplePattern>> VisitVerbSimpleObjectList(SparqlParser.VerbSimpleObjectListContext context)
    {
        return (subject) =>
        {
            var predicate = termVisitor.Visit(context.verbSimple().var());
            var objs = context
                .objectListPath()
                .objectPath()
                .Select(obj => termVisitor.Visit(obj.graphNodePath().varOrTerm()));
            return objs
                .Select(obj => new Query.TriplePattern(subject, predicate, obj))
                .ToList();
        };

    }


}