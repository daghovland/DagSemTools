(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.Resource
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
    let RdfsSubClassOf = Rdfs + "subClassOf";
    [<Literal>]
    let RdfsDatatype = Rdfs + "Datatype";
    
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
    let OwlAnnotatedSource = Owl + "annotatedSource"
    [<Literal>]
    let OwlAnnotatedProperty = Owl + "annotatedProperty"
    [<Literal>]
    let OwlAnnotatedTarget = Owl + "annotatedTarget"
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
    let XsdInteger = Xsd + "integer";
    [<Literal>]
    let XsdHexBinary = Xsd + "hexBinary";
    [<Literal>]
    let XsdBase64Binary = Xsd + "base64Binary";
    [<Literal>]
    let XsdAnyUri = Xsd + "anyURI";
