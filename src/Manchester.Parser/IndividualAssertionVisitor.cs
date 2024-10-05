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
using DagSemTools.AlcTableau;

namespace DagSemTools.Manchester.Parser;
using DagSemTools;

public class IndividualAssertionVisitor : ManchesterBaseVisitor<IEnumerable<Func<IriReference, ALC.ABoxAssertion>>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    public ABoxAssertionVisitor ABoxAssertionVisitor { get; init; }
    public IndividualAssertionVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
        ABoxAssertionVisitor = new ABoxAssertionVisitor(conceptVisitor);
    }

    public override IEnumerable<Func<IriReference, ALC.ABoxAssertion>> VisitIndividualTypes(ManchesterParser.IndividualTypesContext context)
    =>
        context.descriptionAnnotatedList().description().Select(ConceptVisitor.Visit)
            .Select<ALC.Concept, Func<IriReference, ALC.ABoxAssertion>>(
                concept => (
                    (individual) => ALC.ABoxAssertion.NewConceptAssertion(individual, concept)));

    public override IEnumerable<Func<IriReference, ALC.ABoxAssertion>> VisitIndividualFacts(ManchesterParser.IndividualFactsContext context)
    =>
        context.factAnnotatedList().fact()
            .Select<ManchesterParser.FactContext, Func<IriReference, ALC.ABoxAssertion>>(
                    ABoxAssertionVisitor.Visit);

    public override IEnumerable<Func<IriReference, ALC.ABoxAssertion>> VisitIndividualAnnotations(ManchesterParser.IndividualAnnotationsContext context)
        =>
            context.annotations().annotation()
                .Select<ManchesterParser.AnnotationContext, Func<IriReference, ALC.ABoxAssertion>>(
                    ABoxAssertionVisitor.Visit);

}