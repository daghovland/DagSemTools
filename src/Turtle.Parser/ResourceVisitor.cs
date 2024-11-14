/*
 Copyright (C) 2024 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*/


using System.Globalization;
using System.Security.Cryptography;
using DagSemTools.Rdf;
using DagSemTools.Resource;
using DagSemTools.Turtle.Parser;

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
        var resource = Resource.Resource.NewIri(iri);
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
        var resource = Resource.Resource.NewIntegerLiteral(literal);
        return Datastore.AddResource(resource);
    }

    public override uint VisitDecimalLiteral(DecimalLiteralContext context)
    {
        decimal literal = decimal.Parse(context.DECIMAL().GetText(), CultureInfo.InvariantCulture);
        var resource = Resource.Resource.NewDecimalLiteral(literal);
        return Datastore.AddResource(resource);
    }

    public override uint VisitDoubleLiteral(DoubleLiteralContext context)
    {
        double literal = double.Parse(context.DOUBLE().GetText(), CultureInfo.InvariantCulture);
        var resource = Resource.Resource.NewDoubleLiteral(literal);
        return Datastore.AddResource(resource);
    }

    public override uint VisitNamedBlankNode(NamedBlankNodeContext context)
    {
        var blankNode = Resource.Resource.NewNamedBlankNode(context.BLANK_NODE_LABEL().GetText());
        return Datastore.AddResource(blankNode);
    }

    public override uint VisitAnonymousBlankNode(AnonymousBlankNodeContext context) => Datastore.NewAnonymousBlankNode();

    public override uint VisitCollection(CollectionContext context)
    {

        var rdfnil = Datastore.AddResource(Resource.Resource.NewIri(new IriReference(Namespaces.RdfNil)));
        var rdffirst = Datastore.AddResource(Resource.Resource.NewIri(new IriReference(Namespaces.RdfFirst)));
        var rdfrest = Datastore.AddResource(Resource.Resource.NewIri(new IriReference(Namespaces.RdfRest)));

        return context.rdfobject()
            .Aggregate(
                rdfnil,
                (rest, rdfobject) =>
                {
                    var node = Datastore.NewAnonymousBlankNode();
                    var value = Visit(rdfobject);
                    Datastore.AddTriple(new Ingress.Triple(node, rdffirst, value));
                    Datastore.AddTriple(new Ingress.Triple(node, rdfrest, rest));
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
        var triple = new Ingress.Triple(subject, predicate, rdfobject);
        var tripleId = GetTripleId(context);
        Datastore.AddReifiedTriple(triple, tripleId);
        return tripleId;
    }

    public override uint VisitTripleTerm(TripleTermContext context)
    {
        var subject = Visit(context.ttSubject());
        var predicate = Visit(context.predicate());
        var rdfobject = Visit(context.ttObject());
        var triple = new Ingress.Triple(subject, predicate, rdfobject);
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
        => Datastore.AddResource(Resource.Resource.NewBooleanLiteral(true));

    public override uint VisitFalseBooleanLiteral(FalseBooleanLiteralContext context)
        => Datastore.AddResource(Resource.Resource.NewBooleanLiteral(false));

    public override uint VisitPlainStringLiteral(PlainStringLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        var literal = Resource.Resource.NewLiteralString(literalString);
        return Datastore.AddResource(literal);
    }

    public override uint VisitLangLiteral(LangLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        var langDir = context.LANG_DIR().GetText();
        var literal = Resource.Resource.NewLangLiteral(literalString, langDir);
        return Datastore.AddResource(literal);
    }
    public override uint VisitTypedLiteral(TypedLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        IriReference typeIri = _iriGrammarVisitor.Visit(context.iri());
        Resource.Resource typedLiteral = typeIri.ToString() switch
        {
            Namespaces.XsdString => Resource.Resource.NewLiteralString(literalString),
            Namespaces.XsdDouble => Resource.Resource.NewDoubleLiteral(double.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdDecimal => Resource.Resource.NewDecimalLiteral(decimal.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdInteger => Resource.Resource.NewIntegerLiteral(int.Parse(literalString)),
            Namespaces.XsdFloat => Resource.Resource.NewFloatLiteral(float.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdBoolean => Resource.Resource.NewBooleanLiteral(bool.Parse(literalString)),
            Namespaces.XsdDateTime => Resource.Resource.NewDateTimeLiteral(DateTime.Parse(literalString)),
            Namespaces.XsdDate => Resource.Resource.NewDateLiteral(DateOnly.Parse(literalString)),
            Namespaces.XsdDuration => Resource.Resource.NewDurationLiteral(TimeSpan.Parse(literalString)),
            Namespaces.XsdTime => Resource.Resource.NewTimeLiteral(TimeOnly.Parse(literalString)),
            _ => Resource.Resource.NewTypedLiteral(typeIri, literalString)
        };
        return Datastore.AddResource(typedLiteral);
    }

}