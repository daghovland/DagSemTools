grammar Manchester;
import IriGrammar, Concept, ManchesterCommonTokens;

ontologyDocument : prefixDeclaration* ontology EOF;

ontology: ONTOLOGYTOKEN rdfiri? rdfiri? frame* ;
prefixDeclaration: PREFIXTOKEN prefixName ':' '<' IRI '>';
frame: 
    CLASS rdfiri annotatedList * #ClassFrame
    | INDIVIDUAL rdfiri individualFrameList* #IndividualFrame
    ;

annotatedList: 
    SUBCLASSOF descriptionAnnotatedList  #SubClassOf
    | EQUIVALENTTO descriptionAnnotatedList #EquivalentTo  
    ;

individualFrameList:
    'Types:' descriptionAnnotatedList #Types
    | 'Facts:' descriptionAnnotatedList * #Facts
    ;
    
descriptionAnnotatedList: description (COMMA description)* ;