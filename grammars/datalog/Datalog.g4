grammar Datalog;
import Turtle;

datalogProgram : (directive | rule)* EOF ;

rule : head ':-' body PERIOD ;

head : ruleAtom ;

body : ruleAtom (COMMA ruleAtom)* ;

ruleAtom : tripleAtom | typeAtom  ;

tripleAtom :
    '[' term COMMA predicate COMMA term ']'
    | predicate '[' term COMMA term ']'
    ;
typeAtom: predicate '[' term ']'  ; 

predicate : iri ;

term : iri | literal | variable ;

variable : '?' PN_CHARS_BASE PN_CHARS_BASE* ;

