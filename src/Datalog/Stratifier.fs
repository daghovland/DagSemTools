namespace DagSemTools.Datalog

open System.Collections.Generic
open DagSemTools.Rdf.Ingress
open DagSemTools.Datalog.Datalog


module Stratifier =

    type OrderedRelation = {
        Relation : ResourceId
        mutable Successors : ResourceId list
        mutable num_predecessors : int
    }
    let GetTriplePatternRelation (triple : TriplePattern) : ResourceId option =
            match triple.Predicate with
                                        | ResourceOrVariable.Variable _ -> None
                                        | ResourceOrVariable.Resource res -> Some res
    
    let GetRuleAtomRelation (atom : RuleAtom) : ResourceId option =
        let triple = match atom with
                        | PositiveTriple t -> t
                        | NotTriple t -> t
        GetTriplePatternRelation triple                        
                 
    (* The intensional relations (properties) are those that occur in the head of at least one rule *)       
    let GetIntentionalRelations (rules : Rule list)  =
        rules
        |> Seq.choose (fun rule -> rule.Head |> GetTriplePatternRelation)
        |> Seq.distinct
    
    (* The extensional relations (properties) are those that only occur in the body of rules *)       
    let GetExtentionalRelations (rules : Rule list)  =
        let intentionalRelations = GetIntentionalRelations rules
        let bodyRules = rules
                            |> Seq.collect (fun rule ->
                                rule.Body |> Seq.choose GetRuleAtomRelation
                                )
        bodyRules |> Seq.except intentionalRelations |> Seq.distinct
  
    
    (* Returns any rules containing negations of intentional properties. These relations make the program not semipositive *)
    let NegativeIntentionalProperties (rules : Rule list) =
        let intentionalRelations =
                rules
                |> GetIntentionalRelations
                |> Seq.map ResourceOrVariable.Resource
        rules |> Seq.filter (fun rule ->
                            rule.Body |> Seq.exists (fun atom ->
                                match atom with
                                    | NotTriple t -> intentionalRelations |> Seq.contains (t.Predicate)
                                    | _ -> false
                                ) 
                            )
          
    (* A datalog program is semi-positive if negations only occur onextensional relations (Relations that do not occur in the head of any rule) *)
    let IsSemiPositiveProgram (rules : Rule list) =
        NegativeIntentionalProperties rules |> Seq.isEmpty
  
    
    type RulePartitioner(num_concepts: int, rules: Rule list) =
        let preprocess_rules =
            let ordered = Array.init num_concepts (fun i -> { Relation = uint i; Successors = []; num_predecessors = 0 })
            rules |> Seq.iter (fun rule ->
                let dependencies = rule.Body
                                   |> Seq.choose GetRuleAtomRelation
                                   |> Seq.distinct
                match (rule.Head |> GetTriplePatternRelation) with
                | None -> ()
                | Some concept ->
                    let concept_node = ordered.[int concept]
                    dependencies |> Seq.iter (fun dep ->
                        let dep_node = ordered.[int dep]
                        concept_node.Successors <- dep :: concept_node.Successors
                        dep_node.num_predecessors <- dep_node.num_predecessors + 1
                        )
                    ordered.[int concept] <- concept_node
                    
                )
            ordered
        
        // Private properties
        let mutable ordered = preprocess_rules
        let mutable queue = new Queue<ResourceId>()
        let mutable n_unordered = 0
            

        (* Gets all rules that are not part of a cycle. This will become a partition of the stratification *)
        member this.get_rule_partition   =
            let mutable ordered_rules = []
            while queue.Count > 0 do
                let relation_id = queue.Dequeue()
                let relation_rules = rules
                                    |> List.filter (fun rule ->
                                        match rule.Head |> GetTriplePatternRelation with
                                            | None -> false
                                            | Some res -> res = relation_id
                                    )
                let relation = ordered.[int relation_id]
                ordered_rules <- List.concat [relation_rules ; ordered_rules]
                relation.Successors |> List.iter (fun succ ->
                    let succ_node = ordered.[int succ]
                    succ_node.num_predecessors <- succ_node.num_predecessors - 1
                    if succ_node.num_predecessors = 0 then
                        queue.Enqueue succ
                        n_unordered <- n_unordered - 1
                    )
            ordered_rules
        
        (* Order the rules topologically based on dependency. Used for stratification *)
        member this.orderRules  : Rule list list =
            ordered |> Array.iter (fun concept ->
                if concept.num_predecessors = 0 then
                    queue.Enqueue concept.Relation
                else 
                    n_unordered <- n_unordered + 1)
            let mutable stratification = [this.get_rule_partition]
            while n_unordered > 0 do
                stratification <- stratification @ [this.get_rule_partition]
            stratification
            
            
        
        
