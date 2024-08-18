lexer grammar ManchesterCommonTokens;

INVERSE: 'inverse';
THAT: 'that';
ONTOLOGYTOKEN: 'Ontology:';
PREFIXTOKEN: 'Prefix:';
IMPORT: 'Import:';
CLASS: 'Class:';
INDIVIDUAL: 'Individual:';
DATATYPE: 'Datatype:' ;
OBJECTPROPERTY: 'ObjectProperty:';
DATAPROPERTY: 'DataProperty:';
CHARACTERISTICS: 'Characteristics:';
ANNOTATIONPROPERTY: 'AnnotationProperty:';
ANNOTATIONS: 'Annotations:';
FUNCTIONAL: 'Functional';
INVERSEFUNCTIONAL: 'InverseFunctional';
REFLEXIVE: 'Reflexive';
IRREFLEXIVE: 'Irreflexive';
ASYMMETRIC: 'Asymmetric';
TRANSITIVE: 'Transitive';
SYMMETRIC: 'Symmetric';
DISJOINTWITH: 'DisjointWith:';
SUBPROPERTYCHAIN: 'SubPropertyChain:';
SUBCLASSOF: 'SubClassOf:';
EQUIVALENTTO: 'EquivalentTo:';
SUBPROPERTYOF : 'SubPropertyOf:' ;
INVERSEOF : 'InverseOf:' ;
DOMAIN : 'Domain:' ;
RANGE : 'Range:' ;
INTEGER: 'integer';
DECIMAL: 'decimal';
FLOAT: 'float';
STRING: 'string';

LENGTH: 'length';
MINLENGTH: 'minLength';
MAXLENGTH: 'maxLength';
PATTERN: 'pattern';
LANGRANGE: 'langRange';
NOT: 'not';
AND: 'and';
OR: 'or';
SOME: 'some';
ONLY: 'only';

LT: '<';
GT: '>';
LTE: '<=';
GTE: '>=';
LPAREN: '(';
RPAREN: ')';
COMMA: ',';
LSQUARE: '[';
RSQUARE: ']';
COLON: ':';

NEWLINE: [\r\n]+ -> skip;
WHITESPACE : [ \t]+  -> skip ;


