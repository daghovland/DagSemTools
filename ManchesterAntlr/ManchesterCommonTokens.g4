lexer grammar ManchesterCommonTokens;

INVERSE: 'inverse';
THAT: 'that';
ONTOLOGYTOKEN: 'Ontology:';
PREFIXTOKEN: 'Prefix:';
CLASS: 'Class:';
INDIVIDUAL: 'Individual:';
OBJECTPROPERTY: 'ObjectProperty:';
ANNOTATIONS: 'Annotations:';
IMPORT: 'Import:';
SUBCLASSOF: 'SubClassOf:';
EQUIVALENTTO: 'EquivalentTo:';
SUBPROPERTYOF : 'SubPropertyOf:' ;
INVERSEOF : 'InverseOf:' ;
DOMAIN : 'Domain:' ;
RANGE : 'Range:' ;

LT: '<';
GT: '>';
COMMA: ',';
NEWLINE: [\r\n]+ -> skip;
WHITESPACE : [ \t]+  -> skip ;
