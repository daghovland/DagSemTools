grammar Manchester;
import IriGrammar, Concept, ManchesterCommonTokens;

ontologyDocument : prefixDeclaration* ontology EOF;

ontology: ONTOLOGYTOKEN rdfiri? rdfiri? ;
prefixDeclaration: PREFIXTOKEN PREFIXNAME IRI;

