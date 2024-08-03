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
    private ConceptVisitor _conceptVisitor;
    private FrameVisitor _frameVisitor;
    
    private Dictionary<string, IriReference> prefixes = new Dictionary<string, IriReference>();
    public override ALC.OntologyDocument VisitOntologyDocument(OntologyDocumentContext ctxt){
        foreach (var prefixDecl in ctxt.prefixDeclaration())
            prefixes[prefixDecl.prefixName().GetText()] = new IriReference(prefixDecl.IRI().GetText());
        _conceptVisitor = new ConceptVisitor(prefixes);
        _frameVisitor = new FrameVisitor(_conceptVisitor);
        return Visit(ctxt.ontology());
    }
    
    public override ALC.OntologyDocument VisitOntology(OntologyContext ctxt)
    {
        AlcTableau.ALC.ontologyVersion version = (ctxt.rdfiri(0), ctxt.rdfiri(1)) switch
        {
            (null, null) => ALC.ontologyVersion.UnNamedOntology,
            (not null, null) => ALC.ontologyVersion.NewNamedOntology(_conceptVisitor.IriGrammarVisitor.Visit(ctxt.rdfiri(0))),
            (not null, not null) => ALC.ontologyVersion.NewVersionedOntology(
                _conceptVisitor.IriGrammarVisitor.Visit(ctxt.rdfiri(0)),
                _conceptVisitor.IriGrammarVisitor.Visit(ctxt.rdfiri(1))
            ),
            (null, not null) => throw new Exception("A versioned ontology can only be provided with an ontology IRI")
        };
        var tboxAxioms = ctxt.frame()
            .Select(_frameVisitor.Visit)
            .SelectMany(x => x);
        return ALC.OntologyDocument.NewOntology(
            CreatePrefixList(),
            version,
            System.Tuple.Create(ListModule.OfSeq(tboxAxioms), ListModule.Empty<ALC.ABoxAssertion>())
        );
    }
    private FSharpList<ALC.prefixDeclaration> CreatePrefixList()
    {
        var prefixList = new List<ALC.prefixDeclaration>();
        foreach (var kvp in prefixes)
        {
            var prefix = ALC.prefixDeclaration.NewPrefixDefinition(kvp.Key, kvp.Value);
            prefixList.Add(prefix);
        }
        return ListModule.OfSeq(prefixList);
    }

}