namespace AlcTableau

open IriTools

module ALC =
    type Role = IriReference
    
    type Concept =
        | ConceptName of IriReference
        | Disjunction of Left: Concept * Right: Concept
        | Conjunction of Left: Concept * Right: Concept
        | Negation of Concept
        | Existential of Role * Concept
        | Universal of Role * Concept
        | Top
        | Bottom
        
    type TBoxAxiom =
        | Inclusion of Sub: Concept * Sup: Concept
        | Equivalence of Left: Concept * Right: Concept
        
    type TBox = TBoxAxiom list
    
    type ABoxAssertion =
        | Member of Individual: IriReference * Concept
        | RoleMember of Left: IriReference * Right: IriReference * Role
       
    type ABox = ABoxAssertion list
    
    type KnowledgeBase = TBox * ABox