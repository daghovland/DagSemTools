/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.OwlOntology;
using IriTools;
using Microsoft.FSharp.Collections;

namespace DagSemTools.Manchester.Parser;

internal class ABoxAssertionVisitor : ManchesterBaseVisitor<Func<Individual, Assertion>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    public ABoxAssertionVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
    }

    public override Func<Individual, Assertion> VisitPositiveObjectPropertyFact(ManchesterParser.PositiveObjectPropertyFactContext context)
    {
        var propertyIri = ObjectPropertyExpression.NewNamedObjectProperty(Iri.NewFullIri(ConceptVisitor.IriGrammarVisitor.Visit(context.role)));
        var value = Individual.NewNamedIndividual(Iri.NewFullIri(ConceptVisitor.IriGrammarVisitor.Visit(context.@object)));
        return (individual) => Assertion.NewObjectPropertyAssertion(ListModule.Empty< Tuple<Iri, AnnotationValue>>(), propertyIri, individual, value);
    }

    public override Func<Individual, Assertion> VisitPositiveDataPropertyFact(ManchesterParser.PositiveDataPropertyFactContext context)
    {
        var propertyIri = Iri.NewFullIri(ConceptVisitor.IriGrammarVisitor.Visit(context.role));
        var value = context.value.GetText();
        return (individual) => Assertion.NewDataPropertyAssertion(ListModule.Empty< Tuple<Iri, AnnotationValue>>(), propertyIri, individual, value);
    }
    public override Func<Individual, Assertion> VisitNegativeObjectPropertyFact(ManchesterParser.NegativeObjectPropertyFactContext context)
    {
        var propertyIri = ObjectPropertyExpression.NewNamedObjectProperty(Iri.NewFullIri(ConceptVisitor.IriGrammarVisitor.Visit(context.role)));
        var value = Individual.NewNamedIndividual(Iri.NewFullIri(ConceptVisitor.IriGrammarVisitor.Visit(context.@object)));
        return (individual) => Assertion.NewNegativeObjectPropertyAssertion(ListModule.Empty< Tuple<Iri, AnnotationValue>>(), propertyIri, individual, value);
    }

    public override Func<Individual, Assertion> VisitNegativeDataPropertyFact(ManchesterParser.NegativeDataPropertyFactContext context)
    {
        var propertyIri = Iri.NewFullIri(ConceptVisitor.IriGrammarVisitor.Visit(context.role));
        var value = context.value.GetText();
        return (individual) => Assertion.NewNegativeDataPropertyAssertion(ListModule.Empty< Tuple<Iri, AnnotationValue>>(), propertyIri, individual, value);
    }

}