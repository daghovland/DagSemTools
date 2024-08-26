/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

lexer grammar CommonTokens;

INTEGER: 'integer';
DECIMAL: 'decimal';
FLOAT: 'float';
STRING: 'string';


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

WS: [\r\n \t]+ -> channel(1);

LANGUAGETAG : '@' [a-zA-Z]+ ('-' [a-zA-Z0-9]+)*;
QUOTEDSTRING: '"' (~["\\] | '\\')* '"' ;
EXPONENT : ('e' | 'E') ('+' | '-')? DIGITS;
DECIMALLITERAL : ('+' | '-')? DIGITS '.' DIGITS;
INTEGERLITERAL : ('+' | '-')? DIGITS;
// Integers also match floating point, so this rule has to be after INTEGERLITERAL
FLOATINGPOINTLITERAL :  ('+' | '-')? ( DIGITS ( '.' DIGITS) ? (EXPONENT)? ) | ( '.' DIGITS (EXPONENT)?) ( 'f' | 'F' );
DIGITS: [0-9]+;

