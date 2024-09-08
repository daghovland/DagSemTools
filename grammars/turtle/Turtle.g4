grammar Turtle;
import TurtleTokens;

turtleDoc : statement* EOF;

statement: directive | triples PERIOD;

directive: prefix | base ;

prefix: PREFIX_STRING PNAME_NS ABSOLUTEIRIREF  #sparqlPrefix
    | PREFIX_STRING PNAME_NS RELATIVEIRIREF  #sparqlRelativePrefix
    | ATPREFIX PNAME_NS ABSOLUTEIRIREF PERIOD #prefixId
    | ATPREFIX PNAME_NS RELATIVEIRIREF PERIOD #relativePrefixId
    ;


ATPREFIX : '@prefix' ;

base: ATBASE ABSOLUTEIRIREF PERIOD 
    | BASE_STRING ABSOLUTEIRIREF 
    ;

BASE_STRING : 'BASE' ;
    
ATBASE : '@base' ;

PREFIX_STRING : 'PREFIX' ;

triples: 
    subject predicateObjectList #NamedSubjectTriples 
    | blankNodePropertyList predicateObjectList? #BlankNodeTriples
    | reifiedTriple predicateObjectList?  #ReifiedTriples
    ;
    
predicateObjectList: verbObjectList (SEMICOLON (verbObjectList)?)*;

verbObjectList: verb rdfobject (COMMA rdfobject)*;


verb: predicate | RDF_TYPE_ABBR;

RDF_TYPE_ABBR : 'a' ;

subject: iri | blankNode | collection;

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

booleanLiteral: 'true' | 'false';

rdfLiteral: string #plainStringLiteral
    | string LANG_DIR #langLiteral
    | string '^^' iri? #typedLiteral
    ;
    

string: string_single_quote | string_triple_quote;
 
string_single_quote:  STRING_LITERAL_QUOTE  | STRING_LITERAL_SINGLE_QUOTE;
string_triple_quote: STRING_LITERAL_LONG_SINGLE_QUOTE | STRING_LITERAL_LONG_QUOTE;

iri: 
    ABSOLUTEIRIREF #fullIri
    | RELATIVEIRIREF #relativeIri
    | PNAME_LN #prefixedIri
    | PNAME_NS #iriPrefix
    ;

blankNode: BLANK_NODE_LABEL | ANON;

reifier: '~' (iri | blankNode);

reifiedTriple: '<<' (subject | reifiedTriple) predicate rdfobject reifier* '>>';

tripleTerm: '<<(' ttSubject predicate ttObject ')>>';

ttSubject: iri | blankNode;

ttObject: iri | blankNode | literal | tripleTerm ;

annotation: (reifier | ('{|' predicateObjectList '|}'))*;


