grammar IriGrammar; // Kind of IRIs (https://www.rfc-editor.org/rfc/rfc3987#section-5), but only the subset valid as defined in RDF-1.2 https://www.w3.org/TR/rdf12-concepts/#iri-abnf


rdfiri: '<' IRI '>'   #fullIri
      | prefixName ':' localName #prefixedIri
      | ':'? localName #emptyPrefixedIri
      ;

prefixName: LOCALNAME;
localName: LOCALNAME;

// Lexer
WHITESPACE: [ \t\r\n] -> skip;

// According to OWL, this is the PNAME_NS from SPARQL
LOCALNAME: ~([:<> \t\r\n(),]) +;


// This is the IRI from the new RDF-1.2 spec
IRI  :   SCHEME ':' IHIERPART ( '?' IQUERY )? ( '#' IFRAGMENT )? ;
// This is stricter than the spec, but realistic
SCHEME  :   'http'|'https';

LOWER : [a-z];
UPPER: [A-Z];
ALPHA : LOWER | UPPER;
DIGIT: [0-9];

// This is super-loose, but we dont really need to parse the iri structure anyway
IHIERPART  : '//' IAUTHORITY IPATH ;
IQUERY : IPATH;
IFRAGMENT: IPATH;
IPATH : ~[:<>?# \t\r\n(),]+;
    
// Rdf-1.2 recommends to not allow ports or ip numbers in IRIs
IAUTHORITY: IHOST;
// DNS spec only allows these hosts
IHOST: VALIDHOSTNAMES;
VALIDHOSTNAMES : ( LOWER | '.'|'-' | DIGIT ) + ;




