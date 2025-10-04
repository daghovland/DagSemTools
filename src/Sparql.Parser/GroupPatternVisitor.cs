/*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.Rdf;
using Microsoft.FSharp.Collections;

namespace DagSemTools.Sparql.Parser;

internal class GroupPatternVisitor(TermVisitor termVisitor) : SparqlBaseVisitor<IEnumerable<Query.TriplePattern>>
{
    private readonly PropertyPathVisitor _propertyPathVisitor = new(termVisitor);

    public override IEnumerable<Query.TriplePattern> VisitGroupGraphPattern(
        SparqlParser.GroupGraphPatternContext context)
        => Visit(context.groupGraphPatternSub());

    public override IEnumerable<Query.TriplePattern> VisitGroupGraphPatternSub(SparqlParser.GroupGraphPatternSubContext context)
    {
        return context
            .triplesBlock()
            .SelectMany(Visit)
            .ToList();
    }

    public override IEnumerable<Query.TriplePattern> VisitTriplesBlock(SparqlParser.TriplesBlockContext context)
    {
        var triplePatterns = Visit(context.triplesSameSubjectPath());
        if (context.triplesBlock() is not null)
            return triplePatterns.Concat(Visit(context.triplesBlock()));
        return triplePatterns.ToList();
    }

    public override IEnumerable<Query.TriplePattern> VisitNamedSubjectTriplesPath(SparqlParser.NamedSubjectTriplesPathContext context)
    {
        var subject = termVisitor.Visit(context.varOrTerm());
        return context
            .propertyListPathNotEmpty()
            .propertyPath()
            .Select(_propertyPathVisitor.Visit)
            .SelectMany(f => f(subject))
            .ToList();

    }
}