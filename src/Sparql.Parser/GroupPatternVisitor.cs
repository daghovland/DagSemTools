/*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.Rdf;
using Microsoft.FSharp.Collections;
using DagSemTools.Parser;

namespace DagSemTools.Sparql.Parser;

internal class GroupPatternVisitor(TermVisitor termVisitor) : SparqlBaseVisitor<IEnumerable<Query.QueryComponent>>
{
    private readonly PropertyPathVisitor _propertyPathVisitor = new(termVisitor);
    private readonly ExpressionVisitor _expressionVisitor = new(termVisitor);

    public override IEnumerable<Query.QueryComponent> VisitGroupGraphPattern(
        SparqlParser.GroupGraphPatternContext context)
    {
        if (context.subSelect() is { } sub)
            return Visit(sub);
        return Visit(context.groupGraphPatternSub());
    }

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
        if (context.groupOrUnionGraphPattern() != null)
            return Visit(context.groupOrUnionGraphPattern());
        if (context.filter() != null)
            return Visit(context.filter());
        if (context.minusGraphPattern() != null)
            return Visit(context.minusGraphPattern());
        if (context.bind() != null)
            return Visit(context.bind());
        if (context.inlineData() != null)
            return Visit(context.inlineData());
        if (context.graphGraphPattern() != null)
            return Visit(context.graphGraphPattern());
        throw new NotImplementedException($"GraphPatternNotTriples is not yet fully parsed. {context.GetText()} is not supported. Sorry");
    }

    public override IEnumerable<Query.QueryComponent> VisitOptionalGraphPattern(SparqlParser.OptionalGraphPatternContext context)
    {
        var group = Visit(context.groupGraphPattern());
        return new[] { Query.QueryComponent.NewOptional(Query.OptionalPattern.NewOptional(ListModule.OfSeq(group))) };
    }

    public override IEnumerable<Query.QueryComponent> VisitGroupOrUnionGraphPattern(SparqlParser.GroupOrUnionGraphPatternContext context)
    {
        var groups = context.groupGraphPattern()
            .Select(g => ListModule.OfSeq(Visit(g)))
            .ToList();
        if (groups.Count == 1)
            return groups[0];
        return new[] { Query.QueryComponent.NewUnion(ListModule.OfSeq(groups)) };
    }

    public override IEnumerable<Query.QueryComponent> VisitFilter(SparqlParser.FilterContext context)
    {
        var expr = _expressionVisitor.Visit(context.constraint());
        return new[] { Query.QueryComponent.NewFilter(expr) };
    }

    public override IEnumerable<Query.QueryComponent> VisitMinusGraphPattern(SparqlParser.MinusGraphPatternContext context)
    {
        var group = ListModule.OfSeq(Visit(context.groupGraphPattern()));
        return new[] { Query.QueryComponent.NewMinus(group) };
    }

    public override IEnumerable<Query.QueryComponent> VisitBind(SparqlParser.BindContext context)
    {
        var expr = _expressionVisitor.Visit(context.expression());
        var varName = ParserUtils.GetVariableName(context.var().GetText());
        return new[] { Query.QueryComponent.NewBind(expr, varName) };
    }

    public override IEnumerable<Query.QueryComponent> VisitInlineData(SparqlParser.InlineDataContext context)
    {
        var dataBlock = context.dataBlock();
        if (dataBlock.inlineDataOneVar() is { } oneVar)
        {
            var varName = ParserUtils.GetVariableName(oneVar.var().GetText());
            var vars = new List<string> { varName };
            var rows = oneVar.dataBlockValue()
                .Select(dbv => new List<Query.Term> { ParseDataBlockValue(dbv) })
                .Select(r => ListModule.OfSeq(r))
                .ToList();
            return new[] { Query.QueryComponent.NewValues(ListModule.OfSeq(vars), ListModule.OfSeq(rows)) };
        }
        if (dataBlock.inlineDataFull() is { } full)
        {
            var vars = full.var().Select(v => ParserUtils.GetVariableName(v.GetText())).ToList();
            // Each row is a parenthesized group of dataBlockValues
            var rows = new List<Microsoft.FSharp.Collections.FSharpList<Query.Term>>();
            // Children: NIL or '(' var* ')' then '{' ( '(' dataBlockValue* ')' | NIL )* '}'
            // We use the grammar structure: the row groups are inside braces
            // Parse by collecting consecutive dataBlockValue sequences from children
            var allDbvGroups = new List<List<Query.Term>>();
            for (int i = 0; i < full.ChildCount; i++)
            {
                var child = full.GetChild(i);
                if (child.GetText() == "(")
                {
                    var row = new List<Query.Term>();
                    i++;
                    while (i < full.ChildCount && full.GetChild(i).GetText() != ")")
                    {
                        if (full.GetChild(i) is SparqlParser.DataBlockValueContext dbv)
                            row.Add(ParseDataBlockValue(dbv));
                        i++;
                    }
                    allDbvGroups.Add(row);
                }
            }
            // Skip first group which is the variable list; use remaining groups as rows
            // Actually the var list uses var() not dataBlockValue, so allDbvGroups are all data rows
            return new[] { Query.QueryComponent.NewValues(ListModule.OfSeq(vars), ListModule.OfSeq(allDbvGroups.Select(ListModule.OfSeq))) };
        }
        return Array.Empty<Query.QueryComponent>();
    }

    private Query.Term ParseDataBlockValue(SparqlParser.DataBlockValueContext context)
    {
        if (context.iri() != null) return termVisitor.Visit(context.iri());
        if (context.rdfLiteral() != null) return termVisitor.Visit(context.rdfLiteral());
        if (context.numericLiteral() != null) return termVisitor.Visit(context.numericLiteral());
        if (context.booleanLiteral() != null) return termVisitor.Visit(context.booleanLiteral());
        // UNDEF maps to a special unbound variable placeholder
        return Query.Term.NewVariable("__UNDEF__");
    }

    public override IEnumerable<Query.QueryComponent> VisitGraphGraphPattern(SparqlParser.GraphGraphPatternContext context)
    {
        // Treat named graph patterns as plain groups for now
        return Visit(context.groupGraphPattern());
    }

    public override IEnumerable<Query.QueryComponent> VisitSubSelect(SparqlParser.SubSelectContext context)
    {
        var parsedVars = context.selectClause().projection()
            .Select(v => new ProjectionVisitor(termVisitor).Visit(v))
            .ToList();
        var parsedWhereClause = Visit(context.whereClause().groupGraphPattern());
        var solutionModifier = context.solutionModifier();
        var groupBy = new List<Query.Expression>();
        if (solutionModifier.groupClause() != null)
        {
            foreach (var groupCondition in solutionModifier.groupClause().groupCondition())
            {
                if (groupCondition.var() != null)
                    groupBy.Add(Query.Expression.NewExprVariable(ParserUtils.GetVariableName(groupCondition.var().GetText())));
                else if (groupCondition.expression() != null)
                    groupBy.Add(_expressionVisitor.Visit(groupCondition.expression()));
            }
        }
        var subQuery = new Query.SelectQuery(ListModule.OfSeq(parsedVars), ListModule.OfSeq(parsedWhereClause), ListModule.OfSeq(groupBy));
        return new[] { Query.QueryComponent.NewSubquery(subQuery) };
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