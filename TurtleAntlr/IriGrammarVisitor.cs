/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using Microsoft.FSharp.Collections;
using Rdf;

namespace AlcTableau.TurtleAntlr;
using System.Collections.Generic;
using IriTools;
using static TurtleParser;

public class IriGrammarVisitor : TurtleBaseVisitor<UInt32>
{
    public TripleTable tripleTable { get; set; }
    private Dictionary<string, IriReference> _prefixes;
    private IriReference? baseIriReference;
    public IriGrammarVisitor(TripleTable tripleTable)
    {

        tripleTable = tripleTable;
        _prefixes = new Dictionary<string, IriReference>();
    }


    public IriGrammarVisitor(TripleTable _tripleTable, Dictionary<string, IriReference> prefixes)
    {

        tripleTable = _tripleTable;
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
        return tripleTable.AddResource(resource);

    }
    public override UInt32 VisitFullIri(FullIriContext ctxt)
    {
        var uriString = ctxt.IRIREF().GetText()[1..^1];
        var iri = new IriReference(uriString);
        return GetIriId(iri);
    }

    public override UInt32 VisitPrefixedIri(PrefixedIriContext ctxt)
    {
        var prefixedIriString = ctxt.PNAME_NS().GetText();
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
        var prefixedPart = baseIriReference ??
                           throw new Exception("relative IRIs can only be used when the base iri is set. ");
        var iriString = $"{baseIriReference}{ctxt.PNAME_LN()}";
        return GetIriId(new IriReference(iriString));
    }

    public override UInt32 VisitRdfobject(RdfobjectContext context) =>
        Visit(context.GetChild(0));
}