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

internal class ProjectionVisitor() : SparqlBaseVisitor<Query.ProjectionElement>
{
    private ExpressionVisitor _expressionVisitor = new();
    public override Query.ProjectionElement VisitVar(SparqlParser.VarContext context)
        => Query.ProjectionElement.NewProjectVariable(ParserUtils.GetVariableName(context.GetText()));

    public override Query.ProjectionElement VisitVariableAlias(SparqlParser.VariableAliasContext context)
    {
        var expr = _expressionVisitor.Visit(context.expression());
        var alias = ParserUtils.GetVariableName(context.var().GetText());
        return Query.ProjectionElement.NewProjectExpression(expr, alias);
    }
}