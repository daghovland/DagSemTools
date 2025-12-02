/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.ManchesterAntlr;
using DagSemTools.Parser;
using Antlr4.Runtime;
using DagSemTools.OwlOntology;
using IriTools;
using Microsoft.FSharp.Collections;

namespace DagSemTools.Manchester.Parser;

using DagSemTools;

internal class ConceptVisitor : ManchesterBaseVisitor<ClassExpression>
{
    public IriGrammarVisitor IriGrammarVisitor { get; init; }
    private RoleVisitor _roleVisitor;
    private IVisitorErrorListener _errorListener;
    public ConceptVisitor(IVisitorErrorListener errorListener)
    : this(new IriGrammarVisitor(errorListener))
    { _errorListener = errorListener; }
    public ConceptVisitor(IriGrammarVisitor iriGrammarVisitor)
    {
        IriGrammarVisitor = iriGrammarVisitor;
        _errorListener = iriGrammarVisitor.ErrorListener;
        _roleVisitor = new RoleVisitor(IriGrammarVisitor, _errorListener);
    }


    public ConceptVisitor(Dictionary<string, IriReference> prefixes, IVisitorErrorListener errorListener)
    : this(new IriGrammarVisitor(prefixes, errorListener))
    { }

    public override ClassExpression VisitIriPrimaryConcept(ManchesterParser.IriPrimaryConceptContext context)
    {
        var iri = IriGrammarVisitor.Visit(context.rdfiri());
        return ClassExpression.NewClassName(Iri.NewFullIri(iri));
    }

    public override ClassExpression VisitParenthesizedPrimaryConcept(ManchesterParser.ParenthesizedPrimaryConceptContext context) =>
        Visit(context.description());
    public override ClassExpression VisitNegatedPrimaryConcept(ManchesterParser.NegatedPrimaryConceptContext context) =>
        ClassExpression.NewObjectComplementOf(Visit(context.primary()));

    public override ClassExpression VisitConceptDisjunction(ManchesterParser.ConceptDisjunctionContext context) =>
        ClassExpression.NewObjectUnionOf(ListModule.OfSeq([Visit(context.description()), Visit(context.conjunction())]));

    public override ClassExpression VisitConceptSingleDisjunction(ManchesterParser.ConceptSingleDisjunctionContext context) =>
        Visit(context.conjunction());

    public override ClassExpression VisitConceptConjunction(ManchesterParser.ConceptConjunctionContext context)
    {
        var conjunction = Visit(context.conjunction());
        var primary = Visit(context.primary());
        return ClassExpression.NewObjectIntersectionOf(ListModule.OfSeq([conjunction, primary]));
    }
    public override ClassExpression VisitConceptSingleConjunction(ManchesterParser.ConceptSingleConjunctionContext context) =>
        Visit(context.primary());

    public override ClassExpression VisitUniversalConceptRestriction(ManchesterParser.UniversalConceptRestrictionContext context) =>
        ClassExpression.NewObjectAllValuesFrom(
            _roleVisitor.Visit(context.objectPropertyExpression()),
            Visit(context.primary()));

    public override ClassExpression VisitExistentialConceptRestriction(ManchesterParser.ExistentialConceptRestrictionContext context) =>
        ClassExpression.NewObjectSomeValuesFrom(
            _roleVisitor.Visit(context.objectPropertyExpression()),
            Visit(context.primary()));

}