namespace AlcTableau

open System
open IriTools

module ALC =
    
    type Iri = 
    | FullIri of IriReference
    | AbbreviatedIri of string
    | PrefixedIri of PrefixName : string * LocalName : string

    type Role = IriReference
    
    [<CustomEquality>]
    [<CustomComparison>]
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
            | Disjunction (left, right) -> hash (left, "or", right)
            | Conjunction (left, right) -> hash (left, "and", right)
            | Negation concept -> hash ("not", concept)
            | Existential (role, concept) -> hash (role, "some", concept)
            | Universal (role, concept) -> hash (role, "only", concept)
            | Top -> hash "http://www.w3.org/2002/07/owl#Thing"
            | Bottom -> hash "http://www.w3.org/2002/07/owl#Nothing"
        
        interface IComparable with
            member this.CompareTo(obj) =
                match obj with
                | :? Concept as other ->
                    match this, other with
                    | ConceptName iri1, ConceptName iri2 -> compare iri1 iri2
                    | Disjunction (left1, right1), Disjunction (left2, right2) -> 
                        let leftCompare = compare left1 left2
                        if leftCompare <> 0 then leftCompare else compare right1 right2
                    | Conjunction (left1, right1), Conjunction (left2, right2) -> 
                        let leftCompare = compare left1 left2
                        if leftCompare <> 0 then leftCompare else compare right1 right2
                    | Negation concept1, Negation concept2 -> compare concept1 concept2
                    | Existential (role1, concept1), Existential (role2, concept2) -> 
                        let roleCompare = compare role1 role2
                        if roleCompare <> 0 then roleCompare else compare concept1 concept2
                    | Universal (role1, concept1), Universal (role2, concept2) -> 
                        let roleCompare = compare role1 role2
                        if roleCompare <> 0 then roleCompare else compare concept1 concept2
                    | Top, Top -> 0
                    | Bottom, Bottom -> 0
                    | ConceptName _, _ -> -1
                    | _, ConceptName _ -> 1
                    | Disjunction _, _ -> -1
                    | _, Disjunction _ -> 1
                    | Conjunction _, _ -> -1
                    | _, Conjunction _ -> 1
                    | Negation _, _ -> -1
                    | _, Negation _ -> 1
                    | Existential _, _ -> -1
                    | _, Existential _ -> 1
                    | Universal _, _ -> -1
                    | _, Universal _ -> 1
                    | Top, _ -> -1
                    | _, Top -> 1
                    
                | _ -> invalidArg "obj" "Cannot compare Concept with other types."
        
        
    type TBoxAxiom =
        | Inclusion of Sub: Concept * Sup: Concept
        | Equivalence of Left: Concept * Right: Concept
        
    type TBox = TBoxAxiom list
    
    type ABoxAssertion =
        | ConceptAssertion of Individual: IriReference * Concept
        | RoleAssertion of Left: IriReference * Right: IriReference * AssertedRole:  Role
       
    type ABox = ABoxAssertion list
    
    type knowledgeBase = TBox * ABox
    
    type ontologyVersion =
        | UnNamedOntology
        | NamedOntology of OntologyIri: IriReference
        | VersionedOntology of OntologyIri: IriReference * OntologyVersionIri: IriReference
    type ontologyVersion with
        member x.TryGetOntologyVersionIri() =
            match x with
            | NamedOntology iri -> null
            | VersionedOntology (_, iri) -> iri
            | _ -> null
        member x.TryGetOntologyIri() =
            match x with
            | NamedOntology iri -> iri
            | VersionedOntology (iri, _) -> iri
            | _ -> null
            
    type prefixDeclaration =
        | PrefixDefinition of PrefixName: string * PrefixIri: IriReference
    type prefixDeclaration with
        member x.TryGetPrefixName() =
            match x with
            | PrefixDefinition (name, iri) -> (name, iri)
    type OntologyDocument =
        | Ontology of prefixDeclaration list * ontologyVersion * knowledgeBase
    type OntologyDocument with
        member x.TryGetOntology() =
            match x with
            | Ontology (prefixes, ontologyVersion, KB) -> (prefixes, ontologyVersion, KB)
            