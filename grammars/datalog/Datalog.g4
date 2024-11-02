grammar Datalog;
import TurtleResource;

datalogProgram : (directive | rule)* EOF ;

rule : head ':-' body PERIOD ;

head : positiveRuleAtom ;

body : ruleAtom (COMMA ruleAtom)* ;

ruleAtom : 'NOT' positiveRuleAtom #NegativeRuleAtom
    | positiveRuleAtom #YesRuleAtom 
    ;

positiveRuleAtom : tripleAtom | typeAtom  ;

tripleAtom :
    LSQPAREN term COMMA predicate COMMA term RSQPAREN
    | predicate LSQPAREN term COMMA term RSQPAREN
    ;
typeAtom: predicate LSQPAREN term RSQPAREN  ; 

predicate : iri | variable;

term : iri | literal | variable ;

variable : '?' (PN_CHARS_BASE | PN_PREFIX);
 
    

