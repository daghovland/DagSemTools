grammar Concept;
import ManchesterCommonTokens, IriGrammar;

description: description 'or' conjunction #ActualDisjunction
    | conjunction #SingleDisjunction;

conjunction: conjunction 'and' primary #ActualConjunction
    | primary #SingleConjunction
    ; 

restriction:
    INVERSE? rdfiri 'some' primary #ExistentialRestriction
    | INVERSE? rdfiri 'only' primary #UniversalRestriction
    ;

primary:
    'not' primary                   #NegatedPrimary
    | restriction                   #RestrictionPrimary
    | rdfiri                        #IriPrimary
    | '(' description ')'     #ParenthesizedPrimary
    ;

