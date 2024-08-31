grammar Turtle;
import TurtleTokens;

turtleDoc : statement*;

statement: directive | triples PERIOD;

directive: prefixID | base | sparqlPrefix | sparqlBase;

prefixID: ATPREFIX PNAME_NS IRIREF PERIOD;

ATPREFIX : '@prefix' ;

base: ATBASE IRIREF PERIOD;

ATBASE : '@base' ;

sparqlPrefix: PREFIX_STRING PNAME_NS IRIREF ;

PREFIX_STRING : 'PREFIX' ;

sparqlBase: BASE_STRING IRIREF ;

BASE_STRING : 'BASE' ;

triples: 
    |subject predicateObjectList 
    | blankNodePropertyList predicateObjectList?
    | reifiedTriple predicateObjectList? 
    ;
    
predicateObjectList: verb objectList (SEMICOLON (verb objectList)?)*;


objectList: object (COMMA object)*;


verb: predicate | RDF_TYPE_ABBR;

RDF_TYPE_ABBR : 'a' ;

subject: iri | blankNode | collection;

predicate: iri;

object: iri | blankNode | collection | blankNodePropertyList | literal | tripleTerm | reifiedTriple ;

literal: rdfLiteral | numericLiteral | booleanLiteral;

blankNodePropertyList: LSQPAREN predicateObjectList RSQPAREN;


collection: LPAREN object* RPAREN;

RPAREN : ')' ;

LPAREN : '(' ;

numericLiteral: INTEGER | DECIMAL | DOUBLE;

booleanLiteral: 'true' | 'false';

rdfLiteral: string (LANG_DIR | '^^' iri)?;

string: STRING_LITERAL_QUOTE | STRING_LITERAL_SINGLE_QUOTE
    | STRING_LITERAL_LONG_SINGLE_QUOTE | STRING_LITERAL_LONG_QUOTE;

iri: 
    IRIREF #fullIri
    | PNAME_LN #relativeIri
    | PNAME_NS #prefixedIri
    ;

blankNode: BLANK_NODE_LABEL | ANON;

reifier: '~' (iri | blankNode);

reifiedTriple: '<<' (subject | reifiedTriple) predicate object reifier* '>>';

tripleTerm: '<<(' ttSubject predicate ttObject ')>>';

ttSubject: iri | blankNode;

ttObject: iri | blankNode | literal | tripleTerm ;

annotation: (reifier | ('{|' predicateObjectList '|}'))*;
