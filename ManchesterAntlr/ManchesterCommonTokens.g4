lexer grammar ManchesterCommonTokens;

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
LT: '<';
GT: '>';
COMMA: ',';
WHITESPACE : [ \t\r\n]+ -> skip ;
NEWLINE : '\n'  | '\r' '\n';
