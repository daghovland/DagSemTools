grammar Manchester;
import IriGrammar, Concept, ManchesterLexer;

ontologyDocument : prefixDeclaration* ontology EOF;

ontology: ONTOLOGYTOKEN rdfiri? rdfiri? ;
prefixDeclaration: PREFIXTOKEN PREFIXNAME IRI;

