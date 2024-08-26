/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

grammar Concept;
import ManchesterCommonTokens, IriGrammar, DataType;

description: description OR conjunction #ConceptDisjunction
    | conjunction #ConceptSingleDisjunction;

conjunction: rdfiri THAT conjunction_restriction (AND conjunction_restriction)*  #ConceptThat 
    | conjunction AND primary #ConceptConjunction
    | primary #ConceptSingleConjunction
    ; 

conjunction_restriction: concept_restriction #ConjunctionRestriction
    | NOT concept_restriction #NotConjunctionRestriction
    ;

concept_restriction:
    objectPropertyExpression SOME primary #ExistentialConceptRestriction
    | objectPropertyExpression ONLY primary #UniversalConceptRestriction
    | objectPropertyExpression EXACTLY INTEGERLITERAL primary? #CardinalityConceptRestriction
    | dataPropertyExpression SOME dataPrimary #ExistentialDataRestriction
    | dataPropertyExpression ONLY dataPrimary #UniversalDataRestriction
    | dataPropertyExpression EXACTLY INTEGERLITERAL dataPrimary? #CardinalityConceptRestriction
    ;

primary:
    NOT primary                   #NegatedPrimaryConcept
    | concept_restriction                   #RestrictionPrimaryConcept
    | rdfiri                        #IriPrimaryConcept
    | '(' description ')'     #ParenthesizedPrimaryConcept
    ;

objectPropertyExpression: rdfiri #ObjectPropertyIri
    | INVERSE rdfiri #InverseObjectProperty
    ;
    
dataPropertyExpression: rdfiri;