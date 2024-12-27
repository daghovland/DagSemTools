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

namespace DagSemTools.Datalog.Parser;

using System.Collections.Generic;
using IriTools;
using static DatalogParser;

internal class ResourceVisitor : DatalogBaseVisitor<uint>
{
    private readonly StringVisitor _stringVisitor = new();
    private readonly IriGrammarVisitor _iriGrammarVisitor;
    internal PredicateObjectListVisitor PredicateObjectListVisitor { get; private init; }
    public Datastore Datastore { get; init; }
    public ResourceVisitor(Datastore datastore, IriGrammarVisitor iriGrammarVisitor)
    {
        Datastore = datastore;
        _iriGrammarVisitor = iriGrammarVisitor;
        PredicateObjectListVisitor = new PredicateObjectListVisitor(this);
    }

    private UInt32 GetIriId(IriReference iri)
    {
        var resource = RdfResource.NewIri(iri);
        return Datastore.AddNodeResource(resource);
    }

    public override uint VisitIri(IriContext ctxt)
    {
        var iri = _iriGrammarVisitor.Visit(ctxt)
                  ?? throw new System.Exception($"IRI {ctxt.GetText()} is null");
        return GetIriId(iri);
    }
    public override uint VisitRdfTypeAbbrVerb(RdfTypeAbbrVerbContext ctxt)
    {
        var iri = _iriGrammarVisitor.Visit(ctxt)
                  ?? throw new System.Exception($"IRI {ctxt.GetText()} is null");
        return GetIriId(iri);
    }

    public override uint VisitIntegerLiteral(IntegerLiteralContext context)
    {
        int literal = int.Parse(context.INTEGER().GetText());
        var resource = RdfLiteral.NewIntegerLiteral(literal);
        return Datastore.AddLiteralResource(resource);
    }

    public override uint VisitDecimalLiteral(DecimalLiteralContext context)
    {
        decimal literal = decimal.Parse(context.DECIMAL().GetText(), CultureInfo.InvariantCulture);
        var resource = RdfLiteral.NewDecimalLiteral(literal);
        return Datastore.AddLiteralResource(resource);
    }

    public override uint VisitDoubleLiteral(DoubleLiteralContext context)
    {
        double literal = double.Parse(context.DOUBLE().GetText(), CultureInfo.InvariantCulture);
        var resource = RdfLiteral.NewDoubleLiteral(literal);
        return Datastore.AddLiteralResource(resource);
    }

    public override uint VisitNamedBlankNode(NamedBlankNodeContext context)
    {
        var blankNodeName = context.BLANK_NODE_LABEL().GetText();
        return Datastore.Resources.GetOrCreateNamedAnonResource(blankNodeName);
    }

    public override uint VisitAnonymousBlankNode(AnonymousBlankNodeContext context) => Datastore.NewAnonymousBlankNode();

    public override uint VisitCollection(CollectionContext context)
    {

        var rdfnil = Datastore.AddNodeResource(RdfResource.NewIri(new IriReference(Namespaces.RdfNil)));
        var rdffirst = Datastore.AddNodeResource(RdfResource.NewIri(new IriReference(Namespaces.RdfFirst)));
        var rdfrest = Datastore.AddNodeResource(RdfResource.NewIri(new IriReference(Namespaces.RdfRest)));

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
        var triples = PredicateObjectListVisitor.Visit(context.predicateObjectList())(blankNode);
        triples.ToList().ForEach(triple => Datastore.AddTriple(triple));
        return blankNode;
    }

    /// <summary>
    /// Visits the abbreviation 'true' for xsd:true
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override uint VisitTrueBooleanLiteral(TrueBooleanLiteralContext context)
        => Datastore.AddLiteralResource(RdfLiteral.NewBooleanLiteral(true));

    /// <summary>
    /// Visits the abbreviation 'false' for xsd:false
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override uint VisitFalseBooleanLiteral(FalseBooleanLiteralContext context)
        => Datastore.AddLiteralResource(RdfLiteral.NewBooleanLiteral(false));

    /// <inheritdoc />
    public override uint VisitPlainStringLiteral(PlainStringLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        var literal = RdfLiteral.NewLiteralString(literalString);
        return Datastore.AddLiteralResource(literal);
    }

    /// <summary>
    /// Visits a string literal that has a language specified, f.ex. "Hello"@en
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override uint VisitLangLiteral(LangLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        var langDir = context.LANG_DIR().GetText();
        var literal = RdfLiteral.NewLangLiteral(literalString, langDir);
        return Datastore.AddLiteralResource(literal);
    }
    /// <summary>
    /// Visits a string literal that has a datatype specified, f.ex. "3"^^xsd:integer
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override uint VisitTypedLiteral(TypedLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        IriReference typeIri = _iriGrammarVisitor.Visit(context.iri());
        RdfLiteral typedLiteral = typeIri.ToString() switch
        {
            Namespaces.XsdString => RdfLiteral.NewLiteralString(literalString),
            Namespaces.XsdDouble => RdfLiteral.NewDoubleLiteral(double.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdDecimal => RdfLiteral.NewDecimalLiteral(decimal.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdInteger => RdfLiteral.NewIntegerLiteral(int.Parse(literalString)),
            Namespaces.XsdFloat => RdfLiteral.NewFloatLiteral(float.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdBoolean => RdfLiteral.NewBooleanLiteral(bool.Parse(literalString)),
            Namespaces.XsdDateTime => RdfLiteral.NewDateTimeLiteral(DateTime.Parse(literalString)),
            Namespaces.XsdDate => RdfLiteral.NewDateLiteral(DateOnly.Parse(literalString)),
            Namespaces.XsdDuration => RdfLiteral.NewDurationLiteral(TimeSpan.Parse(literalString)),
            Namespaces.XsdTime => RdfLiteral.NewTimeLiteral(TimeOnly.Parse(literalString)),
            _ => RdfLiteral.NewTypedLiteral(typeIri, literalString)
        };
        return Datastore.AddLiteralResource(typedLiteral);
    }

}