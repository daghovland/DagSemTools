/*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.Rdf;
using DagSemTools.Parser;
using Microsoft.FSharp.Collections;

namespace DagSemTools.Sparql.Parser;

internal class ExpressionVisitor(TermVisitor termVisitor) : SparqlBaseVisitor<Query.Expression>
{
    public override Query.Expression VisitRdfLiteralPrimaryExpression(
        SparqlParser.RdfLiteralPrimaryExpressionContext context) =>
            Query.Expression.NewExprTerm(termVisitor.Visit(context.rdfLiteral()));
    

    public override Query.Expression VisitBracketedPrimaryExpression(
        SparqlParser.BracketedPrimaryExpressionContext context) =>
            Visit(context.brackettedExpression().expression());

    public override Query.Expression VisitNumericLiteralPrimaryExpression(
        SparqlParser.NumericLiteralPrimaryExpressionContext context) =>
        Query.Expression.NewExprTerm(termVisitor.Visit(context.numericLiteral()));
    
    
    public override Query.Expression VisitBooleanLiteralPrimaryExpression(
        SparqlParser.BooleanLiteralPrimaryExpressionContext context) =>
        Query.Expression.NewExprTerm(termVisitor.Visit(context.booleanLiteral()));

    public override Query.Expression VisitVariablePrimaryExpression(
        SparqlParser.VariablePrimaryExpressionContext context) =>
        Query.Expression.NewExprTerm(termVisitor.Visit(context.var()));

    public override Query.Expression VisitBuiltInCallPrimaryExpression(
        SparqlParser.BuiltInCallPrimaryExpressionContext context) =>
        throw new NotImplementedException("Built-in calls not yet implemented in SPARQL parser");

    public override Query.Expression VisitIriOrFunctionPrimaryExpression(
        SparqlParser.IriOrFunctionPrimaryExpressionContext context) =>
        throw new NotImplementedException("IRI or function calls not yet implemented in SPARQL parser");    
    
    
}