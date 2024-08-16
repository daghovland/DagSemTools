grammar Manchester;
import ManchesterCommonTokens, IriGrammar, Concept;

ontologyDocument : prefixDeclaration* ontology EOF;

ontology: ONTOLOGYTOKEN rdfiri? rdfiri? importDeclaration* frame* ;
prefixDeclaration: 
    PREFIXTOKEN prefixName ':' '<' IRI '>' #nonEmptyprefixDeclaration
    | PREFIXTOKEN ':' '<' IRI '>' #emptyPrefix
    ;
    
importDeclaration: 'Import:' rdfiri ;
  
frame: 
    CLASS rdfiri annotatedList * #ClassFrame
    | INDIVIDUAL rdfiri individualFrameList* #IndividualFrame
    | OBJECTPROPERTY rdfiri objectPropertyFrameList* #ObjectPropertyFrame
    | DATATYPE rdfiri annotatedList* #DatatypeFrame
    | ANNOTATIONPROPERTY rdfiri annotationPropertyFrameList* #AnnotationPropertyFrame
    ;

annotatedList: 
    SUBCLASSOF descriptionAnnotatedList  #SubClassOf
    | EQUIVALENTTO descriptionAnnotatedList #EquivalentTo  
    ;

individualFrameList:
    'Types:' descriptionAnnotatedList #Types
    | 'Facts:' factAnnotatedList #Facts
    | 'Annotations:' annotationAnnotatedList #Annotations
    ;

annotationPropertyFrameList: 
    SUBPROPERTYOF rdfiri #AnnotationSubPropertyOf
    | DOMAIN descriptionAnnotatedList #AnnotationDomain
    | RANGE descriptionAnnotatedList #AnnotationRange
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

annotationAnnotatedList: annotation (COMMA annotation)* ;
annotation: 
    rdfiri rdfiri #AnnotationIri
    | rdfiri STRING #AnnotationLiteral
    ;

fact: rdfiri rdfiri;