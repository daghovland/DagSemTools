/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/
namespace DagSemTools.Turtle.Parser;

using Microsoft.FSharp.Collections;
using DagSemTools.Ingress;
using System.Collections.Generic;
using IriTools;
using static TurtleDocParser;

/// <summary>
/// Visitor for the IRI grammar in the Turtle language.
/// </summary>
public class IriGrammarVisitor : TurtleDocBaseVisitor<IriReference>
{
    private Dictionary<string, IriReference> _prefixes;
    private IriReference? _baseIriReference;
    /// <summary>
    /// Constructor for the IriGrammarVisitor.
    /// Creates an empty dictionary for the prefixes.
    /// </summary>
    public IriGrammarVisitor()
    {
        _prefixes = new Dictionary<string, IriReference>();
    }
    /// <summary>
    /// Constructor for the IriGrammarVisitor.
    /// The given prefixes are used.
    /// </summary>
    /// <param name="prefixes"></param>
    public IriGrammarVisitor(Dictionary<string, IriReference> prefixes)
    {
        _prefixes = prefixes;
    }

    /// <summary>
    /// Returns a list of prefix declarations.
    /// </summary>
    /// <returns></returns>
    public FSharpList<prefixDeclaration> CreatePrefixList()
    {
        var prefixList = new List<prefixDeclaration>();
        foreach (var kvp in _prefixes)
        {
            var prefix = prefixDeclaration.NewPrefixDefinition(kvp.Key, kvp.Value);
            prefixList.Add(prefix);
        }
        return ListModule.OfSeq(prefixList);
    }

    /// <summary>
    /// Visits a full IRI in angle brackets f.ex. &lt;https://example.com&gt;
    /// </summary>
    /// <param name="ctxt"></param>
    /// <returns></returns>
    public override IriReference VisitFullIri(FullIriContext ctxt)
    {
        var uriString = ctxt.ABSOLUTEIRIREF().GetText()[1..^1];
        return new IriReference(uriString);
    }

    /// <summary>
    /// Visits a prefixed IRI, f.ex. ex:A or rdfs:label
    /// This is the PNAME_LN rule in the SPARQL grammars
    /// </summary>
    /// <param name="ctxt"></param>
    /// <returns></returns>
    public override IriReference VisitPrefixedIri(PrefixedIriContext ctxt)
    {
        var prefixedIriString = ctxt.PNAME_LN().GetText();
        var components = prefixedIriString.Split(':', 2);
        var prefix = components[0];
        if (!_prefixes.TryGetValue(prefix, out var namespaceName))
            throw new Exception($"Prefix {prefix} is not defined.");
        var localName = components[1];
        var iriString = $"{namespaceName}{localName}";
        return new IriReference(iriString);
    }

    /// <summary>
    /// Visits a relative IRI in angled brackets, f.ex. &lt;../relative&gt;
    /// </summary>
    /// <param name="ctxt"></param>
    /// <returns></returns>
    public override IriReference VisitRelativeIri(RelativeIriContext ctxt)
    {
        string rdfiristring = ctxt.RELATIVEIRIREF().GetText();
        return ResolveRelativeIri(rdfiristring);
    }

    /// <summary>
    /// Resolves a relative IRI to a full IRI using the defined base IRI (Or throws an exception if the base IRI is not set)
    /// </summary>
    /// <param name="rdfiristring"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IriReference ResolveRelativeIri(string rdfiristring)
    {
        string iriString = rdfiristring[1..^1];
        return _baseIriReference switch
        {
            null => throw new Exception($"Relative IRI {iriString} can only be used when the base iri is set. "),
            _ => new IriReference(_baseIriReference + iriString)
        };
    }

    /// <summary>
    /// Adds a prefix to the dictionary of prefixes.
    /// If the prefix exists, silently overwrite it.
    /// This is in line with how prefixes are defined in the Turtle grammar.
    /// </summary>
    /// <param name="prefix"></param>
    /// <param name="iri"></param>
    public void AddPrefix(string prefix, IriReference iri) =>
        _prefixes[prefix] = iri;

    /// <summary>
    /// Set or overwrite the base IRI.
    /// </summary>
    /// <param name="iri"></param>
    public void SetBase(IriReference iri) =>
        _baseIriReference = iri;
}