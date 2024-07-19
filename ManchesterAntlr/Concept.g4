grammar Concept;
import IriGrammar, ManchesterCommonTokens;

description: conjunction ('or' conjunction)*;

conjunction: primary ('and' primary)*;

restriction:
    INVERSE? rdfiri SOME primary #ExistentialRestriction
    | INVERSE? rdfiri ONLY primary #UniversalRestriction
    ;

primary:
    NOT primary                   #NegatedPrimary
    | restriction                   #RestrictionPrimary
    | rdfiri                        #IriPrimary
    | LPAREN description RPAREN     #ParenthesizedPrimary
    ;

