/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using System.Globalization;
using Microsoft.FSharp.Collections;
using Rdf;

namespace AlcTableau.TurtleAntlr;
using System.Collections.Generic;
using IriTools;
using static TurtleParser;

public class IriGrammarVisitor : TurtleBaseVisitor<UInt32>
{
    private StringVisitor _stringVisitor = new();
    
    public TripleTable TripleTable { get; init; }
    private Dictionary<string, IriReference> _prefixes;
    private IriReference? baseIriReference;
    public IriGrammarVisitor(TripleTable tripleTable)
    {

        TripleTable = tripleTable;
        _prefixes = new Dictionary<string, IriReference>();
    }

    

    public IriGrammarVisitor(TripleTable tripleTable, Dictionary<string, IriReference> prefixes)
    {
        TripleTable = tripleTable;
        _prefixes = prefixes;
    }

    public FSharpList<ALC.prefixDeclaration> CreatePrefixList()
    {
        var prefixList = new List<ALC.prefixDeclaration>();
        foreach (var kvp in _prefixes)
        {
            var prefix = ALC.prefixDeclaration.NewPrefixDefinition(kvp.Key, kvp.Value);
            prefixList.Add(prefix);
        }
        return ListModule.OfSeq(prefixList);
    }

    private UInt32 GetIriId(IriReference iri)
    {
        var resource = RDFStore.Resource.NewIri(iri);
        return TripleTable.AddResource(resource);

    }
    
    public override UInt32 VisitFullIri(FullIriContext ctxt)
    {
        var uriString = ctxt.ABSOLUTEIRIREF().GetText()[1..^1];
        var iri = new IriReference(uriString);
        return GetIriId(iri);
    }

    public override uint VisitIntegerLiteral(IntegerLiteralContext context)
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

    public override uint VisitRdfLiteral(RdfLiteralContext context)
    {
        var literalString = _stringVisitor.Visit(context.@string());
        uint typeIriId = context.iri() switch
        {
            null => TripleTable.AddResource(RDFStore.Resource.NewIri(new IriReference(Namespaces.XsdString))),
            var typeIriNode => Visit(typeIriNode)
        };
        
        
        return literal;
    }
    public override UInt32 VisitPrefixedIri(PrefixedIriContext ctxt)
    {
        var prefixedIriString = ctxt.PNAME_LN().GetText();
        var components = prefixedIriString.Split(':', 2);
        var prefix = components[0];
        var namespaceName = _prefixes[prefix];
        var localName = components[1];
        var iriString = $"{namespaceName}{localName}";
        var iri = new IriReference(iriString);
        return GetIriId(iri);
    }

    public override UInt32 VisitRelativeIri(RelativeIriContext ctxt)
    {
        string rdfiristring = ctxt.RELATIVEIRIREF().GetText();
        return GetIriId(ResolveRelativeIri(rdfiristring));
    }

    public IriReference ResolveRelativeIri(string rdfiristring)
    {
        string iriString = rdfiristring[1..^1];
        return baseIriReference switch
        {
            null => throw new Exception($"Relative IRI {iriString} can only be used when the base iri is set. "),
            _ => new IriReference(baseIriReference + iriString)
        };
    }

    public override UInt32 VisitRdfobject(RdfobjectContext context) =>
        Visit(context.GetChild(0));

    public void AddPrefix(string prefix, IriReference iri) =>
        _prefixes.Add(prefix, iri);

    public void SetBase(IriReference iri) =>
        baseIriReference = iri;
}