namespace AlcTableau.ManchesterAntlr;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System.Collections.Generic;
using static IManchesterListener;
using System;

public class ManchesterListener : ManchesterBaseListener, IManchesterListener {
    public List<IErrorNode> errors {get; init;}
    public ManchesterListener(){
        errors = new List<IErrorNode>();
    }
    void IParseTreeListener.VisitErrorNode(IErrorNode error){
        errors.Add(error);
    }
    void IManchesterListener.ExitFullIri(ManchesterParser.FullIriContext ctxt){
        Console.WriteLine($"Parsed a full iri {ctxt.IRI()}");
    }
    void IManchesterListener.ExitOntologyDocument(ManchesterParser.OntologyDocumentContext ctxt){
        
    }
}