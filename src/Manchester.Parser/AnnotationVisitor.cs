/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.Ingress;
using DagSemTools.OwlOntology;
using IriTools;
using Microsoft.FSharp.Collections;

namespace DagSemTools.Manchester.Parser;

internal class AnnotationVisitor : ManchesterBaseVisitor<(Iri, AnnotationValue)>
{
    public IriGrammarVisitor IriVisitor { get; init; }
    public AnnotationVisitor(IriGrammarVisitor iriVisitor)
    {
        IriVisitor = iriVisitor;
    }    
    public override (Iri, AnnotationValue) VisitObjectAnnotation(ManchesterParser.ObjectAnnotationContext context)
    {
        var propertyIri = IriVisitor.Visit(context.rdfiri(0));
        var value = IriVisitor.Visit(context.rdfiri(1));
        return (Iri.NewFullIri(propertyIri), AnnotationValue.NewIndividualAnnotation(Individual.NewNamedIndividual(Iri.NewFullIri(value))));
    }

    public override (Iri, AnnotationValue) VisitLiteralAnnotation(ManchesterParser.LiteralAnnotationContext context)
    {
        var propertyIri = IriVisitor.Visit(context.rdfiri());
        var value = context.literal().GetText();
        var literalValue = GraphElement.NewGraphLiteral(RdfLiteral.NewLiteralString(value));
        return (Iri.NewFullIri(propertyIri), AnnotationValue.NewLiteralAnnotation(literalValue));
    }

}