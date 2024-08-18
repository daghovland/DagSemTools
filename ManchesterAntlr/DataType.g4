grammar DataType;
import ManchesterCommonTokens, IriGrammar;


dataRange: dataConjunction (OR dataConjunction)* ;
dataConjunction: dataPrimary (AND dataPrimary)* ;
dataPrimary: dataAtomic #PositiveDataPrimary
    | NOT dataAtomic #NegativeDataPrimary
    ;
    
dataAtomic : datatype #DataTypeAtomic
    | '{' literal (COMMA literal)* '}' #LiteralSet
    | LPAREN dataRange RPAREN #DataRangeParenthesis
    | datatype LSQUARE facet literal (COMMA facet literal)* RSQUARE #DatatypeRestriction
    ;
    
facet: LENGTH #facetLength
    | MINLENGTH #faetMinLenght
    | MAXLENGTH #facetMaxLength
    | PATTERN #facetPattern
    | LANGRANGE #facetLangRange
    | LT #facetLessThan
    | GT #facetGreaterThan
    | LTE #facetLessThanEqual
    | GTE #facetGreaterThanEqual
    ; 

literal : QUOTEDSTRING '^^' datatype #typedLiteral 
    | QUOTEDSTRING #stringLiteralNoLanguage 
    | QUOTEDSTRING LANGUAGETAG #stringLiteralWithLanguage 
    | INTEGERLITERAL #integerLiteral 
    | DECIMALLITERAL #decimalLiteral
    | FLOATINGPOINTLITERAL #floatingPointLiteral
    ;
     
    
datatype:  INTEGER #DatatypeInteger
    | DECIMAL #DatatypeDecimal
    | FLOAT #DatatypeFloat
    | STRING #DatatypeString
    | rdfiri #DatatypeIri
    ;

LANGUAGETAG : '@' [a-zA-Z]+ ('-' [a-zA-Z0-9]+)*;
QUOTEDSTRING: '"' (~["\\] | '\\')* '"' ;
FLOATINGPOINTLITERAL : ('+' | '-')? ( DIGITS ( '.' DIGITS) ? (EXPONENT)? ) | ( '.' DIGITS (EXPONENT)?) ( 'f' | 'F' );
EXPONENT : ('e' | 'E') ('+' | '-')? DIGITS;
DECIMALLITERAL : ('+' | '-')? DIGITS '.' DIGITS;
INTEGERLITERAL : ('+' | '-')? DIGITS;
DIGITS: [0-9]+;

