namespace AlcTableau

open AlcTableau.ALC
open IriTools
open ALC

module NNF = 
    let rec nnf_concept concept =
        match concept with
        | Negation concept1 -> push_negation concept1
        | Conjunction (c1, c2) -> Conjunction(nnf_concept c1, nnf_concept c2)
        | Disjunction (c1, c2) -> Disjunction(nnf_concept c1, nnf_concept c2)
        | Existential (r1, c1) -> Existential(r1, nnf_concept c1)
        | Universal (r1, c1) -> Universal(r1, nnf_concept c1)
        | _ -> concept
    and push_negation c =
        match c with
        | Negation concept1 -> nnf_concept concept1
        | ConceptName iri1 -> Negation c
        | Conjunction (c1, c2) -> Disjunction(push_negation c1, push_negation c2)
        | Disjunction (c1, c2) -> Conjunction(push_negation c1, push_negation c2)
        | Existential (r1, c1) -> Universal(r1, push_negation c1)
        | Universal (r1, c1) -> Existential(r1, push_negation c1)
        | Top -> Bottom
        | Bottom -> Top
    
    let nnf_assertion assertion =
        match assertion with
            | ConceptAssertion (individual, concept) -> ConceptAssertion(individual, nnf_concept concept)
            | r -> r
            
    let nnf_kb (tbox, abox) =
        (tbox, List.map nnf_assertion abox)
        