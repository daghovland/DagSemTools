/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.ManchesterAntlr;
using DagSemTools.Parser;
using DagSemTools.AlcTableau;
using Antlr4.Runtime;
using IriTools;

namespace DagSemTools.Manchester.Parser;
using DagSemTools;

public class ConceptVisitor : ManchesterBaseVisitor<ALC.Concept>
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

    public override ALC.Concept VisitIriPrimaryConcept(ManchesterParser.IriPrimaryConceptContext context)
    {
        var iri = IriGrammarVisitor.Visit(context.rdfiri());
        return ALC.Concept.NewConceptName(iri);
    }

    public override ALC.Concept VisitParenthesizedPrimaryConcept(ManchesterParser.ParenthesizedPrimaryConceptContext context) =>
        Visit(context.description());
    public override ALC.Concept VisitNegatedPrimaryConcept(ManchesterParser.NegatedPrimaryConceptContext context) =>
        ALC.Concept.NewNegation(Visit(context.primary()));

    public override ALC.Concept VisitConceptDisjunction(ManchesterParser.ConceptDisjunctionContext context) =>
        ALC.Concept.NewDisjunction(Visit(context.description()), Visit(context.conjunction()));

    public override ALC.Concept VisitConceptSingleDisjunction(ManchesterParser.ConceptSingleDisjunctionContext context) =>
        Visit(context.conjunction());

    public override ALC.Concept VisitConceptConjunction(ManchesterParser.ConceptConjunctionContext context)
    {
        var conjunction = Visit(context.conjunction());
        var primary = Visit(context.primary());
        return ALC.Concept.NewConjunction(conjunction, primary);
    }
    public override ALC.Concept VisitConceptSingleConjunction(ManchesterParser.ConceptSingleConjunctionContext context) =>
        Visit(context.primary());

    public override ALC.Concept VisitUniversalConceptRestriction(ManchesterParser.UniversalConceptRestrictionContext context) =>
        ALC.Concept.NewUniversal(
            _roleVisitor.Visit(context.objectPropertyExpression()),
            Visit(context.primary()));

    public override ALC.Concept VisitExistentialConceptRestriction(ManchesterParser.ExistentialConceptRestrictionContext context) =>
        ALC.Concept.NewExistential(
            _roleVisitor.Visit(context.objectPropertyExpression()),
            Visit(context.primary()));

}