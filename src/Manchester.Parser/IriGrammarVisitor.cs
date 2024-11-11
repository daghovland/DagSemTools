/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using Antlr4.Runtime;
using DagSemTools.Parser;

namespace DagSemTools.Manchester.Parser;
using System.Collections.Generic;
using IriTools;
using static ManchesterParser;

internal class IriGrammarVisitor : ManchesterBaseVisitor<IriReference>
{
    private readonly Dictionary<string, IriReference> _prefixes;
    public readonly IVisitorErrorListener ErrorListener;
    public IriGrammarVisitor(IVisitorErrorListener errorListener)
    {
        _prefixes = new Dictionary<string, IriReference>();
        ErrorListener = errorListener;
        AddDefaultPrefixes();
    }

    private void AddDefaultPrefixes()
    {

        _prefixes.TryAdd("rdf", new IriReference("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
        _prefixes.TryAdd("rdfs", new IriReference("http://www.w3.org/2000/01/rdf-schema#"));
        _prefixes.TryAdd("xsd", new IriReference("http://www.w3.org/2001/XMLSchema#"));
        _prefixes.TryAdd("owl", new IriReference("http://www.w3.org/2002/07/owl#"));
    }

    public IriGrammarVisitor(Dictionary<string, IriReference> prefixes, IVisitorErrorListener errorListener)
    {
        _prefixes = prefixes;
        AddDefaultPrefixes();
        ErrorListener = errorListener;
    }

    public override IriReference VisitFullIri(FullIriContext ctxt)
    {
        return new IriReference(ctxt.IRI().GetText());
    }

    public override IriReference VisitPrefixedIri(PrefixedIriContext ctxt)
    {
        if (!_prefixes.TryGetValue(ctxt.prefixName.Text, out var prefixedPart))
        {
            ErrorListener.VisitorError(ctxt.Start, ctxt.Start.Line,
                ctxt.Start.Column, $"Prefix {ctxt.prefixName.Text} not defined.");
            return new IriReference("https://example.com/error!");
        }
        var iriString = $"{prefixedPart}{ctxt.localName.Text}";
        return new IriReference(iriString);
    }

    public override IriReference VisitEmptyPrefixedIri(EmptyPrefixedIriContext ctxt)
    {
        if (!_prefixes.TryGetValue("", out var prefixedPart))
        {
            ErrorListener.VisitorError(ctxt.Start, ctxt.Start.Line, ctxt.Start.Column, "No default prefix defined.");
            return new IriReference("https://example.com/error!");
        }
        var iriString = $"{prefixedPart}{ctxt.simpleName.Text}";
        return new IriReference(iriString);
    }
}