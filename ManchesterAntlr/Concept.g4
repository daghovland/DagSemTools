grammar Concept;
import ManchesterCommonTokens, IriGrammar;

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
    | objectPropertyExpression EXACTLY INTEGERLITERAL #CardinalityConceptRestriction
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