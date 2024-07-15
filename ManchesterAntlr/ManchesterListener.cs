namespace AlcTableau.ManchesterAntlr;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using static IManchesterListener;
using System;

public class ManchesterListener : ManchesterBaseListener, IManchesterListener {
    void IParseTreeListener.VisitTerminal(ITerminalNode term){
        throw new NotImplementedException();
    }
    void IParseTreeListener.VisitErrorNode(IErrorNode error){
        throw new NotImplementedException();
    }
    void IParseTreeListener.EnterEveryRule(ParserRuleContext ctxt){
        throw new NotImplementedException();
    }
    void IParseTreeListener.ExitEveryRule(ParserRuleContext ctxt){
        throw new NotImplementedException();
    }
    void IManchesterListener.EnterRdfiri(ManchesterParser.RdfiriContext ctxt){
        throw new NotImplementedException();
    }
    void IManchesterListener.ExitRdfiri(ManchesterParser.RdfiriContext ctxt){
        throw new NotImplementedException();
    }
    void IManchesterListener.EnterFullIri(ManchesterParser.FullIriContext ctxt){
        throw new NotImplementedException();
    }
    void IManchesterListener.ExitFullIri(ManchesterParser.FullIriContext ctxt){
        throw new NotImplementedException();
    }
    void IManchesterListener.EnterOntologyDocument(ManchesterParser.OntologyDocumentContext ctxt){
        throw new NotImplementedException();
    }
    void IManchesterListener.ExitOntologyDocument(ManchesterParser.OntologyDocumentContext ctxt){
        throw new NotImplementedException();
    }
        void IManchesterListener.EnterOntology(ManchesterParser.OntologyContext ctxt){
        throw new NotImplementedException();
    }
    void IManchesterListener.ExitOntology(ManchesterParser.OntologyContext ctxt){
        throw new NotImplementedException();
    }
            void IManchesterListener.EnterPrefixDeclaration(ManchesterParser.PrefixDeclarationContext ctxt){
        throw new NotImplementedException();
    }
    void IManchesterListener.ExitPrefixDeclaration(ManchesterParser.PrefixDeclarationContext ctxt){
        throw new NotImplementedException();
    }
}