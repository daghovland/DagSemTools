/*
    Copyright © 2008-2025 World Wide Web Consortium. W3C
    I do not claim copyright for the trivial translation into Antlr syntax
*/

lexer grammar SparqlTokens;

IRIREF: '<' (~[<>"{}|^`\\\u0000-\u0020])* '>';
PNAME_NS: PN_PREFIX? ':';
PNAME_LN: PNAME_NS PN_LOCAL;
BLANK_NODE_LABEL: '_:' (PN_CHARS_U | [0-9]) ((PN_CHARS|'.')* PN_CHARS)?;
VAR1: '?' VARNAME;
VAR2: '$' VARNAME;
LANG_DIR: '@' [a-zA-Z]+ ('-' [a-zA-Z0-9]+)* ('--' [a-zA-Z]+)?;
INTEGER: [0-9]+;
DECIMAL: [0-9]* '.' [0-9]+;
DOUBLE: [0-9]+ '.' [0-9]* EXPONENT | '.' [0-9]+ EXPONENT | [0-9]+ EXPONENT;
INTEGER_POSITIVE: '+' INTEGER;
DECIMAL_POSITIVE: '+' DECIMAL;
DOUBLE_POSITIVE: '+' DOUBLE;
INTEGER_NEGATIVE: '-' INTEGER;
DECIMAL_NEGATIVE: '-' DECIMAL;
DOUBLE_NEGATIVE: '-' DOUBLE;
EXPONENT: [eE] [+-]? [0-9]+;
WHERE: 'WHERE';
GROUPBY: 'GROUP BY';
ORDERBY: 'ORDER BY';
LPAREN: '(';
RPAREN: ')';
AS: 'AS';
STRING_LITERAL1: '\'' ( ~[\u0027\u005C\u000A\u000D] | ECHAR )* '\'';
STRING_LITERAL2: '"' ( ~[\u0022\u005C\u000A\u000D] | ECHAR )* '"';
STRING_LITERAL_LONG1: '\'\'\'' ( ('\'' | '\'\'')? ( ~[\'\\] | ECHAR ) )* '\'\'\'';
STRING_LITERAL_LONG2: '"""' ( ('"' | '""')? ( ~["\\] | ECHAR ) )* '"""';
ECHAR: '\\' [tbnrf\"\']; 
NIL: '(' WS* ')';
WS: [\u0020\u0009\u000D\u000A]+ -> skip;
ANON: '[' WS* ']';

fragment PN_CHARS_BASE: [A-Za-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u02FF\u0370-\u037D\u037F-\u1FFF\u200C-\u200D\u2070-\u218F\u2C00-\u2FEF\u3001-\uD7FF\uF900-\uFDCF\uFDF0-\uFFFD]
                     | [\uD800-\uDBFF][\uDC00-\uDFFF]; // surrogate pairs for UTF-16

fragment PN_CHARS_U: PN_CHARS_BASE | '_';
fragment VARNAME: (PN_CHARS_U | [0-9]) (PN_CHARS_U | [0-9] | '\u00B7' | [\u0300-\u036F] | [\u203F-\u2040])*;
fragment PN_CHARS: PN_CHARS_U | '-' | [0-9] | '\u00B7' | [\u0300-\u036F] | [\u203F-\u2040];
fragment PN_PREFIX: PN_CHARS_BASE ((PN_CHARS|'.')* PN_CHARS)?;
fragment PN_LOCAL: (PN_CHARS_U | ':' | [0-9] | PLX) ((PN_CHARS | '.' | ':' | PLX)* (PN_CHARS | ':' | PLX))?;
fragment PLX: PERCENT | PN_LOCAL_ESC;
fragment PERCENT: '%' HEX HEX;
fragment HEX: [0-9A-Fa-f];
fragment PN_LOCAL_ESC: '\\' [_~.\-!$&\'()*+,;=/?#@%];