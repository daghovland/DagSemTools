/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using System.Collections;
using DagSemTools.ManchesterAntlr;
using IriTools;
using Microsoft.FSharp.Collections;
using DagSemTools.OwlOntology;

namespace DagSemTools.Manchester.Parser;
using DagSemTools;

internal class FrameVisitor : ManchesterBaseVisitor<(List<ClassAxiom>, List<Assertion>)>
{
    internal ConceptVisitor ConceptVisitor { get; init; }
    private ClassAssertionVisitor ClassAssertionVisitor { get; init; }
    private IndividualAssertionVisitor IndividualAssertionVisitor { get; init; }
    private ObjectPropertyAssertionVisitor ObjectPropertyAssertionVisitor { get; init; }

    public FrameVisitor(ConceptVisitor conceptVisitor)
    {
        ConceptVisitor = conceptVisitor;
        ClassAssertionVisitor = new ClassAssertionVisitor(conceptVisitor);
        IndividualAssertionVisitor = new IndividualAssertionVisitor(conceptVisitor);
        ObjectPropertyAssertionVisitor = new ObjectPropertyAssertionVisitor(conceptVisitor);
    }


    public override (List<ClassAxiom>, List<Assertion>) VisitObjectPropertyFrame(ManchesterParser.ObjectPropertyFrameContext context)
    {
        var objectPropertyIri = ConceptVisitor.IriGrammarVisitor.Visit(context.rdfiri());
        var frame = context.objectPropertyFrameList()
            .SelectMany(ObjectPropertyAssertionVisitor.Visit)
            .Select(classProperty => classProperty(objectPropertyIri))
            .ToList();
        return ([], frame);
    }

    public override (List<ClassAxiom>, List<Assertion>) VisitClassFrame(ManchesterParser.ClassFrameContext context)
    {
        var classIri = ConceptVisitor.IriGrammarVisitor.Visit(context.rdfiri());
        var classConcept = ClassExpression.NewClassName(Iri.NewFullIri(classIri));
        var frame = context.annotatedList()
            .SelectMany(ClassAssertionVisitor.Visit)
            .Select(classProperty => classProperty(classConcept))
            .ToList();
        return (frame, []);
    }

    public override (List<ClassAxiom>, List<Assertion>) VisitIndividualFrame(ManchesterParser.IndividualFrameContext context)
    {
        var individualIri = ConceptVisitor.IriGrammarVisitor.Visit(context.rdfiri());
        var frameList = context.individualFrameList() ??
                        throw new Exception($"Lacking individual fram list on individual {context.rdfiri().GetText()}");
        List<Assertion> frame = frameList
            .SelectMany(IndividualAssertionVisitor.Visit)
            .Select<>(assertion => assertion(individualIri))
            .ToList();
        return ([], frame);
    }

}