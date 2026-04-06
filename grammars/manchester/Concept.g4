/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

grammar Concept;
import ManchesterCommonTokens, CommonTokens, IriGrammar, DataType;

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
    | objectPropertyExpression VALUE rdfiri #ValueConceptRestriction
    | objectPropertyExpression EXACTLY INTEGERLITERAL primary? #CardinalityConceptRestriction
    | objectPropertyExpression MIN INTEGERLITERAL primary? #MinCardinalityConceptRestriction
    | objectPropertyExpression MAX INTEGERLITERAL primary? #MaxCardinalityConceptRestriction
    | dataPropertyExpression SOME dataPrimary #ExistentialDataRestriction
    | dataPropertyExpression ONLY dataPrimary #UniversalDataRestriction
    | dataPropertyExpression EXACTLY INTEGERLITERAL dataPrimary? #CardinalityDataRestriction
    | dataPropertyExpression MIN INTEGERLITERAL dataPrimary? #MinCardinalityDataRestriction
    | dataPropertyExpression MAX INTEGERLITERAL dataPrimary? #MaxCardinalityDataRestriction
    ;

primary:
    NOT primary                   #NegatedPrimaryConcept
    | concept_restriction                   #RestrictionPrimaryConcept
    | rdfiri                        #IriPrimaryConcept
    | '{' individual (COMMA individual)* '}' #NominalPrimaryConcept
    | '(' description ')'     #ParenthesizedPrimaryConcept
    ;

individual: rdfiri ;

objectPropertyExpression: rdfiri #ObjectPropertyIri
    | INVERSE rdfiri #InverseObjectProperty
    ;
    
dataPropertyExpression: rdfiri;



dataRange: dataConjunction #SingleDataDisjunction
    | (dataConjunction OR dataRange) #DisjunctionDataRange
    ;
 
dataConjunction: dataPrimary#SingleDataConjunction
    | dataPrimary AND dataConjunction #ActualDataRangeConjunction
    ;
    
dataPrimary: dataAtomic #PositiveDataPrimary
    | NOT dataAtomic #NegativeDataPrimary
    ;
    
dataAtomic : datatype #DataTypeAtomic
    | '{' literal (COMMA literal)* '}' #LiteralSet
    | LPAREN dataRange RPAREN #DataRangeParenthesis
    | datatype LSQUARE datatype_restriction (COMMA datatype_restriction)* RSQUARE #DatatypeRestriction
    ;
    
datatype_restriction: facet literal;

facet: LENGTH #facetLength
    | MINLENGTH #facetMinLength
    | MAXLENGTH #facetMaxLength
    | PATTERN #facetPattern
    | LANGRANGE #facetLangRange
    | LT #facetLessThan
    | GT #facetGreaterThan
    | LTE #facetLessThanEqual
    | GTE #facetGreaterThanEqual
    ; 
