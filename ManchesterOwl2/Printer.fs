module Manchester.Printer

open AlcTableau.ALC
open IriTools

let printRole (role: Role) = 
    role.ToString()
let rec toString (concept: Concept) =
    match concept with
    | ConceptName iri -> $"%s{iri.ToString()}"
    | Disjunction (left, right) -> 
        $"{toString left} or {toString right}"
    | Conjunction (left, right) -> 
        $"{toString left} and {toString right}"
    | Negation concept -> 
        $"not {toString concept}"
    | Existential (role, concept) ->
        $"{printRole role} some {toString concept}"
    | Universal (role, concept) -> 
        $"{printRole role} only {toString concept}"
    | Top -> "owl:Thing"
    | Bottom -> "owl:Nothing"

