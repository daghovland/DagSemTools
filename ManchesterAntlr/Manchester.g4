grammar Manchester;
import ManchesterCommonTokens, IriGrammar, DataType, Concept;

ontologyDocument : prefixDeclaration* ontology EOF;

ontology: ONTOLOGYTOKEN rdfiri? rdfiri? importDeclaration* annotations* frame* ;
prefixDeclaration: 
    PREFIXTOKEN prefixName=LOCALNAME COLON LT IRI GT #nonEmptyprefixDeclaration
    | PREFIXTOKEN COLON LT IRI GT #emptyPrefix
    ;
        
importDeclaration: IMPORT rdfiri ;
  
frame: 
    CLASS rdfiri annotatedList* #ClassFrame
    | INDIVIDUAL rdfiri individualFrameList* #IndividualFrame
    | OBJECTPROPERTY rdfiri objectPropertyFrameList* #ObjectPropertyFrame
    | DATATYPE rdfiri annotations? (EQUIVALENTTO annotations? dataRange)? annotations?  #DatatypeFrame
    | DATAPROPERTY rdfiri dataPropertyFrameList* #DataPropertyFrame
    | ANNOTATIONPROPERTY rdfiri annotations* #AnnotationPropertyFrame
    ;

annotatedList: 
    SUBCLASSOF descriptionAnnotatedList  #SubClassOf
    | EQUIVALENTTO descriptionAnnotatedList #EquivalentTo
    | annotations #DatatypeAnnotations  
    ;

individualFrameList:
    'Types:' descriptionAnnotatedList #IndividualTypes
    | 'Facts:' factAnnotatedList #IndividualFacts
    | annotations #IndividualAnnotations
    ;
    
objectPropertyFrameList:
    SUBPROPERTYOF objectPropertyExpressionAnnotatedList #SubPropertyOf
    | EQUIVALENTTO objectPropertyExpressionAnnotatedList #PropertyEquivalentTo
    | INVERSEOF objectPropertyExpressionAnnotatedList #InverseOf
    | DOMAIN descriptionAnnotatedList #Domain
    | RANGE descriptionAnnotatedList #Range
    | CHARACTERISTICS objectPropertyCharacteristicAnnotatedList #Characteristics
    | DISJOINTWITH descriptionAnnotatedList #DisjointWith
    | SUBPROPERTYCHAIN annotations? objectPropertyExpression ('o' objectPropertyExpression)+ #SubPropertyChain
    | annotations #PropertyAnnotations
    ;

dataPropertyFrameList:
    SUBPROPERTYOF objectPropertyExpressionAnnotatedList #SubDataPropertyOf
    | EQUIVALENTTO objectPropertyExpressionAnnotatedList #DataPropertyEquivalentTo
    | DOMAIN descriptionAnnotatedList #DataPropertyDomain
    | RANGE dataRangeAnnotatedList #DataPropertyRange
    | CHARACTERISTICS objectPropertyCharacteristicAnnotatedList #DataPropertyCharacteristics
    | DISJOINTWITH descriptionAnnotatedList #DataPropertyDisjointWith
    | annotations #DataPropertyAnnotations
    ;


objectPropertyCharacteristicAnnotatedList: objectPropertyCharacteristic annotations?  (COMMA annotations?  objectPropertyCharacteristic)* ;
objectPropertyCharacteristic: 
    FUNCTIONAL #Functional
    | INVERSEFUNCTIONAL #InverseFunctional
    | REFLEXIVE #Reflexive
    | IRREFLEXIVE #Irreflexive
    | ASYMMETRIC #Asymmetric
    | TRANSITIVE #Transitive
    | SYMMETRIC #Symmetric;

objectPropertyExpressionAnnotatedList: objectPropertyExpression annotations?  (COMMA annotations?  objectPropertyExpression)* ;
objectPropertyExpression: rdfiri 
| INVERSE rdfiri;

dataRangeAnnotatedList: annotations?  dataRange (COMMA annotations?  dataRange)* ;

descriptionAnnotatedList: annotations? description  (COMMA annotations?  description)* ;

factAnnotatedList: annotations?  fact (COMMA annotations?  fact)* ;

annotations: 'Annotations:' annotations? annotation (COMMA annotations? annotation)* ;
annotation: 
    rdfiri rdfiri #ObjectAnnotation
    | rdfiri literal #LiteralAnnotation
    ;

fact: role=rdfiri object=rdfiri #ObjectFact
    | property=rdfiri value=literal #LiteralFact
    ;