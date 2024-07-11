namespace AlcTableau

open IriTools

module ALC =
    
    type Iri = 
    | FullIri of IriReference
    | AbbreviatedIri of string
    | PrefixedIri of PrefixName : string * LocalName : string

    type Role = IriReference
    
    [<CustomEquality>]
    [<NoComparison>]
    type Concept =
        | ConceptName of IriReference
        | Disjunction of Left: Concept * Right: Concept
        | Conjunction of Left: Concept * Right: Concept
        | Negation of Concept
        | Existential of Role: Role * Constraint: Concept
        | Universal of Role: Role * Constraint: Concept
        | Top
        | Bottom
        override this.Equals(obj) =
            match obj with
            | :? Concept as other ->
                match this, other with
                | ConceptName iri1, ConceptName iri2 -> iri1 = iri2
                | Disjunction (left1, right1), Disjunction (left2, right2) -> left1 = left2 && right1 = right2
                | Conjunction (left1, right1), Conjunction (left2, right2) -> left1 = left2 && right1 = right2
                | Negation concept1, Negation concept2 -> concept1 = concept2
                | Existential (role1, concept1), Existential (role2, concept2) -> role1 = role2 && concept1 = concept2
                | Universal (role1, concept1), Universal (role2, concept2) -> role1 = role2 && concept1 = concept2
                | Top, Top -> true
                | Bottom, Bottom -> true
                | _ -> false
            | _ -> false
        override this.GetHashCode() =
            match this with
            | ConceptName iri -> hash iri
            | Disjunction (left, right) -> hash (left, right)
            | Conjunction (left, right) -> hash (left, right)
            | Negation concept -> hash concept
            | Existential (role, concept) -> hash (role, concept)
            | Universal (role, concept) -> hash (role, concept)
            | Top -> hash 1
            | Bottom -> hash 2
        
    type TBoxAxiom =
        | Inclusion of Sub: Concept * Sup: Concept
        | Equivalence of Left: Concept * Right: Concept
        
    type TBox = TBoxAxiom list
    
    type ABoxAssertion =
        | Member of Individual: IriReference * Concept
        | RoleMember of Left: IriReference * Right: IriReference * Role
       
    type ABox = ABoxAssertion list
    
    type KnowledgeBase = TBox * ABox