/*
 Copyright (C) 2024 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*/


using System.Globalization;
using System.Security.Cryptography;
using AlcTableau.Rdf;

namespace AlcTableau.TurtleAntlr;

using System.Collections.Generic;
using IriTools;
using static TurtleParser;

internal class ResourceVisitor : TurtleBaseVisitor<uint>
{
    private StringVisitor _stringVisitor = new();
    private IriGrammarVisitor _iriGrammarVisitor;
    internal PredicateObjectListVisitor _predicateObjectListVisitor { get; private init; }
    public TripleTable TripleTable { get; init; }
    public ResourceVisitor(TripleTable tripleTable, IriGrammarVisitor iriGrammarVisitor)
    {
        TripleTable = tripleTable;
        _iriGrammarVisitor = iriGrammarVisitor;
        _predicateObjectListVisitor = new PredicateObjectListVisitor(this);
    }

    private UInt32 GetIriId(IriReference iri)
    {
        var resource = RDFStore.Resource.NewIri(iri);
        return TripleTable.AddResource(resource);
    }

    public override uint VisitIri(IriContext ctxt)
    {
        var iri = _iriGrammarVisitor.Visit(ctxt);
        return GetIriId(iri);
    }

    public override uint VisitIntegerLiteral(TurtleParser.IntegerLiteralContext context)
    {
        int literal = int.Parse(context.INTEGER().GetText());
        var resource = RDFStore.Resource.NewIntegerLiteral(literal);
        return TripleTable.AddResource(resource);
    }

    public override uint VisitDecimalLiteral(DecimalLiteralContext context)
    {
        decimal literal = decimal.Parse(context.DECIMAL().GetText(), CultureInfo.InvariantCulture);
        var resource = RDFStore.Resource.NewDecimalLiteral(literal);
        return TripleTable.AddResource(resource);
    }

    public override uint VisitDoubleLiteral(DoubleLiteralContext context)
    {
        double literal = double.Parse(context.DOUBLE().GetText(), CultureInfo.InvariantCulture);
        var resource = RDFStore.Resource.NewDoubleLiteral(literal);
        return TripleTable.AddResource(resource);
    }

    public override uint VisitNamedBlankNode(NamedBlankNodeContext context)
    {
        var blankNode = RDFStore.Resource.NewNamedBlankNode(context.BLANK_NODE_LABEL().GetText());
        return TripleTable.AddResource(blankNode);
    }

    public override uint VisitAnonymousBlankNode(AnonymousBlankNodeContext context)
    {
        var anonNumber = TripleTable.ResourceCount;
        var blankNode = RDFStore.Resource.NewAnonymousBlankNode(anonNumber);
        return TripleTable.AddResource(blankNode);
    }

    public override uint VisitCollection(CollectionContext context)
    {
        
        var rdfnil = TripleTable.AddResource(RDFStore.Resource.NewIri(new IriReference(Namespaces.RdfNil)));
        var rdffirst = TripleTable.AddResource(RDFStore.Resource.NewIri(new IriReference(Namespaces.RdfFirst)));
        var rdfrest = TripleTable.AddResource(RDFStore.Resource.NewIri(new IriReference(Namespaces.RdfRest)));
        
        return  context.rdfobject()
            .Aggregate(
                rdfnil,
                (rest, rdfobject) =>
                {
                    var node = TripleTable.NewAnonymousBlankNode();
                    var value = Visit(rdfobject);
                    TripleTable.AddTriple(new RDFStore.Triple(node, rdffirst, value));
                    TripleTable.AddTriple(new RDFStore.Triple(node, rdfrest, rest));
                    return node;
                }
            );
    }
    
    public override uint VisitBlankNodePropertyList(BlankNodePropertyListContext context)
    {
        var blankNode = TripleTable.AddResource(RDFStore.Resource.NewAnonymousBlankNode(TripleTable.ResourceCount));
        var triples = _predicateObjectListVisitor.Visit(context.predicateObjectList())(blankNode);
        triples.ToList().ForEach(triple => TripleTable.AddTriple(triple));
        return blankNode;
    }

    public override uint VisitTrueBooleanLiteral(TrueBooleanLiteralContext context)
        => TripleTable.AddResource(RDFStore.Resource.NewBooleanLiteral(true));

    public override uint VisitFalseBooleanLiteral(FalseBooleanLiteralContext context)
        => TripleTable.AddResource(RDFStore.Resource.NewBooleanLiteral(false));

    public override uint VisitPlainStringLiteral(PlainStringLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        var literal = RDFStore.Resource.NewLiteralString(literalString);
        return TripleTable.AddResource(literal);
    }

    public override uint VisitLangLiteral(LangLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        var langDir = context.LANG_DIR().GetText();
        var literal = RDFStore.Resource.NewLangLiteral(literalString, langDir);
        return TripleTable.AddResource(literal);
    }
    public override uint VisitTypedLiteral(TypedLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.stringLiteral());
        IriReference typeIri = _iriGrammarVisitor.Visit(context.iri());
        RDFStore.Resource typedLiteral = typeIri.ToString() switch
        {
            Namespaces.XsdString => RDFStore.Resource.NewLiteralString(literalString),
            Namespaces.XsdDouble => RDFStore.Resource.NewDoubleLiteral(double.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdDecimal => RDFStore.Resource.NewDecimalLiteral(decimal.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdInteger => RDFStore.Resource.NewIntegerLiteral(int.Parse(literalString)),
            Namespaces.XsdFloat => RDFStore.Resource.NewFloatLiteral(float.Parse(literalString, CultureInfo.InvariantCulture)),
            Namespaces.XsdBoolean => RDFStore.Resource.NewBooleanLiteral(bool.Parse(literalString)),
            Namespaces.XsdDateTime => RDFStore.Resource.NewDateTimeLiteral(DateTime.Parse(literalString)),
            Namespaces.XsdDate => RDFStore.Resource.NewDateLiteral(DateOnly.Parse(literalString)),
            Namespaces.XsdDuration => RDFStore.Resource.NewDurationLiteral(TimeSpan.Parse(literalString)),
            Namespaces.XsdTime => RDFStore.Resource.NewTimeLiteral(TimeOnly.Parse(literalString)),
            _ => RDFStore.Resource.NewTypedLiteral(typeIri, literalString)
        };
        return TripleTable.AddResource(typedLiteral);
    }
    public override UInt32 VisitRdfobject(RdfobjectContext context) =>
        Visit(context.GetChild(0));


}