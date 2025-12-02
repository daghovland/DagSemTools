/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.ManchesterAntlr;
using IriTools;
using Microsoft.FSharp.Collections;
using DagSemTools.OwlOntology;

namespace DagSemTools.Manchester.Parser;

using DagSemTools;

internal class IndividualAssertionVisitor : ManchesterBaseVisitor<IEnumerable<Func<Individual, Assertion>>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    internal ABoxAssertionVisitor ABoxAssertionVisitor { get; init; }
    public IndividualAssertionVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
        ABoxAssertionVisitor = new ABoxAssertionVisitor(conceptVisitor);
    }

    public override IEnumerable<Func<Individual, Assertion>> VisitIndividualTypes(
        ManchesterParser.IndividualTypesContext context)
    =>
        context.descriptionAnnotatedList().description().Select(ConceptVisitor.Visit)
            .Select<ClassExpression, Func<Individual, Assertion>>(
                concept => (
                    (individual) => Assertion.NewClassAssertion(
                        ListModule.Empty<Tuple<Iri, AnnotationValue>>(),
                        concept,
                        individual)));

    public override IEnumerable<Func<Individual, Assertion>> VisitIndividualFacts(ManchesterParser.IndividualFactsContext context)
    =>
        context.factAnnotatedList().fact()
            .Select(ABoxAssertionVisitor.Visit);

    public override IEnumerable<Func<Individual, Assertion>> VisitIndividualAnnotations(ManchesterParser.IndividualAnnotationsContext context)
        =>
            context.annotations().annotation()
                .Select(ABoxAssertionVisitor.Visit);

}