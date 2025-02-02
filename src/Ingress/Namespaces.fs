(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.Ingress
open IriTools



/// <summary>
/// Namespaces and IRIs used in the Turtle language.
/// </summary>
module Namespaces =
    /// <summary>
    /// The rdf namespace
    /// </summary>
    [<Literal>]
    let Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    [<Literal>]
    let Rdfs = "http://www.w3.org/2000/01/rdf-schema#";
    [<Literal>]
    let Owl = "http://www.w3.org/2002/07/owl#";
    /// <summary>
    /// The XML Schema namespace
    /// </summary>
    [<Literal>]
    let Xsd = "http://www.w3.org/2001/XMLSchema#";

    /// <summary>
    /// The IRI for rdf:type, also abbreviated 'a' in turtle
    /// </summary>
    [<Literal>]
    let RdfType = Rdf + "type";
    [<Literal>]
    let RdfNil = Rdf + "nil";
    [<Literal>]
    let RdfFirst = Rdf + "first";
    [<Literal>]
    let RdfRest = Rdf + "rest";

    [<Literal>]
    let RdfReifies = Rdf + "reifies"
    [<Literal>]
    let RdfsLiteral = Rdfs + "Literal";
    
    [<Literal>]
    let RdfsSubClassOf = Rdfs + "subClassOf";
    [<Literal>]
    let RdfsSubPropertyOf = Rdfs + "subPropertyOf";
    [<Literal>]
    let RdfsDatatype = Rdfs + "Datatype";
    [<Literal>]
    let RdfsDomain = Rdfs + "domain";
    [<Literal>]
    let RdfsRange = Rdfs + "range";
    
    [<Literal>]
    let OwlSameAs = Owl + "sameAs";
    [<Literal>]
    let OwlOntology = Owl + "Ontology"
    [<Literal>]
    let OwlImport = Owl + "imports";
    [<Literal>]
    let OwlVersionIri = Owl + "versionIri";
    [<Literal>]
    let OwlOntologyProperty = Owl + "OntologyProperty"
    [<Literal>]
    let OwlAnnotationProperty = Owl + "AnnotationProperty";
    [<Literal>]
    let OwlOnProperty = Owl + "onProperty";
    [<Literal>]
    let OwlOnProperties = Owl + "onProperties"
    [<Literal>]
    let OwlOnDataRange = Owl + "onDataRange"
    [<Literal>]
    let OwlDatatypeProperty = Owl + "DatatypeProperty";
    [<Literal>]
    let OwlObjectProperty = Owl + "ObjectProperty";
    [<Literal>]
    let OwlClass = Owl + "Class"
    [<Literal>]
    let OwlNamedIndividual = Owl + "NamedIndividual"
    [<Literal>]
    let OwlAxiom = Owl + "Axiom"
    [<Literal>]
    let OwlThing = Owl + "Thing"
    [<Literal>]
    let OwlNothing = Owl + "Nothing"
    [<Literal>]
    let OwlAnnotation = Owl + "Annotation"
    [<Literal>]
    let OwlAnnotatedSource = Owl + "annotatedSource"
    [<Literal>]
    let OwlAnnotatedProperty = Owl + "annotatedProperty"
    [<Literal>]
    let OwlAnnotatedTarget = Owl + "annotatedTarget"
    [<Literal>]
    let OwlAllDisjointClasses = Owl + "AllDisjointClasses"
    [<Literal>]
    let OwlAllDisjointProperties = Owl + "AllDisjointProperties"
    [<Literal>]
    let OwlAllDifferent = Owl + "AllDifferent"
    [<Literal>]
    let OwlEquivalentClass = Owl + "equivalentClass"
    [<Literal>]
    let OwlMembers = Owl + "members"
    [<Literal>]
    let OwlEquivalentProperty = Owl + "equivalentProperty"
    [<Literal>]
    let OwlPropertyDisjointWith = Owl + "propertyDisjointWith"
    [<Literal>]
    let OwlFunctionalProperty = Owl + "FunctionalProperty"
    [<Literal>]
    let OwlInverseFunctionalProperty = Owl + "InverseFunctionalProperty"
    [<Literal>]
    let OwlReflexiveProperty = Owl + "ReflexiveProperty"
    [<Literal>]
    let OwlIrreflexiveProperty = Owl + "IrreflexiveProperty"
    [<Literal>]
    let OwlSymmetricProperty = Owl + "SymmetricProperty"
    [<Literal>]
    let OwlAsymmetricProperty = Owl + "AsymmetricProperty"
    [<Literal>]
    let OwlTransitiveProperty = Owl + "TransitiveProperty"
    [<Literal>]
    let OwlDisjointWith = Owl + "disjointWith"
    [<Literal>]
    let OwlDisjointUnionOf = Owl + "disjointUnionOf"
    [<Literal>]
    let OwlNegativePropertyAssertion = Owl + "NegativePropertyAssertion"
    [<Literal>]
    let OwlObjectInverseOf = Owl + "inverseOf"
    [<Literal>]
    let OwlPropertyChainAxiom = Owl + "propertyChainAxiom"
    [<Literal>]
    let OwlRestriction = Owl + "Restriction"
    [<Literal>]
    let OwlIntersectionOf = Owl + "intersectionOf"
    [<Literal>]
    let OwlUnionOf = Owl + "unionOf"
    [<Literal>]
    let OwlComplementOf = Owl + "complementOf"
    [<Literal>]
    let OwlOneOf = Owl + "oneOf"
    [<Literal>]
    let OwlSomeValuesFrom = Owl + "someValuesFrom"
    [<Literal>]
    let OwlAllValuesFrom = Owl + "allValuesFrom"
    [<Literal>]
    let OwlHasValue = Owl + "hasValue"
    [<Literal>]
    let OwlMinQualifiedCardinality = Owl + "minQualifiedCardinality"
    [<Literal>]
    let OwlMaxQualifiedCardinality = Owl + "maxQualifiedCardinality"
    [<Literal>]
    let OwlQualifiedCardinality = Owl + "qualifiedCardinality"
    [<Literal>]
    let OwlCardinality = Owl + "cardinality"
    [<Literal>]
    let OwlMinCardinality = Owl + "minCardinality"
    [<Literal>]
    let OwlMaxCardinality = Owl + "maxCardinality"
    [<Literal>]
    let OwlOnClass = Owl + "onClass"
    [<Literal>]
    let OwlHasSelf = Owl + "hasSelf"
    [<Literal>]
    let XsdString = Xsd + "string";
    [<Literal>]
    let XsdBoolean = Xsd + "boolean";
    [<Literal>]
    let XsdDecimal = Xsd + "decimal";
    [<Literal>]
    let XsdFloat = Xsd + "float";
    [<Literal>]
    let XsdDouble = Xsd + "double";
    [<Literal>]
    let XsdDuration = Xsd + "duration";
    [<Literal>]
    let XsdDateTime = Xsd + "dateTime";
    [<Literal>]
    let XsdTime = Xsd + "time";
    [<Literal>]
    let XsdDate = Xsd + "date";
    [<Literal>]
    let XsdInt = Xsd + "int";
    [<Literal>]
    let XsdInteger = Xsd + "integer"
    [<Literal>]
    let XsdNonNegativeInteger = Xsd + "nonNegativeInteger"
    [<Literal>]
    let XsdHexBinary = Xsd + "hexBinary";
    [<Literal>]
    let XsdBase64Binary = Xsd + "base64Binary";
    [<Literal>]
    let XsdAnyUri = Xsd + "anyURI";
    [<Literal>]
    let XsdMinLength = Xsd + "minLength";
    [<Literal>]
    let XsdMaxLength = Xsd + "maxLength"
    [<Literal>]
    let XsdMinInclusive = Xsd + "minInclusive";
    [<Literal>]
    let XsdMaxInclusive = Xsd + "maxInclusive";
    [<Literal>]
    let XsdMinExclusive = Xsd + "minExclusive";
    [<Literal>]
    let XsdMaxExclusive = Xsd + "maxExclusive";
    [<Literal>]
    let XsdLength = Xsd + "length";
    [<Literal>]
    let XsdPattern = Xsd + "pattern";
    [<Literal>]
    let XsdLangRange = Xsd + "langRange";
    
    
    
    