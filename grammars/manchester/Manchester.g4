/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

grammar Manchester;
import ManchesterCommonTokens, CommonTokens, IriGrammar, DataType, Concept;

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
    
fact: propertyFact #PositiveFact
    | NOT propertyFact #NegativeFact
    ;
propertyFact: role=rdfiri object=rdfiri #ObjectPropertyFact 
    | property=rdfiri value=literal #DataPropertyFact
;
