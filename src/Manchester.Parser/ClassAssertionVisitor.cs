/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.OwlOntology;
using Microsoft.FSharp.Collections;

namespace DagSemTools.Manchester.Parser;
using DagSemTools;

internal class ClassAssertionVisitor : ManchesterBaseVisitor<IEnumerable<Func<OwlOntology.ClassExpression, ClassAxiom>>>
{
    public ConceptVisitor ConceptVisitor { get; init; }
    public ClassAssertionVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
    }

    public override IEnumerable<Func<ClassExpression, ClassAxiom>> VisitSubClassOf(ManchesterParser.SubClassOfContext context)
    =>
        context.descriptionAnnotatedList().description().
            Select(ConceptVisitor.Visit)
            .Select<ClassExpression, Func<ClassExpression, ClassAxiom>>(
                super => (
                    (ClassExpression sub) => ClassAxiom.NewSubClassOf(ListModule.Empty<Annotation>(), sub, super)));

    public override IEnumerable<Func<ClassExpression, ClassAxiom>> VisitEquivalentTo(ManchesterParser.EquivalentToContext context)
        =>
            context.descriptionAnnotatedList().description().
                Select(ConceptVisitor.Visit)
                .Select<ClassExpression, Func<ClassExpression, ALC.TBoxAxiom>>(
                    c => (
                        (ClassExpression frameClass) => ClassAxiom.NewEquivalence(frameClass, c)));

}