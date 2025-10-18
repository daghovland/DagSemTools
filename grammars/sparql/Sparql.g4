/*
    Copyright © 2008-2025 World Wide Web Consortium. W3C
    I do not claim copyright for the trivial translation into Antlr syntax
*/

grammar Sparql;
import SparqlTokens, TurtleResource;

queryUnit          :    query ;
query             :    prologue ( selectQuery | constructQuery | describeQuery | askQuery ) valuesClause;
prologue          :    ( directive | versionDecl )* ;
versionDecl       :    'VERSION' versionSpecifier;
versionSpecifier  :    string_single_quote;
selectQuery       :    selectClause datasetClause* whereClause solutionModifier;
subSelect         :    selectClause whereClause solutionModifier valuesClause;
selectClause      :    'SELECT' ( 'DISTINCT' | 'REDUCED' )? ( projection + | '*' );
projection        :    
    var #variableName 
    | ( '(' expression 'AS' var ')' ) #variableAlias
    ;
constructQuery    :    'CONSTRUCT' ( constructTemplate datasetClause* whereClause solutionModifier | datasetClause* 'WHERE' '{' triplesTemplate? '}' solutionModifier );
describeQuery     :    'DESCRIBE' ( varOrIri+ | '*' ) datasetClause* whereClause? solutionModifier;
askQuery          :    'ASK' datasetClause* whereClause solutionModifier;
datasetClause     :    'FROM' ( defaultGraphClause | namedGraphClause );
defaultGraphClause:    sourceSelector;
namedGraphClause  :    'NAMED' sourceSelector;
sourceSelector    :    iri;
whereClause       :    'WHERE' ? groupGraphPattern;
solutionModifier  :    groupClause? havingClause? orderClause? limitOffsetClauses? ;
groupClause       :    GROUPBY groupCondition+;
groupCondition    :    builtInCall | functionCall | LPAREN expression ( AS var )? RPAREN | var;
havingClause      :    'HAVING' havingCondition+;
havingCondition   :    constraint;
orderClause       :    ORDERBY orderCondition+;
orderCondition    :    ( ( ASC | DESC ) brackettedExpression ) | ( constraint | var );
limitOffsetClauses:    limitClause offsetClause? | offsetClause limitClause?;
limitClause       :    LIMIT INTEGER;
offsetClause      :    OFFSET INTEGER;
valuesClause      :    ( VALUES dataBlock )?;
graphOrDefault    :    DEFAULT | GRAPH ? iri;
graphRef          :    GRAPH iri;
graphRefAll       :    graphRef | DEFAULT | NAMED | ALL;
quadPattern       :    '{' quads '}';
quadData          :    '{' quads '}';
quads             :    triplesTemplate? ( quadsNotTriples PERIOD ? triplesTemplate? )*;
quadsNotTriples   :    GRAPH varOrIri '{' triplesTemplate? '}';
triplesTemplate   :    triplesSameSubject ( '.' triplesTemplate? )?;
groupGraphPattern :    '{' ( subSelect | groupGraphPatternSub ) '}';
groupGraphPatternSub: triplesBlock? ( graphPatternNotTriples '.'? triplesBlock? )*;
triplesBlock      :    triplesSameSubjectPath ( '.' triplesBlock? )?;
reifiedTripleBlock:    reifiedTriple propertyList;
reifiedTripleBlockPath: reifiedTriple propertyListPath;
graphPatternNotTriples: groupOrUnionGraphPattern | optionalGraphPattern | minusGraphPattern | graphGraphPattern | serviceGraphPattern | filter | bind | inlineData;
optionalGraphPattern: OPTIONAL groupGraphPattern;
graphGraphPattern :    'GRAPH' varOrIri groupGraphPattern;
serviceGraphPattern: 'SERVICE' 'SILENT'? varOrIri groupGraphPattern;
bind              :    'BIND' '(' expression 'AS' var ')';
inlineData        :    'VALUES' dataBlock;
dataBlock         :    inlineDataOneVar | inlineDataFull;
inlineDataOneVar  :    var '{' dataBlockValue* '}';
inlineDataFull    :    ( NIL | '(' var* ')' ) '{' ( '(' dataBlockValue* ')' | NIL )* '}';
dataBlockValue    :    iri | rdfLiteral | numericLiteral | booleanLiteral | 'UNDEF' | tripleTermData;
reifier           :    '~' varOrReifierId?;
varOrReifierId    :    var | iri | blankNode;
minusGraphPattern :    'MINUS' groupGraphPattern;
groupOrUnionGraphPattern: groupGraphPattern ( 'UNION' groupGraphPattern )*;
filter            :    'FILTER' constraint;
constraint        :    brackettedExpression | builtInCall | functionCall;
functionCall      :    iri argList;
argList           :    NIL | '(' 'DISTINCT'? expression ( ',' expression )* ')';
expressionList    :    NIL | '(' expression ( ',' expression )* ')';
constructTemplate :    '{' constructTriples? '}';
constructTriples  :    triplesSameSubject ( '.' constructTriples? )?;
triplesSameSubject:    varOrTerm propertyListNotEmpty | triplesNode propertyList | reifiedTripleBlock;
propertyList      :    propertyListNotEmpty?;
propertyListNotEmpty: verb objectList ( ';' ( verb objectList )? )*;
verb              :    varOrIri | 'a';
objectList        :    tripleObject ( ',' tripleObject )*;
tripleObject            :    graphNode annotation;
triplesSameSubjectPath: 
      varOrTerm propertyListPathNotEmpty #NamedSubjectTriplesPath 
    | triplesNodePath propertyListPath #TriplesPathProperty 
    | reifiedTripleBlockPath #ReifiedTripleBlockPathPattern;

propertyListPath  :    propertyListPathNotEmpty?;
propertyListPathNotEmpty: propertyPath ( ';' ( propertyPath )? )*;
propertyPath      :    
    verbPath objectListPath #VerbPathObjectList
   | verbSimple objectListPath #VerbSimpleObjectList ;
verbPath          :    path;
verbSimple        :    var;
objectListPath    :    objectPath ( ',' objectPath )*;
objectPath        :    graphNodePath annotationPath;
path              :    pathAlternative;
pathAlternative   :    pathSequence ( '|' pathSequence )*;
pathSequence      :    pathEltOrInverse ( '/' pathEltOrInverse )*;
pathElt           :    pathPrimary pathMod?;
pathEltOrInverse  :    pathElt | '^' pathElt;
pathMod           :    '?' | '*' | '+';
pathPrimary       :    iri | 'a' | '!' pathNegatedPropertySet | '(' path ')';
pathNegatedPropertySet: pathOneInPropertySet | '(' ( pathOneInPropertySet ( '|' pathOneInPropertySet )* )? ')';
pathOneInPropertySet: iri | 'a' | '^' ( iri | 'a' );
triplesNode       :    collection | blankNodePropertyList;
blankNodePropertyList: '[' propertyListNotEmpty ']';
triplesNodePath   :    collectionPath | blankNodePropertyListPath;
blankNodePropertyListPath: '[' propertyListPathNotEmpty ']';
collection        :    '(' graphNode+ ')';
collectionPath    :    '(' graphNodePath+ ')';
annotationPath    :    ( reifier | annotationBlockPath )*;
annotationBlockPath: '{|' propertyListPathNotEmpty '|}';
annotation        :    ( reifier | annotationBlock )*;
annotationBlock   :    '{|' propertyListNotEmpty '|}';
graphNode         :    varOrTerm | triplesNode | reifiedTriple;
graphNodePath     :    varOrTerm | triplesNodePath | reifiedTriple;
varOrTerm         :    var | iri | rdfLiteral | numericLiteral | booleanLiteral | blankNode | NIL | tripleTerm;
reifiedTriple     :    '<<' reifiedTripleSubject verb reifiedTripleObject reifier? '>>';
reifiedTripleSubject: var | iri | rdfLiteral | numericLiteral | booleanLiteral | blankNode | reifiedTriple;
reifiedTripleObject: var | iri | rdfLiteral | numericLiteral | booleanLiteral | blankNode | reifiedTriple | tripleTerm;
tripleTerm        :    '<<(' tripleTermSubject verb tripleTermObject ')>>';
tripleTermSubject :    var | iri | rdfLiteral | numericLiteral | booleanLiteral | blankNode;
tripleTermObject  :    var | iri | rdfLiteral | numericLiteral | booleanLiteral | blankNode | tripleTerm;
tripleTermData    :    '<<(' tripleTermDataSubject ( iri | 'a' ) tripleTermDataObject ')>>';
tripleTermDataSubject: iri | rdfLiteral | numericLiteral | booleanLiteral;
tripleTermDataObject: iri | rdfLiteral | numericLiteral | booleanLiteral | tripleTermData;
varOrIri          :    var | iri;
var               :    VAR1 | VAR2;
expression        :    conditionalOrExpression;
conditionalOrExpression: conditionalAndExpression ( '||' conditionalAndExpression )*;
conditionalAndExpression: valueLogical ( '&&' valueLogical )*;
valueLogical      :    relationalExpression;
relationalExpression: numericExpression ( '=' numericExpression | '!=' numericExpression | '<' numericExpression | '>' numericExpression | '<=' numericExpression | '>=' numericExpression | 'IN' expressionList | 'NOT' 'IN' expressionList )?;
numericExpression :    additiveExpression;
additiveExpression:    multiplicativeExpression ( '+' multiplicativeExpression | '-' multiplicativeExpression | ( numericLiteral ) ( ( '*' unaryExpression ) | ( '/' unaryExpression ) )* )*;
multiplicativeExpression: unaryExpression ( '*' unaryExpression | '/' unaryExpression )*;
unaryExpression   :    '!' primaryExpression | '+' primaryExpression | '-' primaryExpression | primaryExpression;
primaryExpression :    brackettedExpression | builtInCall | iriOrFunction | rdfLiteral | numericLiteral | booleanLiteral | var | exprTripleTerm;
exprTripleTerm    :    '<<(' exprTripleTermSubject verb exprTripleTermObject ')>>';
exprTripleTermSubject: iri | rdfLiteral | numericLiteral | booleanLiteral | var;
exprTripleTermObject: iri | rdfLiteral | numericLiteral | booleanLiteral | var | exprTripleTerm;
brackettedExpression: '(' expression ')';
builtInCall       :    aggregate | 'STR' '(' expression ')' | 'LANG' '(' expression ')' | 'LANGMATCHES' '(' expression ',' expression ')' | 'LANGDIR' '(' expression ')' | 'DATATYPE' '(' expression ')' | 'BOUND' '(' var ')' | 'IRI' '(' expression ')' | 'URI' '(' expression ')' | 'BNODE' ( '(' expression ')' | NIL ) | 'RAND' NIL | 'ABS' '(' expression ')' | 'CEIL' '(' expression ')' | 'FLOOR' '(' expression ')' | 'ROUND' '(' expression ')' | 'CONCAT' expressionList | substringExpression | 'STRLEN' '(' expression ')' | strReplaceExpression | 'UCASE' '(' expression ')' | 'LCASE' '(' expression ')' | 'ENCODE_FOR_URI' '(' expression ')' | 'CONTAINS' '(' expression ',' expression ')' | 'STRSTARTS' '(' expression ',' expression ')' | 'STRENDS' '(' expression ',' expression ')' | 'STRBEFORE' '(' expression ',' expression ')' | 'STRAFTER' '(' expression ',' expression ')' | 'YEAR' '(' expression ')' | 'MONTH' '(' expression ')' | 'DAY' '(' expression ')' | 'HOURS' '(' expression ')' | 'MINUTES' '(' expression ')' | 'SECONDS' '(' expression ')' | 'TIMEZONE' '(' expression ')' | 'TZ' '(' expression ')' | 'NOW' NIL | 'UUID' NIL | 'STRUUID' NIL | 'MD5' '(' expression ')' | 'SHA1' '(' expression ')' | 'SHA256' '(' expression ')' | 'SHA384' '(' expression ')' | 'SHA512' '(' expression ')' | 'COALESCE' expressionList | 'IF' '(' expression ',' expression ',' expression ')' | 'STRLANG' '(' expression ',' expression ')' | 'STRLANGDIR' '(' expression ',' expression ',' expression ')' | 'STRDT' '(' expression ',' expression ')' | 'sameTerm' '(' expression ',' expression ')' | 'isIRI' '(' expression ')' | 'isURI' '(' expression ')' | 'isBLANK' '(' expression ')' | 'isLITERAL' '(' expression ')' | 'isNUMERIC' '(' expression ')' | 'hasLANG' '(' expression ')' | 'hasLANGDIR' '(' expression ')' | regexExpression | existsFunc | notExistsFunc | 'isTRIPLE' '(' expression ')' | 'TRIPLE' '(' expression ',' expression ',' expression ')' | 'SUBJECT' '(' expression ')' | 'PREDICATE' '(' expression ')' | 'OBJECT' '(' expression ')';
regexExpression   :    'REGEX' '(' expression ',' expression ( ',' expression )? ')';
substringExpression: 'SUBSTR' '(' expression ',' expression ( ',' expression )? ')';
strReplaceExpression: 'REPLACE' '(' expression ',' expression ',' expression ( ',' expression )? ')';
existsFunc        :    'EXISTS' groupGraphPattern;
notExistsFunc     :    'NOT' 'EXISTS' groupGraphPattern;
aggregate         :    'COUNT' '(' 'DISTINCT'? ( '*' | expression ) ')' | 'SUM' '(' 'DISTINCT'? expression ')' | 'MIN' '(' 'DISTINCT'? expression ')' | 'MAX' '(' 'DISTINCT'? expression ')' | 'AVG' '(' 'DISTINCT'? expression ')' | 'SAMPLE' '(' 'DISTINCT'? expression ')' | 'GROUP_CONCAT' '(' 'DISTINCT'? expression ( ';' 'SEPARATOR' '=' stringLiteral )? ')';
iriOrFunction     :    iri argList?;
