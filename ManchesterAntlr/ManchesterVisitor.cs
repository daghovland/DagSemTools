using System.Runtime.Loader;
using ManchesterAntlr;
using Microsoft.FSharp.Collections;

namespace AlcTableau.ManchesterAntlr;
using AlcTableau;
using System;
using System.Collections.Generic;
using IriTools;
using static ManchesterParser;

public class ManchesterVisitor : ManchesterBaseVisitor<AlcTableau.ALC.OntologyDocument>
{
    private ConceptVisitor? _conceptVisitor;
    private FrameVisitor? _frameVisitor;

    private readonly Dictionary<string, IriReference> _prefixes = new Dictionary<string, IriReference>();

    public override ALC.OntologyDocument VisitOntologyDocument(OntologyDocumentContext ctxt)
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

        _conceptVisitor = new ConceptVisitor(_prefixes);
        _frameVisitor = new FrameVisitor(_conceptVisitor);
        return Visit(ctxt.ontology());
    }

    public static IEnumerable<T> concateOrKeep<T>(IEnumerable<T> a, IEnumerable<T>? b) =>
        b == null ? a : a.Concat(b);


    public override ALC.OntologyDocument VisitOntology(OntologyContext ctxt)
    {
        if (_frameVisitor == null || _conceptVisitor == null)
            throw new Exception("Smoething strange happened. Please report. FrameVisitor and ConceptVisitor should have been initialized in VisitOntologyDocument before visiting ontology");
        ALC.ontologyVersion version = (ctxt.rdfiri(0), ctxt.rdfiri(1)) switch
        {
            (null, null) => ALC.ontologyVersion.UnNamedOntology,
            (not null, null) => ALC.ontologyVersion.NewNamedOntology(_conceptVisitor.IriGrammarVisitor.Visit(ctxt.rdfiri(0))),
            (not null, not null) => ALC.ontologyVersion.NewVersionedOntology(
                _conceptVisitor.IriGrammarVisitor.Visit(ctxt.rdfiri(0)),
                _conceptVisitor.IriGrammarVisitor.Visit(ctxt.rdfiri(1))
            ),
            (null, not null) => throw new Exception("A versioned ontology can only be provided with an ontology IRI")
        };
        var knowledgeBase = ctxt.frame()
            .Select(_frameVisitor.Visit)
            .Aggregate<(List<ALC.TBoxAxiom>, List<ALC.ABoxAssertion>), (IEnumerable<ALC.TBoxAxiom>, IEnumerable<ALC.ABoxAssertion>)>
            ((new List<ALC.TBoxAxiom>(), new List<ALC.ABoxAssertion>()),
                (acc, x) => (concateOrKeep(acc.Item1, x.Item1), concateOrKeep(acc.Item2, x.Item2)));
        return ALC.OntologyDocument.NewOntology(
            CreatePrefixList(),
            version,
            System.Tuple.Create(ListModule.OfSeq(knowledgeBase.Item1), ListModule.OfSeq(knowledgeBase.Item2))
        );
    }
    private FSharpList<ALC.prefixDeclaration> CreatePrefixList()
    {
        var prefixList = new List<ALC.prefixDeclaration>();
        foreach (var kvp in _prefixes)
        {
            var prefix = ALC.prefixDeclaration.NewPrefixDefinition(kvp.Key, kvp.Value);
            prefixList.Add(prefix);
        }
        return ListModule.OfSeq(prefixList);
    }

}