/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

namespace AlcTableau.ManchesterAntlr;
using System.Collections.Generic;
using IriTools;
using static ManchesterParser;

public class IriGrammarVisitor : ManchesterBaseVisitor<IriReference>
{
    private Dictionary<string, IriReference> _prefixes;
    public IriGrammarVisitor()
    {
        _prefixes = new Dictionary<string, IriReference>();
        AddDefaultPrefixes();
    }

    private void AddDefaultPrefixes()
    {

        _prefixes.TryAdd("rdf", new IriReference("https://www.w3.org/1999/02/22-rdf-syntax-ns#"));
        _prefixes.TryAdd("rdfs", new IriReference("https://www.w3.org/2000/01/rdf-schema#"));
        _prefixes.TryAdd("xsd", new IriReference("https://www.w3.org/2001/XMLSchema#"));
        _prefixes.TryAdd("owl", new IriReference("https://www.w3.org/2002/07/owl#"));
    }

    public IriGrammarVisitor(Dictionary<string, IriReference> prefixes)
    {
        _prefixes = prefixes;
        AddDefaultPrefixes();
    }

    public override IriReference VisitFullIri(FullIriContext ctxt)
    {
        return new IriReference(ctxt.IRI().GetText());
    }

    public override IriReference VisitPrefixedIri(PrefixedIriContext ctxt)
    {
        var prefixedPart = _prefixes[ctxt.prefixName.Text];
        var iriString = $"{prefixedPart}{ctxt.localName.Text}";
        return new IriReference(iriString);
    }

    public override IriReference VisitEmptyPrefixedIri(EmptyPrefixedIriContext ctxt)
    {
        var prefixedPart = _prefixes[""];
        var iriString = $"{prefixedPart}{ctxt.simpleName.Text}";
        return new IriReference(iriString);
    }
}