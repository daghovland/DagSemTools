(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)


(*
    Dotnet representation of OWL 2, as specified in https://www.w3.org/TR/2012/REC-owl2-syntax-20121211
*)

namespace DagSemTools.OwlOntology

open DagSemTools.Ingress
open IriTools

    type Iri = 
        | FullIri of IriReference
    
    type Individual =
        | NamedIndividual of Iri
        | AnonymousIndividual of uint32
    
    type AnnotationProperty = Iri
    type ObjectProperty = Iri
    type DataProperty = Iri
    type Datatype = Iri
    type Class = Iri
    
    type AnnotationValue =
        IndividualAnnotation of Individual
        | LiteralAnnotation of GraphElement
        | IriAnnotation of Iri
        
    type Annotation = AnnotationProperty * AnnotationValue
        
    type AnnotationAxiom =
        | AnnotationAssertion of Annotation list * AnnotationProperty * GraphElement * GraphElement
        | SubAnnotationPropertyOf of Annotation list * AnnotationProperty * AnnotationProperty
        | AnnotationPropertyDomain of Annotation list * AnnotationProperty * Iri
        | AnnotationPropertyRange of Annotation list * AnnotationProperty * Iri
        
    type DataRange =
        NamedDataRange of Datatype
        | DataIntersectionOf of DataRange list
        | DataUnionOf of DataRange list
        | DataComplementOf of DataRange
        | DataOneOf of GraphElement list
        | DatatypeRestriction of Datatype * (DataProperty * GraphElement) list
        
    type ObjectPropertyExpression =
        | NamedObjectProperty of ObjectProperty
        | AnonymousObjectProperty of uint32
        | InverseObjectProperty of ObjectPropertyExpression
        | ObjectPropertyChain of ObjectPropertyExpression list
        
    type subPropertyExpression = 
        | SubObjectPropertyExpression of ObjectPropertyExpression
        | PropertyExpressionChain of ObjectPropertyExpression list
    
       
    type ClassExpression =
        ClassName of Class
        | AnonymousClass of uint32
        | ObjectUnionOf of ClassExpression list
        | ObjectIntersectionOf of ClassExpression list
        | ObjectComplementOf of ClassExpression
        | ObjectOneOf of Individual list
        | ObjectSomeValuesFrom of ObjectPropertyExpression * ClassExpression
        | ObjectAllValuesFrom of ObjectPropertyExpression * ClassExpression
        | ObjectHasValue of ObjectPropertyExpression * Individual
        | ObjectHasSelf of ObjectPropertyExpression
        | ObjectMinQualifiedCardinality of int * ObjectPropertyExpression * ClassExpression
        | ObjectMaxQualifiedCardinality of int * ObjectPropertyExpression * ClassExpression
        | ObjectExactQualifiedCardinality of int * ObjectPropertyExpression * ClassExpression
        | ObjectExactCardinality of int * ObjectPropertyExpression 
        | ObjectMinCardinality of int * ObjectPropertyExpression
        | ObjectMaxCardinality of int * ObjectPropertyExpression
        | DataSomeValuesFrom of DataProperty list * DataRange
        | DataAllValuesFrom of DataProperty list * DataRange
        | DataHasValue of DataProperty * GraphElement
        | DataMinQualifiedCardinality of int * DataProperty * DataRange
        | DataMaxQualifiedCardinality of int * DataProperty * DataRange
        | DataExactQualifiedCardinality of int * DataProperty * DataRange
        | DataMinCardinality of int * DataProperty
        | DataMaxCardinality of int * DataProperty
        | DataExactCardinality of int * DataProperty

    type ObjectPropertyAxiom =
        | ObjectPropertyDomain of ObjectPropertyExpression * ClassExpression
        | ObjectPropertyRange of ObjectPropertyExpression * ClassExpression
        | ObjectPropertyAssertion of ObjectPropertyExpression * Individual * Individual
        | SubObjectPropertyOf of Annotation list * subPropertyExpression * ObjectPropertyExpression
        | EquivalentObjectProperties of Annotation list * ObjectPropertyExpression list
        | DisjointObjectProperties of Annotation list * ObjectPropertyExpression list
        | InverseObjectProperties of Annotation list * ObjectPropertyExpression * ObjectPropertyExpression
        | FunctionalObjectProperty of Annotation list * ObjectPropertyExpression
        | InverseFunctionalObjectProperty of Annotation list * ObjectPropertyExpression
        | ReflexiveObjectProperty of Annotation list * ObjectPropertyExpression
        | IrreflexiveObjectProperty of Annotation list * ObjectPropertyExpression
        | SymmetricObjectProperty of Annotation list * ObjectPropertyExpression
        | AsymmetricObjectProperty of Annotation list * ObjectPropertyExpression
        | TransitiveObjectProperty of Annotation list * ObjectPropertyExpression
    
    
    type DataPropertyAxiom =
        SubDataPropertyOf of Annotation list * DataProperty * DataProperty
        | EquivalentDataProperties of Annotation list * DataProperty list
        | DisjointDataProperties of Annotation list * DataProperty list
        | DataPropertyDomain of Annotation list * DataProperty * ClassExpression
        | DataPropertyRange of Annotation list * DataProperty * DataRange
        | FunctionalDataProperty of Annotation list * DataProperty
    
    
    type ClassAxiom =
        | SubClassOf of Annotation list * ClassExpression * ClassExpression
        | EquivalentClasses of Annotation list * ClassExpression list
        | DisjointClasses of Annotation list * ClassExpression list
        | DisjointUnion of Annotation list * Class * ClassExpression list
    
    type Assertion =
        SameIndividual of Annotation list * Individual list
        | DifferentIndividuals of Annotation list * Individual list
        | ClassAssertion of Annotation list * ClassExpression * Individual
        | ObjectPropertyAssertion of Annotation list * ObjectPropertyExpression * Individual * Individual
        | NegativeObjectPropertyAssertion of Annotation list * ObjectPropertyExpression * Individual * Individual
        | DataPropertyAssertion of Annotation list * DataProperty * Individual * GraphElement
        | NegativeDataPropertyAssertion of Annotation list * DataProperty * Individual * GraphElement
    
    type Entity =
        | ClassDeclaration of Class
        | ObjectPropertyDeclaration of ObjectProperty
        | DataPropertyDeclaration of DataProperty
        | DatatypeDeclaration of Datatype
        | AnnotationPropertyDeclaration of AnnotationProperty
        | NamedIndividualDeclaration of Individual
    
    type Declaration = Annotation list * Entity
    
    type Axiom =
        AxiomDeclaration of Declaration
        | AxiomClassAxiom of ClassAxiom
        | AxiomObjectPropertyAxiom of ObjectPropertyAxiom
        | AxiomDataPropertyAxiom of DataPropertyAxiom
        | AxiomDatatypeDefinition of Annotation list * Datatype * DataRange
        | AxiomHasKey of Annotation list * ClassExpression * ObjectPropertyExpression list * DataProperty list
        | AxiomAssertion of Assertion    
        | AxiomAnnotationAxiom of AnnotationAxiom
    