(*
    Copyright (C) 2024-2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.    
    Contact: hovlanddag@gmail.com
*)

namespace DagSemTools.AlcTableau

open System
open IriTools
open DagSemTools.Ingress

module ALC =
    
    type Role =
        | Iri of IriReference
        | Inverse of IriReference
    
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
        
    let rec GetConceptNames (concept: Concept) : IriReference list =
        match concept with
        | ConceptName iri -> [iri]
        | Disjunction (left, right) -> GetConceptNames left @ GetConceptNames right
        | Conjunction (left, right) -> GetConceptNames left @ GetConceptNames right
        | Negation concept -> GetConceptNames concept
        | Existential (_, concept) -> GetConceptNames concept
        | Universal (_, concept) -> GetConceptNames concept
        | Top -> []
        | Bottom -> []
        
    let rec GetConcepts (concept: Concept) : Concept list =
        (match concept with
        | ConceptName _ -> []
        | Disjunction (left, right) -> GetConcepts left @ GetConcepts right
        | Conjunction (left, right) -> GetConcepts left @ GetConcepts right
        | Negation concept -> GetConcepts concept
        | Existential (_, concept) -> GetConcepts concept
        | Universal (_, concept) -> GetConcepts concept
        | Top -> []
        | Bottom -> []) @ [concept]
    let rec GetExistentials (concept: Concept) : (Role * Concept) list =
        (match concept with
        | ConceptName _ -> []
        | Disjunction (left, right) -> GetExistentials left @ GetExistentials right
        | Conjunction (left, right) -> GetExistentials left @ GetExistentials right
        | Negation concept -> GetExistentials concept
        | Existential (role, inner) -> GetExistentials inner @ [(role, inner)]
        | Universal (_, concept) -> GetExistentials concept
        | Top -> []
        | Bottom -> [])
    
    
    type TBoxAxiom =
        | Inclusion of Sub: Concept * Sup: Concept
    
    let GetAxiomConcepts (axiom: TBoxAxiom) : Concept list =
        match axiom with
        | Inclusion (sub, sup) -> GetConcepts sub @ GetConcepts sup
    let GetAxiomExistentials (axiom: TBoxAxiom)  =
        match axiom with
        | Inclusion (sub, sup) -> GetExistentials sub @ GetExistentials sup
        
    let GetAxiomConceptNames (axiom: TBoxAxiom) : IriReference list =
        match axiom with
        | Inclusion (sub, sup) -> GetConceptNames sub @ GetConceptNames sup
    type TBox = TBoxAxiom list
    
    type ABoxAssertion =
        | ConceptAssertion of Individual: IriReference * Concept
        | NegativeAssertion of ABoxAssertion
        | RoleAssertion of Individual: IriReference * Right: IriReference * AssertedRole:  Role
        | NegativeRoleAssertion of Individual: IriReference * Right: IriReference * AssertedRole:  Role
        | LiteralAssertion of Individual: IriReference * Property: IriReference * Value: string
        | NegativeLiteralAssertion of Individual: IriReference * Property: IriReference * Value: string
        | LiteralAnnotationAssertion of Individual: IriReference * Property: IriReference * Value: string
        | ObjectAnnotationAssertion of Individual:  IriReference * Property: IriReference * Value: IriReference
       
    type ABox = ABoxAssertion list
    
    type knowledgeBase = TBox * ABox
    
            
    type OntologyDocument =
        | Ontology of prefixDeclaration list * ontologyVersion * knowledgeBase
    type OntologyDocument with
        member x.TryGetOntology() =
            match x with
            | Ontology (prefixes, ontologyVersion, KB) -> (prefixes, ontologyVersion, KB)
            