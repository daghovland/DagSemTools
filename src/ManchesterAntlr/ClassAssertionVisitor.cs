/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using AlcTableau.ManchesterAntlr;
using IriTools;
using Microsoft.FSharp.Collections;

namespace ManchesterAntlr;
using AlcTableau;

public class ClassAssertionVisitor : ManchesterBaseVisitor<IEnumerable<Func<ALC.Concept, ALC.TBoxAxiom>>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    public ClassAssertionVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
    }

    public override IEnumerable<Func<ALC.Concept, ALC.TBoxAxiom>> VisitSubClassOf(ManchesterParser.SubClassOfContext context)
    =>
        context.descriptionAnnotatedList().description().
            Select(ConceptVisitor.Visit)
            .Select<ALC.Concept, Func<ALC.Concept, ALC.TBoxAxiom>>(
                super => (
                    (ALC.Concept sub) => ALC.TBoxAxiom.NewInclusion(sub, super)));

    public override IEnumerable<Func<ALC.Concept, ALC.TBoxAxiom>> VisitEquivalentTo(ManchesterParser.EquivalentToContext context)
        =>
            context.descriptionAnnotatedList().description().
                Select(ConceptVisitor.Visit)
                .Select<ALC.Concept, Func<ALC.Concept, ALC.TBoxAxiom>>(
                    c => (
                        (ALC.Concept frameClass) => ALC.TBoxAxiom.NewEquivalence(frameClass, c)));

}