lexer grammar ManchesterCommonTokens;

INVERSE: 'inverse';
THAT: 'that';
ONTOLOGYTOKEN: 'Ontology:';
PREFIXTOKEN: 'Prefix:';
CLASS: 'Class:';
INDIVIDUAL: 'Individual:';
ANNOTATIONS: 'Annotations:';
IMPORT: 'Import:';
SUBCLASSOF: 'SubClassOf:';
EQUIVALENTTO: 'EquivalentTo:';
LT: '<';
GT: '>';
COMMA: ',';
NEWLINE: [\r\n]+ -> skip;
WHITESPACE : [ \t]+  -> skip ;
