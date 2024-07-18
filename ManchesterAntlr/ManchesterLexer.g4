lexer grammar ManchesterLexer;

SOME: 'some';
ONLY: 'only';
OR: 'or';
AND: 'and';
INVERSE: 'inverse';
NOT: 'not';
THAT: 'that';

ONTOLOGYTOKEN: 'Ontology:';
PREFIXTOKEN: 'Prefix:';
CLASS: 'Class:';
ANNOTATIONS: 'Annotations:';
IMPORT: 'Import:';
SUBCLASSOF: 'SubClassOf:';
EQUIVALENTTO: 'EquivalentTo:';

LPAREN: '(';
RPAREN: ')';
LT: '<';
GT: '>';
COMMA: ',';


WHITESPACE : [ \t\r\n]+ -> skip ;
NEWLINE : '\n'  | '\r' '\n';
