grammar Concept;
import ManchesterCommonTokens, IriGrammar;

description: description OR conjunction #ActualDisjunction
    | conjunction #SingleDisjunction;

conjunction: conjunction AND primary #ActualConjunction
    | primary #SingleConjunction
    ; 

restriction:
    INVERSE? rdfiri SOME primary #ExistentialRestriction
    | INVERSE? rdfiri ONLY primary #UniversalRestriction
    ;

primary:
    NOT primary                   #NegatedPrimary
    | restriction                   #RestrictionPrimary
    | rdfiri                        #IriPrimary
    | '(' description ')'     #ParenthesizedPrimary
    ;

