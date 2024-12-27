/*
 Copyright (C) 2024 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*/


using System.Globalization;
using DagSemTools.Rdf;
using DagSemTools.Ingress;

namespace DagSemTools.Turtle.Parser;

using System.Collections.Generic;
using IriTools;
using static TurtleDocParser;

internal class ResourceVisitor : TurtleDocBaseVisitor<uint>
{
    private StringVisitor _stringVisitor = new();
    private IriGrammarVisitor _iriGrammarVisitor;
    internal PredicateObjectListVisitor _predicateObjectListVisitor { get; private init; }
    internal Datastore Datastore { get; init; }
    public ResourceVisitor(Datastore datastore, IriGrammarVisitor iriGrammarVisitor)
    {
        Datastore = datastore;
        _iriGrammarVisitor = iriGrammarVisitor;
        _predicateObjectListVisitor = new PredicateObjectListVisitor(this);
    }

    private UInt32 GetIriId(IriReference iri)
    {
        var resource = GraphElement.NewIri(iri);
        return Datastore.AddResource(resource);
    }

    /// <summary>
    /// Visits an iri
    /// </summary>
    /// <param name="ctxt"></param>
    /// <returns></returns>
    public override uint VisitIri(IriContext ctxt)
    {
        var iri = _iriGrammarVisitor.VisitIri(ctxt) ?? throw new System.Exception("IRI is null");
        return GetIriId(iri);
    }

    /// <summary>
    /// Visits the abbreviation 'a' for rdf:type
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override uint VisitRdfTypeAbbrVerb(RdfTypeAbbrVerbContext context)
    {
        var iri = new IriReference(Namespaces.RdfType);
        return GetIriId(iri);
    }

    public override uint VisitIntegerLiteral(IntegerLiteralContext context)
    {
        int literal = int.Parse(context.INTEGER().GetText());
        var resource = GraphElement.NewIntegerLiteral(literal);
        return Datastore.AddResource(resource);
    }

    public override uint VisitDecimalLiteral(DecimalLiteralContext context)
    {
        decimal literal = decimal.Parse(context.DECIMAL().GetText(), CultureInfo.InvariantCulture);
        var resource = GraphElement.NewDecimalLiteral(literal);
        return Datastore.AddResource(resource);
    }

    public override uint VisitDoubleLiteral(DoubleLiteralContext context)
    {
        double literal = double.Parse(context.DOUBLE().GetText(), CultureInfo.InvariantCulture);
        var resource = GraphElement.NewDoubleLiteral(literal);
        return Datastore.AddResource(resource);
    }

    public override uint VisitNamedBlankNode(NamedBlankNodeContext context)
    {
        var blankNodeName = context.BLANK_NODE_LABEL().GetText();
        return Datastore.Resources.GetOrCreateNamedAnonResource(blankNodeName);
    }

    public override uint VisitAnonymousBlankNode(AnonymousBlankNodeContext context) => Datastore.NewAnonymousBlankNode();

    public override uint VisitCollection(CollectionContext context)
    {

        var rdfnil = Datastore.AddResource(GraphElement.NewIri(new IriReference(Namespaces.RdfNil)));
        var rdffirst = Datastore.AddResource(GraphElement.NewIri(new IriReference(Namespaces.RdfFirst)));
        var rdfrest = Datastore.AddResource(GraphElement.NewIri(new IriReference(Namespaces.RdfRest)));

        return context.rdfobject()
            .Aggregate(
                rdfnil,
                (rest, rdfobject) =>
                {
                    var node = Datastore.NewAnonymousBlankNode();
                    var value = Visit(rdfobject);
                    Datastore.AddTriple(new Rdf.Ingress.Triple(node, rdffirst, value));
                    Datastore.AddTriple(new Rdf.Ingress.Triple(node, rdfrest, rest));
                    return node;
                }
            );
    }

    private uint GetTripleId(ReifiedTripleContext context) =>
        context.reifier() switch
        {
            null => Datastore.NewAnonymousBlankNode(),
            var reifier => Visit(reifier)
        };

    public override uint VisitReifiedTriple(ReifiedTripleContext context)
    {
        var subject = Visit(context.subjectOrReifiedTriple());
        var predicate = Visit(context.predicate());
        var rdfobject = Visit(context.rdfobject());
        var triple = new Rdf.Ingress.Triple(subject, predicate, rdfobject);
        var tripleId = GetTripleId(context);
        Datastore.AddReifiedTriple(triple, tripleId);
        return tripleId;
    }

    public override uint VisitTripleTerm(TripleTermContext context)
    {
        var subject = Visit(context.ttSubject());
        var predicate = Visit(context.predicate());
        var rdfobject = Visit(context.ttObject());
        var triple = new Rdf.Ingress.Triple(subject, predicate, rdfobject);
        var tripleId = Datastore.NewAnonymousBlankNode();
        Datastore.AddReifiedTriple(triple, tripleId);
        return tripleId;
    }


    public override uint VisitBlankNodePropertyList(BlankNodePropertyListContext context)
    {
        var blankNode = Datastore.NewAnonymousBlankNode();
        var triples = _predicateObjectListVisitor.Visit(context.predicateObjectList())(blankNode);
        triples.ToList().ForEach(triple => Datastore.AddTriple(triple));
        return blankNode;
    }

    public override uint VisitTrueBooleanLiteral(TrueBooleanLiteralContext context)
        => Datastore.AddResource(GraphElement.NewBooleanLiteral(true));

    public override uint VisitFalseBooleanLiteral(FalseBooleanLiteralContext context)
        => Datastore.AddResource(GraphElement.NewBooleanLiteral(false));

    public override uint VisitPlainStringLiteral(PlainStringLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        var literal = GraphElement.NewLiteralString(literalString);
        return Datastore.AddResource(literal);
    }

    public override uint VisitLangLiteral(LangLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        var langDir = context.LANG_DIR().GetText();
        var literal = GraphElement.NewLangLiteral(literalString, langDir);
        return Datastore.AddResource(literal);
    }
    public override uint VisitTypedLiteral(TypedLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        IriReference typeIri = _iriGrammarVisitor.Visit(context.iri());
        GraphElement typedLiteral = typeIri.ToString() switch
        {
            Namespaces.XsdString => GraphElement.NewLiteralString(literalString),
            Namespaces.XsdDouble => GraphElement.NewDoubleLiteral(double.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdDecimal => GraphElement.NewDecimalLiteral(decimal.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdInteger => GraphElement.NewIntegerLiteral(int.Parse(literalString)),
            Namespaces.XsdFloat => GraphElement.NewFloatLiteral(float.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdBoolean => GraphElement.NewBooleanLiteral(bool.Parse(literalString)),
            Namespaces.XsdDateTime => GraphElement.NewDateTimeLiteral(DateTime.Parse(literalString)),
            Namespaces.XsdDate => GraphElement.NewDateLiteral(DateOnly.Parse(literalString)),
            Namespaces.XsdDuration => GraphElement.NewDurationLiteral(TimeSpan.Parse(literalString)),
            Namespaces.XsdTime => GraphElement.NewTimeLiteral(TimeOnly.Parse(literalString)),
            _ => GraphElement.NewTypedLiteral(typeIri, literalString)
        };
        return Datastore.AddResource(typedLiteral);
    }

}