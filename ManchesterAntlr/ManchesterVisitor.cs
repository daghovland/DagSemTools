namespace AlcTableau.ManchesterAntlr;
using AlcTableau;
using System;
using System.Collections.Generic;
using IriTools;
using static ManchesterParser;

public class ManchesterVisitor : ManchesterBaseVisitor<AlcTableau.ALC.TBoxAxiom>
{
    private Dictionary<string, IriReference> prefixes = new Dictionary<string, IriReference>();
    public override ALC.TBoxAxiom VisitOntologyDocument(OntologyDocumentContext ctxt){
        foreach (var prefixDecl in ctxt.prefixDeclaration())
            prefixes[prefixDecl.PREFIXNAME().GetText()] = new IriReference(prefixDecl.IRI().GetText());
        return Visit(ctxt.ontology());
    }

}