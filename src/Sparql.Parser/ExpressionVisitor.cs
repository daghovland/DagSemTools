/*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using Antlr4.Runtime;
using DagSemTools.Rdf;
using DagSemTools.Parser;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace DagSemTools.Sparql.Parser;

internal class ExpressionVisitor(TermVisitor termVisitor) : SparqlBaseVisitor<Query.Expression>
{
    private readonly StringVisitor _stringVisitor = new();

    public override Query.Expression VisitRdfLiteralPrimaryExpression(
        SparqlParser.RdfLiteralPrimaryExpressionContext context) =>
            Query.Expression.NewExprTerm(termVisitor.Visit(context.rdfLiteral()));

    // Handles brackettedExpression used directly in constraint (FILTER, HAVING, ORDER BY).
    public override Query.Expression VisitBrackettedExpression(
        SparqlParser.BrackettedExpressionContext context) =>
            Visit(context.expression());

    // Handles the labeled alternative in primaryExpression.
    public override Query.Expression VisitBracketedPrimaryExpression(
        SparqlParser.BracketedPrimaryExpressionContext context) =>
            Visit(context.brackettedExpression());

    public override Query.Expression VisitNumericLiteralPrimaryExpression(
        SparqlParser.NumericLiteralPrimaryExpressionContext context) =>
        Query.Expression.NewExprTerm(termVisitor.Visit(context.numericLiteral()));

    public override Query.Expression VisitBooleanLiteralPrimaryExpression(
        SparqlParser.BooleanLiteralPrimaryExpressionContext context) =>
        Query.Expression.NewExprTerm(termVisitor.Visit(context.booleanLiteral()));

    public override Query.Expression VisitVariablePrimaryExpression(
        SparqlParser.VariablePrimaryExpressionContext context) =>
        Query.Expression.NewExprTerm(termVisitor.Visit(context.var()));

    // Delegates to VisitBuiltInCall; the aggregate sub-rule is handled via labeled alternatives.
    public override Query.Expression VisitBuiltInCallPrimaryExpression(
        SparqlParser.BuiltInCallPrimaryExpressionContext context) =>
        Visit(context.builtInCall());

    // One dispatch check is unavoidable here since builtInCall has 40+ unlabeled alternatives.
    public override Query.Expression VisitBuiltInCall(SparqlParser.BuiltInCallContext context)
    {
        if (context.aggregate() is { } agg)
            return Visit(agg);
        // EXISTS / NOT EXISTS — group pattern is not an expression; represent with empty args
        if (context.existsFunc() is not null)
            return Query.Expression.NewExprBuiltInCall("EXISTS", Microsoft.FSharp.Collections.FSharpList<Query.Expression>.Empty);
        if (context.notExistsFunc() is not null)
            return Query.Expression.NewExprBuiltInCall("NOT_EXISTS", Microsoft.FSharp.Collections.FSharpList<Query.Expression>.Empty);
        // Generic built-in: capture as name + args
        var name = context.GetChild(0).GetText().ToUpperInvariant();
        var args = context.expression().Select(Visit).ToList();
        return Query.Expression.NewExprBuiltInCall(name, Microsoft.FSharp.Collections.ListModule.OfSeq(args));
    }

    public override Query.Expression VisitIriOrFunctionPrimaryExpression(
        SparqlParser.IriOrFunctionPrimaryExpressionContext context) =>
        throw new NotImplementedException("IRI or function calls not yet implemented in SPARQL parser");

    // --- Binary/unary expression visitors ---

    public override Query.Expression VisitConditionalOrExpression(SparqlParser.ConditionalOrExpressionContext context)
    {
        var operands = context.conditionalAndExpression();
        if (operands.Length == 1)
            return Visit(operands[0]);
        return operands.Skip(1).Aggregate(Visit(operands[0]),
            (acc, rhs) => Query.Expression.NewExprBinaryOp("||", acc, Visit(rhs)));
    }

    public override Query.Expression VisitConditionalAndExpression(SparqlParser.ConditionalAndExpressionContext context)
    {
        var operands = context.valueLogical();
        if (operands.Length == 1)
            return Visit(operands[0]);
        return operands.Skip(1).Aggregate(Visit(operands[0]),
            (acc, rhs) => Query.Expression.NewExprBinaryOp("&&", acc, Visit(rhs)));
    }

    public override Query.Expression VisitRelationalExpression(SparqlParser.RelationalExpressionContext context)
    {
        var left = Visit(context.numericExpression(0));
        if (context.numericExpression().Length == 1)
            return left;
        var right = Visit(context.numericExpression(1));
        // Find the operator token — it's the second child (index 1)
        var op = context.GetChild(1).GetText();
        return Query.Expression.NewExprBinaryOp(op, left, right);
    }

    public override Query.Expression VisitAdditiveExpression(SparqlParser.AdditiveExpressionContext context)
    {
        var first = Visit(context.multiplicativeExpression(0));
        var result = first;
        // Children: multiplicativeExpression ('+'/'-' multiplicativeExpression)*
        int multIdx = 1;
        for (int i = 1; i < context.ChildCount; i++)
        {
            var childText = context.GetChild(i).GetText();
            if (childText == "+" || childText == "-")
            {
                var rhs = Visit(context.multiplicativeExpression(multIdx++));
                result = Query.Expression.NewExprBinaryOp(childText, result, rhs);
                i++; // skip the rhs child
            }
        }
        return result;
    }

    public override Query.Expression VisitMultiplicativeExpression(SparqlParser.MultiplicativeExpressionContext context)
    {
        var first = Visit(context.unaryExpression(0));
        var result = first;
        int unaryIdx = 1;
        for (int i = 1; i < context.ChildCount; i++)
        {
            var childText = context.GetChild(i).GetText();
            if (childText == "*" || childText == "/")
            {
                var rhs = Visit(context.unaryExpression(unaryIdx++));
                result = Query.Expression.NewExprBinaryOp(childText, result, rhs);
                i++;
            }
        }
        return result;
    }

    public override Query.Expression VisitUnaryExpression(SparqlParser.UnaryExpressionContext context)
    {
        var firstChild = context.GetChild(0).GetText();
        if (firstChild == "!" || firstChild == "+" || firstChild == "-")
            return Query.Expression.NewExprUnaryOp(firstChild, Visit(context.primaryExpression()));
        return Visit(context.primaryExpression());
    }

    // --- Aggregate visitors (one per labeled alternative in the aggregate rule) ---

    public override Query.Expression VisitSumAggregate(SparqlParser.SumAggregateContext context) =>
        Query.Expression.NewExprAggregate(
            Query.Aggregate.NewSum(HasDistinct(context), TermFromExpression(context.expression())));

    public override Query.Expression VisitMinAggregate(SparqlParser.MinAggregateContext context) =>
        Query.Expression.NewExprAggregate(
            Query.Aggregate.NewMin(HasDistinct(context), TermFromExpression(context.expression())));

    public override Query.Expression VisitMaxAggregate(SparqlParser.MaxAggregateContext context) =>
        Query.Expression.NewExprAggregate(
            Query.Aggregate.NewMax(HasDistinct(context), TermFromExpression(context.expression())));

    public override Query.Expression VisitAvgAggregate(SparqlParser.AvgAggregateContext context) =>
        Query.Expression.NewExprAggregate(
            Query.Aggregate.NewAvg(HasDistinct(context), TermFromExpression(context.expression())));

    public override Query.Expression VisitSampleAggregate(SparqlParser.SampleAggregateContext context) =>
        Query.Expression.NewExprAggregate(
            Query.Aggregate.NewSample(HasDistinct(context), TermFromExpression(context.expression())));

    public override Query.Expression VisitCountAggregate(SparqlParser.CountAggregateContext context)
    {
        bool distinct = HasDistinct(context);
        var term = context.expression() is { } expr
            ? FSharpOption<Query.Term>.Some(TermFromExpression(expr))
            : FSharpOption<Query.Term>.None;
        return Query.Expression.NewExprAggregate(Query.Aggregate.NewCount(distinct, term));
    }

    public override Query.Expression VisitGroupConcatAggregate(SparqlParser.GroupConcatAggregateContext context)
    {
        bool distinct = HasDistinct(context);
        var term = TermFromExpression(context.expression());
        var separator = context.stringLiteral() is { } sep
            ? FSharpOption<string>.Some(_stringVisitor.Visit(sep))
            : FSharpOption<string>.None;
        return Query.Expression.NewExprAggregate(Query.Aggregate.NewGroupConcat(distinct, term, separator));
    }

    // --- Helpers ---

    private static bool HasDistinct(ParserRuleContext context) =>
        context.children?.Any(c => c.GetText() == "DISTINCT") ?? false;

    private Query.Term TermFromExpression(SparqlParser.ExpressionContext ctx)
    {
        var expr = Visit(ctx);
        return expr switch
        {
            Query.Expression.ExprTerm t => t.Item,
            _ => throw new NotImplementedException($"Only simple term expressions supported in aggregates, got: {ctx.GetText()}")
        };
    }
}
