/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

lexer grammar ManchesterCommonTokens;

INVERSE: 'inverse';
THAT: 'that';
ONTOLOGYTOKEN: 'Ontology:';
PREFIXTOKEN: 'Prefix:';
IMPORT: 'Import:';
CLASS: 'Class:';
INDIVIDUAL: 'Individual:';
DATATYPE: 'Datatype:' ;
OBJECTPROPERTY: 'ObjectProperty:';
DATAPROPERTY: 'DataProperty:';
CHARACTERISTICS: 'Characteristics:';
ANNOTATIONPROPERTY: 'AnnotationProperty:';
ANNOTATIONS: 'Annotations:';
FUNCTIONAL: 'Functional';
INVERSEFUNCTIONAL: 'InverseFunctional';
REFLEXIVE: 'Reflexive';
IRREFLEXIVE: 'Irreflexive';
ASYMMETRIC: 'Asymmetric';
TRANSITIVE: 'Transitive';
SYMMETRIC: 'Symmetric';
DISJOINTWITH: 'DisjointWith:';
SUBPROPERTYCHAIN: 'SubPropertyChain:';
SUBCLASSOF: 'SubClassOf:';
EQUIVALENTTO: 'EquivalentTo:';
SUBPROPERTYOF : 'SubPropertyOf:' ;
INVERSEOF : 'InverseOf:' ;
DOMAIN : 'Domain:' ;
RANGE : 'Range:' ;

LENGTH: 'length';
MINLENGTH: 'minLength';
MAXLENGTH: 'maxLength';
PATTERN: 'pattern';
LANGRANGE: 'langRange';
NOT: 'not';
AND: 'and';
OR: 'or';
SOME: 'some';
ONLY: 'only';
EXACTLY: 'exactly';
MIN: 'min';
MAX: 'max';
SELF: 'Self';
