grammar TriGDoc;
import TurtleResource;

trigDoc: (directive | block)* EOF;

block 	: 	
    triplesOrGraph #BlockTriplesOrGraph 
    | wrappedGraph  #BlockDefaultWrappedGraph
    | triples2 #BlockTriples2
    | ('GRAPH' labelOrSubject wrappedGraph) #BlockNamedWrappedGraph
    ;

triplesOrGraph 	: 	
    labelOrSubject wrappedGraph #NamedWrappedGraph
    | labelOrSubject predicateObjectList '.' #LabelOrSubjectTriples
    | reifiedTriple predicateObjectList? '.'#ReifiedTripleObjectList
    ;

triples2 	: 	
    (blankNodePropertyList predicateObjectList? '.') #BlankNodeTriples2
    | (collection predicateObjectList '.') #CollectionTriples2
    ;

wrappedGraph 	: 	'{' triplesBlock? '}';

triplesBlock 	: 	triples ('.' triplesBlock?)?;

labelOrSubject 	: 	iri | blankNode;