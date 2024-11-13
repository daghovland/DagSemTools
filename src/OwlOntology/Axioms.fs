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

module OwlOntology.Axioms

open System
open DagSemTools.Resource
open IriTools

    type Iri = 
        | FullIri of IriReference
        | AbbreviatedIri of string
        | PrefixedIri of PrefixName : string * LocalName : string
    
    type Individual =
        | NamedIndividual of IriReference
        | AnonymousIndividual of uint32
    
    type AnnotationProperty = Iri
    type ObjectProperty = Iri
    type DataProperty = Iri
    type Datatype = Iri
    type Class = Iri
    
    type AnnotationValue =
        IndividualAnnotation of Individual
        | LiteralAnnotation of Resource
        | IriAnnotation of Iri
        
    type Annotation = AnnotationProperty * AnnotationValue
        
    type AnnotationAxiom =
        | AnnotationAssertion of Annotation list * AnnotationProperty * Resource * Resource
        | SubAnnotationPropertyOf of Annotation list * AnnotationProperty * AnnotationProperty
        | AnnotationPropertyDomain of Annotation list * AnnotationProperty * Iri
        | AnnotationPropertyRange of Annotation list * AnnotationProperty * Iri
        
    type DataRange =
        Datatype of Datatype
        | DataIntersectionOf of DataRange list
        | DataUnionOf of DataRange list
        | DataComplementOf of DataRange
        | DataOneOf of Resource list
        | DatatypeRestriction of Datatype * (DataProperty * Resource) list
        
    type ObjectPropertyExpression =
        | ObjectProperty of IriReference
        | InverseObjectProperty of ObjectPropertyExpression
        | ObjectPropertyChain of ObjectPropertyExpression list
        
    type subPropertyExpression = 
        | SubObjectPropertyExpression of ObjectPropertyExpression
        | PropertyExpressionChain of ObjectPropertyExpression list
    
       
    type ClassExpression =
        ClassName of Class
        | ObjectUnionOf of ClassExpression list
        | ObjectIntersectionOf of ClassExpression list
        | ObjectComplementOf of ClassExpression
        | ObjectOneOf of Individual list
        | ObjectSomeValuesFrom of ObjectPropertyExpression * ClassExpression
        | ObjectAllValuesFrom of ObjectPropertyExpression * ClassExpression
        | ObjectHasValue of ObjectPropertyExpression * Individual
        | ObjectHasSelf of ObjectPropertyExpression
        | ObjectMinCardinality of int * ObjectPropertyExpression * ClassExpression
        | ObjectMaxCardinality of int * ObjectPropertyExpression * ClassExpression
        | ObjectExactCardinality of int * ObjectPropertyExpression * ClassExpression
        | DataSomeValuesFrom of DataProperty list * DataRange
        | DataAllValuesFrom of DataProperty list * DataRange
        | DataHasValue of DataProperty * Resource
        | DataMinCardinality of int * DataProperty * DataRange list
        | DataMaxCardinality of int * DataProperty * DataRange list
        | DataExactCardinality of int * DataProperty * DataRange list

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
        | DataPropertyAssertion of Annotation list * DataProperty * Individual * Resource
        | NegativeDataPropertyAssertion of Annotation list * DataProperty * Individual * Resource
        
    
    type Entity =
        | ClassDeclaration of Class
        | ObjectPropertyDeclaration of ObjectProperty
        | DataPropertyDeclaration of DataProperty
        | DatatypeDeclaration of Datatype
        | AnnotationPropertyDeclaration of AnnotationProperty
        | NamedIndividualDeclaration of Individual
    
    type Declaration = Declaration of Annotation list * Entity
    
    type Axiom =
        Declaration of Declaration
        | ClassAxiom of ClassAxiom
        | ObjectPropertyAxiom of ObjectPropertyAxiom
        | DataPropertyAxiom of DataPropertyAxiom
        | DatatypeDefinition of Annotation list * Datatype * DataRange
        | HasKey of Annotation list * ClassExpression * ObjectPropertyExpression list * DataProperty list
        | Assertion of Assertion    
        | AnnotationAxiom of AnnotationAxiom
    