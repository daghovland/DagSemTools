lexer grammar IriGrammar; // Kind of IRIs (https://www.rfc-editor.org/rfc/rfc3987#section-5), but only the subset valid as defined in RDF-1.2 https://www.w3.org/TR/rdf12-concepts/#iri-abnf

// This is the IRI from the new RDF-1.2 spec
IRI  :   SCHEME ':' IHIERPART ( '?' IQUERY )? ( '#' IFRAGMENT )?;
// This is stricter than the spec, but realistic
SCHEME  :   'http'|'https';
LOWER : [a-z]+;
IREGNAME : ( LOWER | '.'|'-' | DIGIT ) + ;
DIGIT: [0-9];

// This is super-loose, but we dont really need to parse the iri structure anyway
IHIERPART  : '//' IAUTHORITY IPATH;
IQUERY : IPATH;
IFRAGMENT: IPATH;
IPATH : [^>]+;
    
// Rdf-1.2 recommends to not allow ports or ip numbers in IRIs
IAUTHORITY: IHOST;
IHOST: IREGNAME;
ALPHA: [A-Z]|[a-z];
RESERVED: GENDELIMS | SUBDELIMS;
GENDELIMS :   [:|?#[\]@];
SUBDELIMS :   [!$&'()*+,;=];




