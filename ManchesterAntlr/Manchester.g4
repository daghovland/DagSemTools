grammar Manchester;
import IriGrammar, Concept, ManchesterCommonTokens;

ontologyDocument : prefixDeclaration* ontology EOF;

ontology: ONTOLOGYTOKEN rdfiri? rdfiri? frame* ;
prefixDeclaration: PREFIXTOKEN prefixName IRI;
frame: classFrame;
classFrame: CLASS rdfiri subClassOf* ;

subClassOf: SUBCLASSOF description*;

