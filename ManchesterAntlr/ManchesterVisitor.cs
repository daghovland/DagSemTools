namespace AlcTableau.ManchesterAntlr;
using AlcTableau;
using System;
using System.Collections.Generic;
using IriTools;
using static ManchesterParser;

public class ManchesterVisitor : ManchesterBaseVisitor<AlcTableau.ALC.OntologyDocument>
{
    private Dictionary<string, IriReference> prefixes = new Dictionary<string, IriReference>();
    public override ALC.OntologyDocument VisitOntologyDocument(OntologyDocumentContext ctxt){
        foreach (var prefixDecl in ctxt.prefixDeclaration())
            prefixes[prefixDecl.prefixName().GetText()] = new IriReference(prefixDecl.IRI().GetText());
        return Visit(ctxt.ontology());
    }

}