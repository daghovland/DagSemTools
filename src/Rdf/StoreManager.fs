namespace Rdf

open Rdf.RDFStore


module StoreManager = 
    
    let init() =
        {
            ResourceMap = Map.empty
            ResourceList = Array.empty
            TripleList = Array.empty
            ThreeKeysIndex = Map.empty
            subject_index = Array.empty
            predicate_index = Array.empty
            object_index = Array.empty
            subject_predicate_index = Map.empty
            object_predicate_index = Map.empty
        }
