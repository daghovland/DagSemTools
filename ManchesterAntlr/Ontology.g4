grammar Ontology;
import Concept;

start: prefixDeclaration* ontology EOF;

prefixDeclaration: PREFIXTOKEN prefixName rdfiri;

ontology: ONTOLOGYTOKEN ;