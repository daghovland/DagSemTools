namespace AlcTableau

open IriTools
open ALC

module NNF = 
    let rec nnf concept =
        match concept with
        | Negation concept1 -> push_negation concept1
        | Conjunction (c1, c2) -> Conjunction(nnf c1, nnf c2)
        | Disjunction (c1, c2) -> Disjunction(nnf c1, nnf c2)
        | Existential (r1, c1) -> Existential(r1, nnf c1)
        | Universal (r1, c1) -> Universal(r1, nnf c1)
        | _ -> concept
    and push_negation c =
        match c with
        | Negation concept1 -> nnf concept1
        | ConceptName iri1 -> Negation c
        | Conjunction (c1, c2) -> Disjunction(push_negation c1, push_negation c2)
        | Disjunction (c1, c2) -> Conjunction(push_negation c1, push_negation c2)
        | Existential (r1, c1) -> Universal(r1, push_negation c1)
        | Universal (r1, c1) -> Existential(r1, push_negation c1)
        | Top -> Bottom
        | Bottom -> Top
    