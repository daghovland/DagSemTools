grammar Datalog;
import Turtle;

datalogProgram : (directive | rule)* EOF ;

rule : head ':-' body PERIOD ;

head : positiveRuleAtom ;

body : ruleAtom (COMMA ruleAtom)* ;

ruleAtom : 'NOT' positiveRuleAtom #NegativeRuleAtom
    | positiveRuleAtom #YesRuleAtom 
    ;

positiveRuleAtom : tripleAtom | typeAtom  ;

tripleAtom :
    '[' term COMMA predicate COMMA term ']'
    | predicate '[' term COMMA term ']'
    ;
typeAtom: predicate '[' term ']'  ; 

predicate : iri ;

term : iri | literal | variable ;

variable : '?' PN_CHARS_BASE PN_CHARS_BASE* ;

