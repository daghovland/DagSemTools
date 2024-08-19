grammar IriGrammar; // Kind of IRIs (https://www.rfc-editor.org/rfc/rfc3987#section-5), but only the subset valid as defined in RDF-1.2 https://www.w3.org/TR/rdf12-concepts/#iri-abnf
import ManchesterCommonTokens;

rdfiri: LT IRI GT   #fullIri
      | prefixName=LOCALNAME COLON localName=LOCALNAME  #prefixedIri
      | COLON? simpleName = (LOCALNAME 
      | DECIMALLITERAL 
      | EXPONENT 
      | INTEGERLITERAL 
      | FLOATINGPOINTLITERAL  
      ) #emptyPrefixedIri // Numbers are valid localnames in Manchester syntax
      ;

localname: LOCALNAME;
integerliteral: INTEGERLITERAL;

// Lexer
WHITESPACE: [ \t\r\n] -> skip;

// According to OWL, this is the PNAME_NS from SPARQL
// Leading integers are removed, since they are matched by the INTEGERLITERAL rule
LOCALNAME:  ~([/():<>[\] \t\r\n,0123456789]) ~([/():<>[\] \t\r\n,]) *;


// This is the IRI from the new RDF-1.2 spec
IRI  :   SCHEME COLON IHIERPART  ;
SCHEME  :   'http'|'https';

// This is super-loose, but we dont really need to parse the iri structure anyway
IHIERPART  : '//' IAUTHORITY IPATH ;
IPATH : '/' ~[:<> \t\r\n,]*;
    
// Rdf-1.2 recommends to not allow ports or ip numbers in IRIs
IAUTHORITY: IHOST;
// DNS spec only allows these hosts
IHOST: VALIDHOSTNAMES;
VALIDHOSTNAMES : ( [a-z] | '.'|'-' | [0-9] ) + ;



