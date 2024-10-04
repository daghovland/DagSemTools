grammar Datalog;
import Turtle;

datalogProgram : (directive | rule)* ;

rule : head ':-' body '.' ;

head : ruleAtom ;

body : ruleAtom (',' ruleAtom)* ;

ruleAtom : '[' resource ',' resource ',' resource ']' ;

resource : iri | literal | variable ;

variable : '?' PN_LOCAL ;

