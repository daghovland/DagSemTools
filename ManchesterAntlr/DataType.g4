grammar DataType;
import ManchesterCommonTokens, IriGrammar;


dataRange: dataConjunction #SingleDataDisjunction
    | (dataConjunction OR dataRange) #DisjunctionDataRange
    ;
 
dataConjunction: dataPrimary#SingleDataConjunction
    | dataPrimary AND dataConjunction #ActualDataRangeConjunction
    ;
    
dataPrimary: dataAtomic #PositiveDataPrimary
    | NOT dataAtomic #NegativeDataPrimary
    ;
    
dataAtomic : datatype #DataTypeAtomic
    | '{' literal (COMMA literal)* '}' #LiteralSet
    | LPAREN dataRange RPAREN #DataRangeParenthesis
    | datatype LSQUARE datatype_restriction (COMMA datatype_restriction)* RSQUARE #DatatypeRestriction
    ;
    
datatype_restriction: facet literal;

facet: LENGTH #facetLength
    | MINLENGTH #facetMinLength
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
