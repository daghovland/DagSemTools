lexer grammar ManchesterCommonTokens;

INVERSE: 'inverse';
THAT: 'that';
ONTOLOGYTOKEN: 'Ontology:';
PREFIXTOKEN: 'Prefix:';
CLASS: 'Class:';
INDIVIDUAL: 'Individual:';
DATATYPE: 'Datatype:' ;
OBJECTPROPERTY: 'ObjectProperty:';
ANNOTATIONPROPERTY: 'AnnotationProperty:';
ANNOTATIONS: 'Annotations:';
IMPORT: 'Import:';
SUBCLASSOF: 'SubClassOf:';
EQUIVALENTTO: 'EquivalentTo:';
SUBPROPERTYOF : 'SubPropertyOf:' ;
INVERSEOF : 'InverseOf:' ;
DOMAIN : 'Domain:' ;
RANGE : 'Range:' ;

NOT: 'not';
AND: 'and';
OR: 'or';
SOME: 'some';
ONLY: 'only';

LT: '<';
GT: '>';
LPAREN: '(';
RPAREN: ')';
COMMA: ',';
NEWLINE: [\r\n]+ -> skip;
WHITESPACE : [ \t]+  -> skip ;

STRING : '"' ~["]+ '"';