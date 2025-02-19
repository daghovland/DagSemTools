/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools.Parser;
using Microsoft.FSharp.Collections;
using DagSemTools.Ingress;
using DagSemTools.OwlOntology;

namespace DagSemTools.Manchester.Parser;
using DagSemTools;
using System;
using System.Collections.Generic;
using IriTools;
using static ManchesterParser;

internal class ManchesterVisitor : ManchesterBaseVisitor<OwlOntology.OntologyDocument>
{
    private ConceptVisitor? _conceptVisitor;
    private FrameVisitor? _frameVisitor;
    private IVisitorErrorListener _errorListener;

    internal ManchesterVisitor(IVisitorErrorListener errorListener)
    {
        _errorListener = errorListener;
    }

    private readonly Dictionary<string, IriReference> _prefixes = new Dictionary<string, IriReference>();

    public override OntologyDocument VisitOntologyDocument(OntologyDocumentContext ctxt)
    {
        foreach (var prefixDecl in ctxt.prefixDeclaration())
        {
            if (prefixDecl is NonEmptyprefixDeclarationContext NonEmptyPrefix)
                _prefixes.Add(NonEmptyPrefix.prefixName.Text, new IriReference(NonEmptyPrefix.IRI().GetText()));
            else if (prefixDecl is EmptyPrefixContext emptyPrefix)
                _prefixes.Add("", new IriReference(emptyPrefix.IRI().GetText()));
            else
                throw new Exception("Unknown prefix declaration type");
        }

        _conceptVisitor = new ConceptVisitor(_prefixes, _errorListener);
        _frameVisitor = new FrameVisitor(_conceptVisitor);
        return ctxt.ontology() switch
        {
            null => new OntologyDocument(
                CreatePrefixList(),
                new Ontology(
                    ListModule.Empty<IriReference>(),
                    ontologyVersion.UnNamedOntology,
                    ListModule.Empty<Annotation>(),
                    ListModule.Empty<Axiom>()
                )),
            _ => Visit(ctxt.ontology())
        };
    }

    public static IEnumerable<T> concateOrKeep<T>(IEnumerable<T> a, IEnumerable<T>? b) =>
        b == null ? a : a.Concat(b);


    public override OntologyDocument VisitOntology(OntologyContext ctxt)
    {
        if (_frameVisitor == null || _conceptVisitor == null)
            throw new Exception("Smoething strange happened. Please report. FrameVisitor and ConceptVisitor should have been initialized in VisitOntologyDocument before visiting ontology");
        ontologyVersion version = (ctxt.rdfiri(0), ctxt.rdfiri(1)) switch
        {
            (null, null) => ontologyVersion.UnNamedOntology,
            (not null, null) => ontologyVersion.NewNamedOntology(_conceptVisitor.IriGrammarVisitor.Visit(ctxt.rdfiri(0))),
            (not null, not null) => ontologyVersion.NewVersionedOntology(
                _conceptVisitor.IriGrammarVisitor.Visit(ctxt.rdfiri(0)),
                _conceptVisitor.IriGrammarVisitor.Visit(ctxt.rdfiri(1))
            ),
            (null, not null) => throw new Exception("A versioned ontology can only be provided with an ontology IRI")
        };
        var knowledgeBase = ctxt.frame()
            .Select(_frameVisitor.Visit)
            .Aggregate<(List<ClassAxiom>, List<Assertion>), (IEnumerable<ClassAxiom>, IEnumerable<Assertion>)>
            ((new List<ClassAxiom>(), new List<Assertion>()),
                (acc, x) => (concateOrKeep(acc.Item1, x.Item1), concateOrKeep(acc.Item2, x.Item2)));
        return new OntologyDocument(
            CreatePrefixList(),
            new Ontology(
                ListModule.Empty<IriReference>(),
                ontologyVersion.UnNamedOntology,
                ListModule.Empty<Annotation>(),
                 ListModule.OfSeq(knowledgeBase.Item1.Concat(knowledgeBase.Item2)) 
                )
        );
    }
    private FSharpList<OwlOntology.prefixDeclaration> CreatePrefixList()
    {
        var prefixList = new List<OwlOntology.prefixDeclaration>();
        foreach (var kvp in _prefixes)
        {
            var prefix = OwlOntology.prefixDeclaration.NewPrefixDefinition(kvp.Key, kvp.Value);
            prefixList.Add(prefix);
        }
        return ListModule.OfSeq(prefixList);
    }

}