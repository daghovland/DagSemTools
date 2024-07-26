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
    }

    public IriGrammarVisitor(Dictionary<string, IriReference> prefixes)
    {
        _prefixes = prefixes;
        AddDefaultPrefixes();
    }
    
    public override IriReference VisitFullIri(FullIriContext ctxt){
        return new IriReference( ctxt.IRI().GetText());
    }

    public override IriReference VisitPrefixedIri(PrefixedIriContext ctxt){
        var prefixedPart = _prefixes[ctxt.prefixName().GetText()];
        var iriString = $"{prefixedPart}{ctxt.localName().GetText()}";
        return new IriReference(iriString);
    }

    public override IriReference VisitEmptyPrefixedIri(EmptyPrefixedIriContext ctxt){
        var prefixedPart = _prefixes[""];
        var iriString = $"{prefixedPart}{ctxt.localName().GetText()}";
        return new IriReference(iriString);
    }
}