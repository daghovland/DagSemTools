/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

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
