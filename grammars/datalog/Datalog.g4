grammar Datalog;
import TurtleResource;

datalogProgram : (directive | rule)* EOF ;

rule : 
    head ':-' body PERIOD #ProperRule
    | head PERIOD #Fact
    ;

head : positiveRuleAtom 
    | "FALSE";

body : ruleAtom (COMMA ruleAtom)* ;

ruleAtom : 'NOT' positiveRuleAtom #NegativeRuleAtom
    | positiveRuleAtom #YesRuleAtom 
    ;

positiveRuleAtom : tripleAtom | typeAtom  ;

tripleAtom :
    LSQPAREN term COMMA relation COMMA term RSQPAREN
    | relation LSQPAREN term COMMA term RSQPAREN
    ;
typeAtom: relation LSQPAREN term RSQPAREN
    ; 

relation : variable | verb ;

term : rdfobject | variable ;

variable : '?' (PN_CHARS_BASE | PN_PREFIX);
 
    

