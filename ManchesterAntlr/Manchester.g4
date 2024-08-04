grammar Manchester;
import IriGrammar, Concept, ManchesterCommonTokens;

ontologyDocument : prefixDeclaration* ontology EOF;

ontology: ONTOLOGYTOKEN rdfiri? rdfiri? frame* ;
prefixDeclaration: 
    PREFIXTOKEN prefixName ':' '<' IRI '>' #nonEmptyprefixDeclaration
    | PREFIXTOKEN ':' '<' IRI '>' #emptyPrefix
    ;
    
frame: 
    CLASS rdfiri annotatedList * #ClassFrame
    | INDIVIDUAL rdfiri individualFrameList* #IndividualFrame
    | OBJECTPROPERTY rdfiri objectPropertyFrameList* #ObjectPropertyFrame
    ;

annotatedList: 
    SUBCLASSOF descriptionAnnotatedList  #SubClassOf
    | EQUIVALENTTO descriptionAnnotatedList #EquivalentTo  
    ;

individualFrameList:
    'Types:' descriptionAnnotatedList #Types
    | 'Facts:' factAnnotatedList #Facts
    ;
    
objectPropertyFrameList:
    SUBPROPERTYOF objectPropertyExpressionAnnotatedList #SubPropertyOf
    | EQUIVALENTTO objectPropertyExpressionAnnotatedList #PropertyEquivalentTo
    | INVERSEOF objectPropertyExpressionAnnotatedList #InverseOf
    | DOMAIN descriptionAnnotatedList #Domain
    | RANGE descriptionAnnotatedList #Range
    ;

objectPropertyExpressionAnnotatedList: objectPropertyExpression (COMMA objectPropertyExpression)* ;
objectPropertyExpression: rdfiri 
| INVERSE rdfiri;

descriptionAnnotatedList: description (COMMA description)* ;

factAnnotatedList: fact (COMMA fact)* ;
fact: rdfiri rdfiri;