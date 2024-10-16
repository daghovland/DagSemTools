namespace DagSemTools.Datalog

open System.Collections.Generic
open DagSemTools.Rdf.Ingress
open DagSemTools.Datalog.Datalog


module Stratifier =

    (*
        A binary relation represents a triple atom where the object place is a variable
        A unary predicate reperesents a triple atom where the object plase is a term. F.ex. the type triples
        I realize the names may be counter-intuitive, but if you think about the variables of the triples as the arguments it makes sense
    *)
    [<Struct>]
    [<StructuralEquality>]
    [<StructuralComparison>]
    type Relation =
        | BinaryPredicate of ResourceId
        | UnaryPredicate of predicate : ResourceId * obj : ResourceId 
    
    
    (* A relation matches a triple if the predicate matches a binary relation or if both predicate and object matches a unary relation *)
    let MatchTripleRelation (triple : TriplePattern) (relation : Relation)  : bool =
        match relation with
        | BinaryPredicate res -> match triple.Predicate with
                                    | ResourceOrVariable.Resource res' -> res = res'
                                    | _ -> false
        | UnaryPredicate (res, obj) -> match triple.Predicate, triple.Object with
                                        | ResourceOrVariable.Resource res', ResourceOrVariable.Resource obj' -> res = res' && obj = obj'
                                        | _ -> false
    let MatchRelations (rel1) (rel2) : bool =
        match rel1, rel2 with
        | BinaryPredicate res1, BinaryPredicate res2 -> res1 = res2
        | UnaryPredicate (res1, obj1), UnaryPredicate (res2, obj2) -> res1 = res2 && obj1 = obj2
        | UnaryPredicate (res1p, res1o), BinaryPredicate res2 -> res1p = res2
        | BinaryPredicate res1, UnaryPredicate (res2p, res2o) -> res1 = res2p
    
    [<Struct>]
    [<StructuralEquality>]
    [<StructuralComparison>]
    type RelationEdge =
        | PositiveRelationEdge of pedge:  int
        | NegativeRelationEdge of nedge: int
    
    [<Struct>]
    [<StructuralEquality>]
    [<StructuralComparison>]
    type OrderedRelation = {
        Relation : Relation
        mutable Successors : RelationEdge list
        mutable num_predecessors : int
        mutable uses_negative_edge : bool
    }
    let GetTriplePatternRelation (triple : TriplePattern) : Relation =
            match triple.Predicate with
                                        | ResourceOrVariable.Variable _ -> failwith "The predicate/relation in a datalog rule head cannot be a variable. "
                                        | ResourceOrVariable.Resource res -> match triple.Object with
                                                                                    | ResourceOrVariable.Variable _ -> (BinaryPredicate res)
                                                                                    | ResourceOrVariable.Resource obj -> (UnaryPredicate (res, obj))
    
    let GetRuleAtomRelation (atom : RuleAtom) : Relation =
        let triple = match atom with
                        | PositiveTriple t -> t
                        | NotTriple t -> t
        GetTriplePatternRelation triple                        
                 
    (* The intensional relations (properties) are those that occur in the head of at least one rule *)       
    let GetIntentionalRelations (rules : Rule list)  =
        rules
        |> Seq.map (fun rule -> rule.Head |> GetTriplePatternRelation)
        |> Seq.distinct
    
    (* The extensional relations (properties) are those that only occur in the body of rules *)       
    let GetExtentionalRelations (rules : Rule list)  =
        let intentionalRelations = GetIntentionalRelations rules
        let bodyRules = rules
                            |> Seq.collect (fun rule ->
                                rule.Body |> Seq.map GetRuleAtomRelation
                                )
        bodyRules |> Seq.except intentionalRelations |> Seq.distinct
  
    let GetRelations (rules : Rule list) =
        let intentionalRelations = GetIntentionalRelations rules
        let bodyRules = rules
                            |> Seq.collect (fun rule ->
                                rule.Body |> Seq.map GetRuleAtomRelation
                                )
        Seq.concat [intentionalRelations ; bodyRules] |> Seq.distinct
    
    (* Returns any rules containing negations of intentional properties. These relations make the program not semipositive *)
    let NegativeIntentionalProperties (rules : Rule list) =
        let intentionalRelations =
                rules
                |> GetIntentionalRelations
        rules |> Seq.filter (fun rule ->
                            rule.Body |> Seq.exists (fun atom ->
                                match atom with
                                    | NotTriple t -> intentionalRelations |> Seq.exists (MatchTripleRelation t)
                                    | _ -> false
                                ) 
                            )
          
    (* A datalog program is semi-positive if negations only occur on extentional relations (Relations that do not occur in the head of any rule) *)
    let IsSemiPositiveProgram (rules : Rule list) =
        NegativeIntentionalProperties rules |> Seq.isEmpty
  
    type Edge =
        | PositiveEdge
        | NegativeEdge
    
    type RulePartitioner(rules: Rule list) =
        
        let relations = GetRelations rules |> Seq.toArray
        let relationMap = relations |> Array.mapi (fun i r -> r, i) |> Map.ofArray
        
        let updateRelation (_ordered : OrderedRelation array) (headRelationNo : int) (atom : RuleAtom) =
                match atom with
                | NotTriple t ->
                    let atomRelation = t |> GetTriplePatternRelation
                    let atomRelationNo = relationMap.[atomRelation]
                    _ordered.[int atomRelationNo].Successors <- NegativeRelationEdge headRelationNo :: _ordered.[int atomRelationNo].Successors
                    _ordered.[int headRelationNo].num_predecessors <- _ordered.[int headRelationNo].num_predecessors + 1
                    _ordered.[int atomRelationNo].uses_negative_edge <- true
                | PositiveTriple t ->
                    let atomRelation = t |> GetTriplePatternRelation
                    let atomRelationNo = relationMap.[atomRelation]
                    _ordered.[int atomRelationNo].Successors <- PositiveRelationEdge headRelationNo :: _ordered.[int atomRelationNo].Successors
                    _ordered.[int headRelationNo].num_predecessors <- _ordered.[int headRelationNo].num_predecessors + 1
        
        let mutable ordered = 
            let _ordered = relations |> Array.map (fun r -> {Relation = r; Successors = []; num_predecessors = 0; uses_negative_edge = false})
            rules |> Seq.iter (fun rule ->
                                        let headRelation = rule.Head |> GetTriplePatternRelation
                                        let headRelationNo = relationMap.[headRelation]
                                        let mutable headRelationEntry = _ordered.[int headRelationNo]
                                        rule.Body
                                           |> Seq.iter (updateRelation _ordered headRelationNo)
                                        _ordered.[int headRelationNo] <- headRelationEntry
                                )
            _ordered
        
        (* The queue contains all relations that are not dependent on any relations (that have not already been output) *)
        let mutable ready_elements_queue =
            Queue<int>(ordered |> Array.filter (fun concept -> concept.num_predecessors = 0)
                    |> Array.map (fun concept -> relationMap.[concept.Relation]))
            
        (* The concepts that depended on a negation of a concept that is being output in the current stratification must wait till the next layer *)
        let mutable next_elements_queue = Queue<int>()
        let mutable n_unordered = Array.length ordered - ready_elements_queue.Count
            
        member this.GetReadyElementsQueue() = ready_elements_queue    
            
        (* Between iterations, the switch about using negative edge must be reset,
            and all elements marked for next stratifications must be moved into the queue for the current stratification *)    
        member this.reset_stratification =
            for i in 0 .. (Array.length ordered - 1) do
                ordered.[i].uses_negative_edge <- false 
            while next_elements_queue.Count > 0 do
                ready_elements_queue.Enqueue(next_elements_queue.Dequeue())
            
            
        (* Updates the successor of a relation, and if the relation is ready to be output, it is added to the queue *)
        member this.update_successor (successor : RelationEdge) =
            let relation_id = 
                match successor with
                | PositiveRelationEdge relation_id ->
                    relation_id
                | NegativeRelationEdge relation_id ->
                    ordered.[int relation_id].uses_negative_edge <- true
                    relation_id
            let relation = ordered.[int relation_id]
            ordered.[int relation_id].num_predecessors <- ordered.[int relation_id].num_predecessors - 1
            if relation.num_predecessors = 0 then
                if relation.uses_negative_edge then
                    next_elements_queue.Enqueue(relation_id)
                else
                    ready_elements_queue.Enqueue(relation_id)
            
            
        (* Gets all rules that are not part of a cycle. This will become a partition of the stratification
            After this is run, the remaining elements in "ordered" contain at least one cycle, and all elements are either in
            a cycle or depend on an element in a cycle *)
        member this.get_rule_partition() : Rule seq  =
            let mutable ordered_rules = Seq.empty
            this.reset_stratification
            while ready_elements_queue.Count > 0 do
                let relation_id = ready_elements_queue.Dequeue()
                let relation = relations.[int relation_id]
                let relation_rules = rules
                                    |> List.filter (fun rule ->
                                        (rule.Head |> GetTriplePatternRelation) = relation
                                        )
                ordered_rules <- Seq.append relation_rules ordered_rules
                ordered.[int relation_id].Successors |> Seq.iter (this.update_successor)
            Seq.distinct ordered_rules
        
        (* Order the rules topologically based on dependency. Used for stratification *)
        member this.orderRules  : Rule seq seq =
            let mutable stratification = [this.get_rule_partition()]
            while n_unordered > 0 do
                //TODO: Check for cycle with negative, break a cycle
                stratification <- stratification @ [this.get_rule_partition()]
            stratification
            
            
        
        
