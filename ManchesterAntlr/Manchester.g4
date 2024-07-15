grammar Manchester;
import IriGrammar;

ontologyDocument : prefixDeclaration* ontology EOF;

ontology: ONTOLOGYTOKEN rdfiri rdfiri? ;
prefixDeclaration: PREFIXTOKEN;

rdfiri: fullIri;
fullIri: '<' IRI '>';

// Lexer
ONTOLOGYTOKEN: 'Ontology:';
PREFIXTOKEN: 'Prefix:';

DIGIT   : [0-9];
WHITESPACE : [ \t\r\n]+ -> skip ;
NEWLINE : '\n'  | '\r' '\n';