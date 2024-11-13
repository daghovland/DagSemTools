namespace DagSemTools.Rdf2Owl

open DagSemTools.Rdf
open OwlOntology

module Translator =
    let extractOntology (tripleTable : TripleTable) (resources : ResourceManager) =
        OwlOntology.Ontology.Ontology ([], Ontology.UnNamedOntology, [], [])