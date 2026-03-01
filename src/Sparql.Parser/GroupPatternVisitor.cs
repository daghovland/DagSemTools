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

internal class GroupPatternVisitor(TermVisitor termVisitor) : SparqlBaseVisitor<IEnumerable<Query.QueryComponent>>
{
    private readonly PropertyPathVisitor _propertyPathVisitor = new(termVisitor);

    public override IEnumerable<Query.QueryComponent> VisitGroupGraphPattern(
        SparqlParser.GroupGraphPatternContext context)
        => Visit(context.groupGraphPatternSub());

    public override IEnumerable<Query.QueryComponent> VisitGroupGraphPatternSub(SparqlParser.GroupGraphPatternSubContext context)
    {
        var components = new List<Query.QueryComponent>();
        if (context.triplesBlock(0) != null)
        {
            components.AddRange(Visit(context.triplesBlock(0)));
        }

        for (int i = 0; i < context.graphPatternNotTriples().Length; i++)
        {
            components.AddRange(Visit(context.graphPatternNotTriples(i)));
            if (context.triplesBlock(i + 1) != null)
            {
                components.AddRange(Visit(context.triplesBlock(i + 1)));
            }
        }

        return components;
    }

    public override IEnumerable<Query.QueryComponent> VisitGraphPatternNotTriples(SparqlParser.GraphPatternNotTriplesContext context)
    {
        if (context.optionalGraphPattern() != null)
            return Visit(context.optionalGraphPattern());
        throw new NotImplementedException($"GraphPatternNotTriples is not yet fully parsed. {context.GetText()} is not supported. Sorry");
    }

    public override IEnumerable<Query.QueryComponent> VisitOptionalGraphPattern(SparqlParser.OptionalGraphPatternContext context)
    {
        var group = Visit(context.groupGraphPattern());
        return new[] { Query.QueryComponent.NewOptional(Query.OptionalPattern.NewOptional(ListModule.OfSeq(group))) };
    }

    public override IEnumerable<Query.QueryComponent> VisitTriplesBlock(SparqlParser.TriplesBlockContext context)
    {
        var triplePatterns = Visit(context.triplesSameSubjectPath());
        if (triplePatterns == null)
            throw new NotImplementedException("TriplesSameSubjectPath is not yet parsed. Sorry");
        if (context.triplesBlock() is not null)
            return triplePatterns.Concat(Visit(context.triplesBlock()));
        return triplePatterns.ToList();
    }

    public override IEnumerable<Query.QueryComponent> VisitNamedSubjectTriplesPath(SparqlParser.NamedSubjectTriplesPathContext context)
    {
        var subject = termVisitor.Visit(context.varOrTerm());
        var propPath = _propertyPathVisitor.Visit(context.propertyListPathNotEmpty());
        if (subject == null || propPath == null)
            throw new NotImplementedException("VarOrTerm or propertyPath is not properly parsed. This is a bug in the parser. Sorry");
        return propPath(subject).Select(qc => Query.QueryComponent.NewPattern(qc));
    }
}