/*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using System.Globalization;
using System.Numerics;
using DagSemTools.Ingress;
using DagSemTools.Parser;
using DagSemTools.Rdf;
using IriTools;
using Microsoft.FSharp.Collections;

namespace DagSemTools.Sparql.Parser;

internal class TermVisitor(
    IriGrammarVisitor iriGrammarVisitor,
    GraphElementManager elementManager)
    : SparqlBaseVisitor<Query.Term>
{
    StringVisitor _stringVisitor = new();

    public override Query.Term VisitVar(SparqlParser.VarContext context)
        => Query.Term.NewVariable(ParserUtils.GetVariableName(context.GetText()));

    public override Query.Term VisitIri(SparqlParser.IriContext context)
    => Query.Term.NewResource(
        elementManager.AddNodeResource(
            RdfResource.NewIri(
            iriGrammarVisitor.Visit(context))
        ));
    
    public override Query.Term VisitRdfTypeAbbrVerb(SparqlParser.RdfTypeAbbrVerbContext context)
    {
        var iri = new IriReference(Namespaces.RdfType);
        return Query.Term.NewResource(
            elementManager.AddNodeResource(
                RdfResource.NewIri(
                    iri)
            ));
    }

    public override Query.Term VisitIntegerLiteral(SparqlParser.IntegerLiteralContext context)
    {
        int literal = int.Parse(context.INTEGER().GetText());
        return Query.Term.NewResource(
            elementManager.AddLiteralResource(
            RdfLiteral.NewIntegerLiteral(literal)));
    }

    public override Query.Term VisitDecimalLiteral(SparqlParser.DecimalLiteralContext context)
    {
        decimal literal = decimal.Parse(context.DECIMAL().GetText(), CultureInfo.InvariantCulture);
        var resource = RdfLiteral.NewDecimalLiteral(literal);
        return Query.Term.NewResource(
            elementManager.AddLiteralResource(resource));
    }

    public override Query.Term VisitDoubleLiteral(SparqlParser.DoubleLiteralContext context)
    {
        double literal = double.Parse(context.DOUBLE().GetText(), CultureInfo.InvariantCulture);
        var resource = RdfLiteral.NewDoubleLiteral(literal);
        return Query.Term.NewResource(
            elementManager.AddLiteralResource(resource));
    }

    public override Query.Term VisitNamedBlankNode(SparqlParser.NamedBlankNodeContext context)
    {
        var blankNodeName = context.BLANK_NODE_LABEL().GetText();
        var elem = elementManager.GetOrCreateNamedAnonResource(blankNodeName);
        return Query.Term.NewResource(elem);
    }

    public override Query.Term VisitAnonymousBlankNode(SparqlParser.AnonymousBlankNodeContext context) =>
        Query.Term.NewResource(elementManager.CreateUnnamedAnonResource());

    public override Query.Term VisitCollection(SparqlParser.CollectionContext context)
    {
        throw new NotImplementedException("Collection not implemented");
        // var rdfnil = elementManager.AddNodeResource(RdfResource.NewIri(new IriReference(Namespaces.RdfNil)));
        // var rdffirst = elementManager.AddNodeResource(RdfResource.NewIri(new IriReference(Namespaces.RdfFirst)));
        // var rdfrest = elementManager.AddNodeResource(RdfResource.NewIri(new IriReference(Namespaces.RdfRest)));
        //
        // return context.graphNode()
        //     .Aggregate(
        //         rdfnil,
        //         (rest, rdfobject) =>
        //         {
        //             var node = elementManager.CreateUnnamedAnonResource();
        //             var value = Visit(rdfobject);
        //             elementManager.AddTriple(new Rdf.Ingress.Triple(node, rdffirst, value));
        //             elementManager.AddTriple(new Rdf.Ingress.Triple(node, rdfrest, rest));
        //             return node;
        //         }
        //     );
    }

    private Query.Term GetTripleId(SparqlParser.ReifiedTripleContext context) =>
        context.reifier() switch
        {
            null => Query.Term.NewResource( elementManager.CreateUnnamedAnonResource()),
            var reifier => Visit(reifier)
        };

    public override Query.Term VisitReifiedTriple(SparqlParser.ReifiedTripleContext context)
    {
        var subject = Visit(context.reifiedTripleSubject());
        var predicate = Visit(context.verb());
        var rdfobject = Visit(context.reifiedTripleObject());
        throw new NotImplementedException("Reified triple not implemented");
        // var triple = new Rdf.Ingress.Triple(subject, predicate, rdfobject);
        // var tripleId = GetTripleId(context);
        // elementManager.AddReifiedTriple(triple, tripleId);
        // return tripleId;
    }

    public override Query.Term VisitTripleTerm(SparqlParser.TripleTermContext context)
    {
        var subject = Visit(context.tripleTermSubject());
        var predicate = Visit(context.verb());
        var rdfobject = Visit(context.tripleTermObject());
        // var triple = new Rdf.Ingress.Triple(subject, predicate, rdfobject);
        // var tripleId = elementManager.CreateUnnamedAnonResource();
        // elementManager.AddReifiedTriple(triple, tripleId);
        // return tripleId;
        throw new NotImplementedException("Triple term not implemented");
    }


    public override Query.Term VisitBlankNodePropertyList(SparqlParser.BlankNodePropertyListContext context)
    {
        throw new NotImplementedException("Blank node property list not implemented");
        // var blankNode = elementManager.CreateUnnamedAnonResource();
        // var triples = _predicateObjectListVisitor.Visit(context.predicateObjectList())(blankNode);
        // triples.ToList().ForEach(triple => elementManager.AddTriple(triple));
        // return blankNode;
    }

    public override Query.Term VisitTrueBooleanLiteral(SparqlParser.TrueBooleanLiteralContext context)
        => Query.Term.NewResource( elementManager.AddLiteralResource(RdfLiteral.NewBooleanLiteral(true)));

    public override Query.Term VisitFalseBooleanLiteral(SparqlParser.FalseBooleanLiteralContext context)
        => Query.Term.NewResource( elementManager.AddLiteralResource(RdfLiteral.NewBooleanLiteral(false)));

    public override Query.Term VisitPlainStringLiteral(SparqlParser.PlainStringLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        var literal = RdfLiteral.NewLiteralString(literalString);
        return Query.Term.NewResource(elementManager.AddLiteralResource(literal));
    }

    public override Query.Term VisitLangLiteral(SparqlParser.LangLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        var langDir = context.LANG_DIR().GetText();
        var literal = RdfLiteral.NewLangLiteral(literalString, langDir);
        return Query.Term.NewResource(elementManager.AddLiteralResource(literal));
    }
    public override Query.Term VisitTypedLiteral(SparqlParser.TypedLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        IriReference typeIri = iriGrammarVisitor.Visit(context.iri());
        RdfLiteral typedLiteral = typeIri.ToString() switch
        {
            Namespaces.XsdString => RdfLiteral.NewLiteralString(literalString),
            Namespaces.XsdDouble => RdfLiteral.NewDoubleLiteral(double.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdDecimal => RdfLiteral.NewDecimalLiteral(decimal.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdInteger => RdfLiteral.NewIntegerLiteral(BigInteger.Parse(literalString)),
            Namespaces.XsdFloat => RdfLiteral.NewFloatLiteral(float.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdBoolean => RdfLiteral.NewBooleanLiteral(bool.Parse(literalString)),
            Namespaces.XsdDateTime => RdfLiteral.NewDateTimeLiteral(DateTime.Parse(literalString)),
            Namespaces.XsdDate => RdfLiteral.NewDateLiteral(DateOnly.Parse(literalString)),
            Namespaces.XsdDuration => RdfLiteral.NewDurationLiteral(TimeSpan.Parse(literalString)),
            Namespaces.XsdTime => RdfLiteral.NewTimeLiteral(TimeOnly.Parse(literalString)),
            _ => RdfLiteral.NewTypedLiteral(typeIri, literalString)
        };
        return Query.Term.NewResource(elementManager.AddLiteralResource(typedLiteral));
    }

}