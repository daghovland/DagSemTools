grammar Concept;
import ManchesterCommonTokens, IriGrammar;

start: description EOF;

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
    NOT primary                   #NegatedPrimary
    | restriction                   #RestrictionPrimary
    | rdfiri                        #IriPrimary
    | '(' description ')'     #ParenthesizedPrimary
    ;

