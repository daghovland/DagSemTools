/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.OwlOntology;
using DagSemTools.Manchester.Parser;
using DagSemTools.Parser;
using Antlr4.Runtime;
using IriTools;

namespace DagSemTools.Manchester.Parser;

internal class RoleVisitor : ManchesterBaseVisitor<ObjectPropertyExpression>
{
    private IriGrammarVisitor _iriGrammarVisitor;
    private IVisitorErrorListener _errorListener;
    internal RoleVisitor(IriGrammarVisitor iriGrammarVisitor, IVisitorErrorListener errorListener)
    {
        _iriGrammarVisitor = iriGrammarVisitor;
        _errorListener = errorListener;
    }

    public override ObjectPropertyExpression VisitObjectPropertyExpression(ManchesterParser.ObjectPropertyExpressionContext context) =>
        context.INVERSE() == null
            ? ObjectPropertyExpression.NewNamedObjectProperty(Iri.NewFullIri(new IriReference(_iriGrammarVisitor.Visit(context.rdfiri()))))
            : ObjectPropertyExpression.NewInverseObjectProperty(ObjectPropertyExpression.NewNamedObjectProperty(Iri.NewFullIri(new IriReference(_iriGrammarVisitor.Visit(context.rdfiri())))));
}