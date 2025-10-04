/*
    Copyright © 2008-2025 World Wide Web Consortium. W3C
    I do not claim copyright for the trivial translation into Antlr syntax
*/

lexer grammar SparqlTokens;

VAR1: '?' VARNAME;
VAR2: '$' VARNAME;
WHERE: 'WHERE';
GROUPBY: 'GROUP BY';
ORDERBY: 'ORDER BY';
GRAPH: 'GRAPH';
ASC: 'ASC';
DESC: 'DESC';
LIMIT: 'LIMIT';
OFFSET: 'OFFSET';
VALUES: 'VALUES';
DEFAULT: 'DEFAULT';
NAMED: 'NAMED';
ALL: 'ALL';
OPTIONAL: 'OPTIONAL';
AS: 'AS';

fragment PN_CHARS_BASE: [A-Za-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u02FF\u0370-\u037D\u037F-\u1FFF\u200C-\u200D\u2070-\u218F\u2C00-\u2FEF\u3001-\uD7FF\uF900-\uFDCF\uFDF0-\uFFFD]
                     | [\uD800-\uDBFF][\uDC00-\uDFFF]; // surrogate pairs for UTF-16

fragment PN_CHARS_U: PN_CHARS_BASE | '_';
fragment VARNAME: (PN_CHARS_U | [0-9]) (PN_CHARS_U | [0-9] | '\u00B7' | [\u0300-\u036F] | [\u203F-\u2040])*;
fragment PN_CHARS: PN_CHARS_U | '-' | [0-9] | '\u00B7' | [\u0300-\u036F] | [\u203F-\u2040];
