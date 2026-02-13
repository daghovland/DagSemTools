grammar Datalog;
import TurtleTokens;


NOT: 'NOT'|'not';

datalogProgram : (directive | rule)* EOF ;

rule : 
    head ':-' body PERIOD #ProperRule
    | head PERIOD #Fact
    ;

head : positiveRuleAtom #NormalRuleHead
    | 'false' #ContradictionHead;


body : ruleAtom (COMMA ruleAtom)* ;

ruleAtom : NOT positiveRuleAtom #NegativeRuleAtom
    | positiveRuleAtom #YesRuleAtom 
    ;



positiveRuleAtom : t=tripleAtom g=graphname? #QuadPatternAtom
    | ty=typeAtom g=graphname? #TypePatternAtom
    ;

graphname: term;

tripleAtom :
    LSQPAREN term COMMA relation COMMA term RSQPAREN
    | relation LSQPAREN term COMMA term RSQPAREN
    ;
typeAtom: relation LSQPAREN term RSQPAREN
    ; 

relation : variable | verb ;

term : rdfobject | variable ;

variable : '?' (PN_CHARS_BASE | PN_PREFIX);
 
    


directive: prefix | baseDeclaration | version;


prefix: PREFIX_STRING PNAME_NS iri PERIOD? #sparqlPrefix
    | ATPREFIX PNAME_NS iri PERIOD? #prefixId
    ;


ATPREFIX : '@prefix' ;

baseDeclaration: ATBASE ABSOLUTEIRIREF PERIOD 
    | BASE_STRING ABSOLUTEIRIREF 
    ;

BASE_STRING : 'BASE' ;
    
ATBASE : '@base' ;

version: ('@version' | 'VERSION') versionSpecifier '.'? ;
versionSpecifier: string_single_quote; 

PREFIX_STRING: [Pp] [Rr] [Ee] [Ff] [Ii] [Xx];

triples: 
    subject predicateObjectList #NamedSubjectTriples 
    | blankNodePropertyList predicateObjectList? #BlankNodeTriples
    | reifiedTriple predicateObjectList?  #ReifiedTriples
    ;
    
predicateObjectList: verbObjectList (SEMICOLON (verbObjectList)?)*;

verbObjectList: verb annotatedObject (COMMA annotatedObject)*;

annotatedObject : rdfobject annotation ;


verb: 
    predicate #PredicateVerb 
    | 'a' #RdfTypeAbbrVerb
    ;

subject: iri | blankNode | collection | reifiedTriple;

predicate: iri;

rdfobject: iri 
    | blankNode 
    | collection 
    | blankNodePropertyList 
    | literal 
    | tripleTerm 
    | reifiedTriple 
    ;

literal: rdfLiteral | numericLiteral | booleanLiteral;

blankNodePropertyList: LSQPAREN predicateObjectList RSQPAREN;


collection: LPAREN rdfobject* RPAREN;

RPAREN : ')' ;

LPAREN : '(' ;

numericLiteral: INTEGER #integerLiteral 
    | DECIMAL #decimalLiteral
    | DOUBLE #doubleLiteral
    ;

booleanLiteral: 'true' #TrueBooleanLiteral
    | 'false' #FalseBooleanLiteral
    ;

rdfLiteral: stringLiteral #PlainStringLiteral
    | stringLiteral LANG_DIR #LangLiteral
    | stringLiteral '^^' iri? #TypedLiteral
    ;    

stringLiteral: string_single_quote | string_triple_quote;
 
string_single_quote:  STRING_LITERAL_QUOTE  | STRING_LITERAL_SINGLE_QUOTE;
string_triple_quote: STRING_LITERAL_LONG_SINGLE_QUOTE | STRING_LITERAL_LONG_QUOTE;

iri: turtleIri;

turtleIri:
    ABSOLUTEIRIREF #fullIri
    | RELATIVEIRIREF #relativeIri
    | PNAME_LN #prefixedIri
    | PNAME_NS #iriPrefix
    ;

blankNode: BLANK_NODE_LABEL #namedBlankNode 
    | ANON #anonymousBlankNode
    ;

reifier: '~' (iri | blankNode);

subjectOrReifiedTriple: subject | reifiedTriple;

reifiedTriple: '<<' subjectOrReifiedTriple predicate rdfobject reifier? '>>';

tripleTerm: '<<(' ttSubject predicate ttObject ')>>';

ttSubject: iri | blankNode;

ttObject: iri | blankNode | literal | tripleTerm ;

annotation: (reifier | ('{|' predicateObjectList '|}'))*;


ABSOLUTEIRIREF: '<' URI_SCHEMES  (~[<>"{}|^`\\] | UCHAR)* '>';

URI_SCHEMES: 'http://'|'https://'|'mailto:';

RELATIVEIRIREF: '<' (~[<>"{}|^`\\] | UCHAR)* '>';

PNAME_NS: PN_PREFIX? COLON;

PNAME_LN: PNAME_NS PN_LOCAL;

BLANK_NODE_LABEL: '_:' (PN_CHARS_U | [0-9]) ((PN_CHARS | '.')* PN_CHARS)?;

LANG_DIR : '@' [a-zA-Z]+ ('-' [a-zA-Z0-9]+)* ('--' [a-zA-Z]+)? ;

INTEGER: [+-]? [0-9]+;

DECIMAL: [+-]? [0-9]* '.' [0-9]+;

DOUBLE: [+-]? (([0-9]+ '.' [0-9]* EXPONENT) | ('.' [0-9]+ EXPONENT) | ([0-9]+ EXPONENT));

EXPONENT: [eE] [+-]? [0-9]+;


STRING_LITERAL_LONG_SINGLE_QUOTE: '\'\'\'' ( ('\'' | '\'\'')? (~['\\] | ECHAR | UCHAR) )* '\'\'\'' ;

STRING_LITERAL_LONG_QUOTE: '"""' ( ('"' | '""')? (~["\\] | ECHAR | UCHAR) )* '"""' ;

STRING_LITERAL_QUOTE: '"' (~["\\\n\r] | ECHAR | UCHAR)* '"' ;

STRING_LITERAL_SINGLE_QUOTE: '\'' (~['\\\n\r] | ECHAR | UCHAR)* '\'' ;


UCHAR: '\\u' HEX HEX HEX HEX | '\\U' HEX HEX HEX HEX HEX HEX HEX HEX;

ECHAR: '\\' [tbnrf\\"'];

WS: [ \t\r\n]+ -> skip;
ANON: '[' WS* ']';

PN_CHARS_BASE: [A-Z]
               	| 	[a-z]
               	| 	[\u00C0-\u00D6]
               	| 	[\u00D8-\u00F6]
               	| 	[\u00F8-\u02FF]
               	| 	[\u0370-\u037D]
               	| 	[\u037F-\u1FFF]
               	| 	[\u200C-\u200D]
               	| 	[\u2070-\u218F]
               	| 	[\u2C00-\u2FEF]
               	| 	[\u3001-\uD7FF]
               	| 	[\uF900-\uFDCF]
               	| 	[\uFDF0-\uFFFD]
               	| 	SUPPLEMENTARY_PLANE ;
               	
fragment SUPPLEMENTARY_PLANE : [\uD800-\uDBFF][\uDC00-\uDFFF];
               	
PN_CHARS_U: PN_CHARS_BASE | '_';

PN_CHARS: PN_CHARS_U
               	| 	'-'
               	| 	[0-9]
               	| 	[\u00B7]
               	| 	[\u0300-\u036F]
               	| 	[\u203F-\u2040];

PN_PREFIX: PN_CHARS_BASE ((PN_CHARS | PERIOD)* PN_CHARS)?;

PN_LOCAL: (PN_CHARS_U | COLON | [0-9] | PLX) ((PN_CHARS | PERIOD | COLON | PLX)* (PN_CHARS | COLON | PLX ))?;

PLX: PERCENT | PN_LOCAL_ESC;

PERCENT: '%' HEX HEX;

HEX: [0-9] | [A-F] | [a-f];

PN_LOCAL_ESC: '\\' ('_' | '~' | '.' | '-' | '!' | '$' | '&' | '\'' | '(' | ')' | '*' | '+' | ',' | ';' | '=' | '/' | '?' | '#' | '@' | '%');

PERIOD: '.';

COMMENT: '#' ~[\r\n]* [\r\n]+ -> skip;

COLON : ':' ;

SEMICOLON : ';' ;

COMMA : ',' ;

RSQPAREN : ']' ;

LSQPAREN : '[' ;
