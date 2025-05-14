grammar TriGDoc;
import TurtleResource;

trigDoc: (directive | block)*;

block 	: 	triplesOrGraph | wrappedGraph | triples2 | ('GRAPH' labelOrSubject wrappedGraph);

triplesOrGraph 	: 	(labelOrSubject (wrappedGraph | (predicateObjectList '.'))) | (reifiedTriple predicateObjectList? '.');
triples2 	: 	(blankNodePropertyList predicateObjectList? '.') | (collection predicateObjectList '.');
wrappedGraph 	: 	'{' triplesBlock? '}';
triplesBlock 	: 	triples ('.' triplesBlock?)?;
labelOrSubject 	: 	iri | blankNode;