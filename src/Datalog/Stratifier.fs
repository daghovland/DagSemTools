(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)


namespace DagSemTools.Datalog

open System.Collections.Generic
open DagSemTools.Rdf.Ingress
open DagSemTools.Datalog.Datalog

(* 
    The Stratifier module intends to create a stratification of a datalog program, such that negation can be supported
    The algorithm is meant to follow the one from the chapters on negation in  Abiteboul, Hull, Vianu: "Foundations of Databases" (1995)
 *)
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
        | AllRelations
        | BinaryPredicate of ResourceId
        | UnaryPredicate of predicate : ResourceId * obj : ResourceId 
    
    
    (* A relation matches a triple if the predicate matches a binary relation or if both predicate and object matches a unary relation *)
    let MatchTripleRelation (triple : TriplePattern) (relation : Relation)  : bool =
        match relation with
        | AllRelations -> true
        | BinaryPredicate res -> match triple.Predicate with
                                    | ResourceOrVariable.Resource res' -> res = res'
                                    | _ -> false
        | UnaryPredicate (res, obj) -> match triple.Predicate, triple.Object with
                                        | ResourceOrVariable.Resource res', ResourceOrVariable.Resource obj' -> res = res' && obj = obj'
                                        | _ -> false
        
    let MatchRelations (rel1) (rel2) : bool =
        match rel1, rel2 with
        | AllRelations, _ -> true
        | _, AllRelations -> true
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
        mutable num_predecessors : uint
        mutable uses_intensional_negative_edge : bool
        mutable intensional : bool
        mutable visited: bool
        mutable output: bool
    }
    let GetTriplePatternRelation (triple : TriplePattern) : Relation =
            match triple.Predicate with
                                        | ResourceOrVariable.Variable _ -> AllRelations
                                        | ResourceOrVariable.Resource res -> match triple.Object with
                                                                                    | ResourceOrVariable.Variable _ -> (BinaryPredicate res)
                                                                                    | ResourceOrVariable.Resource obj -> (UnaryPredicate (res, obj))
    
    let GetRuleAtomRelation (atom : RuleAtom) : Relation =
        let triple = match atom with
                        | PositiveTriple t -> t
                        | NotTriple t -> t
        GetTriplePatternRelation triple
        // match GetTriplePatternRelation triple with
        // | AllRelations -> None
        // | r -> Some r
                 
    let GetBodyRelations rules = rules
                                |> Seq.collect (fun rule ->
                                    rule.Body |> Seq.map GetRuleAtomRelation
                                    )
    let GetHeadRelations rules = rules
                                |> Seq.map (fun rule -> rule.Head |> GetTriplePatternRelation)
                            
                
    (* The intensional relations (properties) are those that occur in the head of at least one rule *)       
    let GetIntentionalRelations (rules : Rule list)  =
        let headRelations =
            GetHeadRelations rules
        if headRelations |> Seq.exists (fun r -> r = AllRelations) then
            Seq.concat [headRelations ; GetBodyRelations rules ] |> Seq.distinct
        else
            headRelations
    
    (* The extensional relations (properties) are those that only occur in the body of rules *)       
    let GetExtentionalRelations (rules : Rule list)  =
        if rules |> Seq.exists (fun rule -> rule.Head |> GetTriplePatternRelation = AllRelations) then
            Seq.empty
        else
            GetBodyRelations rules |> Seq.except (GetHeadRelations rules) |> Seq.distinct
  
    let GetRelations (rules : Rule list) =
        Seq.concat [GetHeadRelations rules ; GetBodyRelations rules] |> Seq.distinct
    
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
  
    (* The RulePartitioner creates a stratification of the program if it is stratifiable, and otherwise fails *)
    type RulePartitioner(rules: Rule list) =
        
        let relations = GetRelations rules |> Seq.toArray
        let relationMap = relations |> Array.mapi (fun i r -> r, i) |> Map.ofArray
        
        (* 
            This is the core of a topoogical sorting of the relations.
            Based on the algorithm in Knuths Art of Computer Programming, chapter 2
            
            The treatment of the "wildcard" triplepattern (ternary relation?) AllVariables is my addition
            I have not proved this addition correct
            
            This method is called whenever a relation is removed from the queue of relations ready for ouput
            
        *)
        let updateAtom (_ordered : OrderedRelation array) relationEdgeType (headRelationNo : int) (bodyTriplePattern : TriplePattern) =
            let atomRelation = bodyTriplePattern |> GetTriplePatternRelation
            let numRelations = Array.length _ordered
            let atomRelationNo = relationMap.[atomRelation]
            _ordered.[int atomRelationNo].Successors <- relationEdgeType headRelationNo :: _ordered.[int atomRelationNo].Successors
            _ordered.[int headRelationNo].num_predecessors <- _ordered.[int headRelationNo].num_predecessors + 1u
            
        (* Adds a dependency to and from the wildcard-relation *)
        let updateWildcardRelation (_ordered: OrderedRelation array) =
            match relationMap.TryGetValue AllRelations with
            | false, _ -> ()
            | true, wildcardRelationNo ->
                for i in 0 .. (Array.length _ordered - 1) do
                    if i <> wildcardRelationNo then
                        _ordered.[i].Successors <- PositiveRelationEdge wildcardRelationNo :: _ordered.[i].Successors
                        _ordered.[wildcardRelationNo].num_predecessors <- _ordered.[wildcardRelationNo].num_predecessors + 1u
            _ordered
                    
        let updateRelation (_ordered : OrderedRelation array) (headRelationNo : int) (ruleBodyAtom : RuleAtom) =
                _ordered.[int headRelationNo].intensional <- true
                match ruleBodyAtom with
                | NotTriple t ->
                    updateAtom _ordered NegativeRelationEdge headRelationNo t
                | PositiveTriple t ->
                    updateAtom _ordered PositiveRelationEdge headRelationNo t
        
        (* This is only run once on initialization to set up the data structure for topologial sorting of relations *)
        let mutable ordered_relations = 
            let _ordered = relations |> Array.map (fun r -> {Relation = r
                                                             Successors = []
                                                             num_predecessors = 0u
                                                             uses_intensional_negative_edge = false
                                                             intensional = false
                                                             visited = false
                                                             output = false
                                                             })
            rules |> Seq.iter (fun rule ->
                                        let headRelation = rule.Head |> GetTriplePatternRelation
                                        let headRelationNo = relationMap.[headRelation]
                                        rule.Body
                                           |> Seq.iter (updateRelation _ordered headRelationNo)
                                )
            updateWildcardRelation _ordered
        
        (* The queue contains all relations that are not dependent on any relations (that have not already been output) *)
        let mutable ready_elements_queue =
            Queue<int>(ordered_relations |> Array.filter (fun concept -> concept.num_predecessors = 0u)
                    |> Array.map (fun concept -> relationMap.[concept.Relation]))
            
        (* The concepts that depended on a negation of a concept that is being output in the current stratification must wait till the next layer *)
        let mutable next_elements_queue = Queue<int>()
        let mutable n_unordered = Array.length ordered_relations - ready_elements_queue.Count
        
        
        member this.cycle_finder (visited : int seq) (current : int) : int seq option =
            let current_element = ordered_relations.[current]
            if (visited |> Seq.contains current) then
                visited |> Seq.skipWhile (fun id -> id <> current) |> Seq.distinct  |> Some
            else if current_element.visited then None
            else
                // TODO: Enable this optimisation when the cycle finder is working
                // ordered_relations.[current].visited <- true
                current_element.Successors |> Seq.choose
                                                    (fun edge ->
                                                        match edge with
                                                        | PositiveRelationEdge relation_id -> 
                                                            (this.cycle_finder (Seq.append visited [current]) relation_id)
                                                        | NegativeRelationEdge relation_id ->
                                                            if (this.cycle_finder (Seq.append visited [current]) relation_id |> Option.isSome) then
                                                                // TODO: Output cycle
                                                                failwith "Datalog program contains a cycle with negation and is not stratifiable!"
                                                            else
                                                                None
                                                    )
                                            |> Seq.tryHead
            
            
        member this.GetReadyElementsQueue() = ready_elements_queue    
        member this.GetOrderedRelations() = ordered_relations
            
        (* Between iterations, the switch about using negative edge must be reset,
            and all elements marked for next stratifications must be moved into the queue for the current stratification *)    
        member this.reset_stratification =
            for i in 0 .. (Array.length ordered_relations - 1) do
                ordered_relations.[i].uses_intensional_negative_edge <- false
                ordered_relations.[i].visited <- false
            while next_elements_queue.Count > 0 do
                ready_elements_queue.Enqueue(next_elements_queue.Dequeue())
            
            
        (* Updates a successor of a relation, and if the relation is ready to be output, it is added to the queue *)
        member this.update_successor (removed_relation_id : int) (successor : RelationEdge) =
            let relation_id = 
                match successor with
                | PositiveRelationEdge relation_id ->
                    relation_id
                | NegativeRelationEdge relation_id ->
                    let removed_relation = ordered_relations.[removed_relation_id]
                    if removed_relation.intensional then
                        ordered_relations.[relation_id].uses_intensional_negative_edge <- true
                    relation_id
            let old_relation = ordered_relations.[relation_id]
            if not old_relation.output then        
                if old_relation.num_predecessors < 1u then failwith "Datalog program preprocessing failed. This is a bug, please report that topological ordering failed, num_predecessors < 1"
                let new_predecessors = old_relation.num_predecessors - 1u
                ordered_relations.[relation_id] <- { old_relation with num_predecessors = new_predecessors }
                if ordered_relations.[relation_id].num_predecessors = 0u && not ordered_relations.[relation_id].output then
                    ordered_relations.[relation_id].output <- true
                    if ordered_relations.[relation_id].uses_intensional_negative_edge then
                        next_elements_queue.Enqueue(relation_id)
                    else
                        ready_elements_queue.Enqueue(relation_id)
                
            
        (* Gets all rules that are not part of a cycle. This will become a partition of the stratification
            After this is run, the remaining elements in "ordered" contain at least one cycle, and all elements are either in
            a cycle or depend on an element in a cycle *)
        member this.get_rule_partition() : Rule seq  =
            let mutable ordered_rules = Seq.empty
            while ready_elements_queue.Count > 0 do
                let relation_id = ready_elements_queue.Dequeue()
                let relation = relations.[relation_id]    
                ordered_relations.[int relation_id].Successors |> Seq.iter (this.update_successor relation_id)
                if ordered_relations.[relation_id].intensional then
                    let relation_rules = rules
                                        |> List.filter (fun rule ->
                                            (rule.Head |> GetTriplePatternRelation) = relation
                                            )
                    ordered_rules <- Seq.append relation_rules ordered_rules
                    ordered_relations.[int relation_id].intensional <- false
            Seq.distinct ordered_rules
        
        (* Checks whether a rule is completely covered by a cycle (given the already output relations, which are treated as extensional/edb
            In other words, whether all elements of the body are in the cycle, or are extensional*)
        member this.RuleIsCoveredByCycle cycle rule =
                rule.Body |> Seq.forall (fun atom ->
                                            let atomRelation = atom|> GetRuleAtomRelation 
                                            ordered_relations.[relationMap.[atomRelation]].intensional = false
                                            || (cycle |> Seq.exists (MatchRelations atomRelation))
                                        )
        (* 
            Only called when topological sorting stops, so there is a cycle
            Finds one cycle, if that contains a negative edge, reports error, otherwise,
            checks whether the first relation in the cycle is ready to be output, and if so, outputs it
            An example of where it is not ready, is if it depends on another cycle, which needs to be output first. 
            
            The proof that these cycles / strongly connected components always exist is in the Alice book
         *)
        member this.handle_cycle()  =
            match (ordered_relations
                  |> Array.filter (fun relation -> relation.num_predecessors > 0u
                                                    && relation.output = false)
                  |> Array.map (fun relation -> relationMap.[relation.Relation])
                  |> Array.choose (this.cycle_finder [])
                  |> Array.filter (fun cycle ->
                            let cycle_element = cycle |> Seq.head
                            let rules = rules
                                        |> List.filter (fun rule ->
                                            (rule.Head |> GetTriplePatternRelation) = relations.[cycle_element]
                                            )
                            rules |> List.forall (this.RuleIsCoveredByCycle (cycle |> Seq.map (fun id -> relations.[id])))
                            )
                            
                  |> Seq.tryHead)
            with
            | Some cycle ->
                  cycle |> Seq.distinct |>(Seq.iter (fun rel ->
                      if not ordered_relations.[rel].output then 
                        ordered_relations.[rel].output <- true
                        ready_elements_queue.Enqueue rel)
                  )
            | None -> failwith "Datalog program preprocessing failed. This is a bug, please report"
                
            
        (*  Catches some errors in stratification, to avoid a wrong stratification being returned
            TODO: Remove when stratification is stable and tests cover all corners
        *)
        member this.is_stratified (stratification: Rule seq seq) =
            ready_elements_queue.Count = 0
            && next_elements_queue.Count = 0
            && (stratification |> Seq.sumBy Seq.length) = rules.Length
            && ordered_relations |> Array.forall (fun relation -> relation.intensional = false)

        (* Used in the while loop in orderRules to test whether stratification is finished *)
        member this.topological_sort_finished() =
            ordered_relations |> Array.forall (fun relation -> relation.output || relation.num_predecessors = 0u)
                
        (* Order the rules topologically based on dependency. Used for stratification
            Each Rule seq in the outermost seq is a partition, and these partitions must be handled sequentially during materialization *)
        member this.orderRules  : Rule seq seq =
            let mutable stratification = []
            if ready_elements_queue.Count = 0 then
                    this.handle_cycle()
            while ready_elements_queue.Count > 0 do
                stratification <- stratification @ [this.get_rule_partition()]
                this.reset_stratification
                if ready_elements_queue.Count = 0  && (not (this.topological_sort_finished ()))  then
                    this.handle_cycle()
                    
            if not (this.is_stratified stratification) then
                 failwith "Datalog program preprocessing failed! This is a bug, please report"
            stratification
            