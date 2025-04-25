(*
    Copyright (C) 2024,2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)


namespace DagSemTools.Datalog

open System
open System.Collections.Generic
open System.Collections.Immutable
open DagSemTools.Rdf.Ingress
open Serilog
open DagSemTools.Datalog.Unification

(* 
    The Stratifier module intends to create a stratification of a datalog program, such that negation can be supported
    The algorithm is meant to follow the one from the chapters on negation in  Abiteboul, Hull, Vianu: "Foundations of Databases" (1995)
 *)
module internal Stratifier =

    (*
        A binary relation represents a triple atom where the object place is a variable
        A unary predicate reperesents a triple atom where the object plase is a term. F.ex. the type triples
        I realize the names may be counter-intuitive, but if you think about the variables of the triples as the arguments it makes sense
    [<Struct>]
    [<StructuralEquality>]
    [<StructuralComparison>]
    type TriplePattern =
        | AllRelations
        | BinaryPredicate of GraphElementId
        | UnaryPredicate of predicate : GraphElementId * obj : GraphElementId 
    *)
        
    (*let MatchRelations (rel1) (rel2) : bool =
        match rel1, rel2 with
        | AllRelations, _ -> true
        | _, AllRelations -> true
        | BinaryPredicate res1, BinaryPredicate res2 -> res1 = res2
        | UnaryPredicate (res1, obj1), UnaryPredicate (res2, obj2) -> res1 = res2 && obj1 = obj2
        | UnaryPredicate (res1p, res1o), BinaryPredicate res2 -> res1p = res2
        | BinaryPredicate res1, UnaryPredicate (res2p, res2o) -> res1 = res2p
    *)
    
    
    [<Struct>]
    [<CustomEquality>]
    [<CustomComparison>]
    type OrderedRule = {
        Relation : Rule
        mutable Successors : PatternEdge list
        mutable num_predecessors : uint
        mutable uses_intensional_negative_edge : bool
        mutable visited: bool
        mutable output: bool
    }
    with
        override this.Equals (other) =
                       match other with
                       | :? OrderedRule as other ->
                           this.Relation.Equals other.Relation
                       | _ -> false
         override this.GetHashCode () =
               this.Relation.GetHashCode()

        interface System.IComparable with
            member this.CompareTo (obj: obj): int =
                            match obj with
                             | :? OrderedRule as other ->
                                 compare this.Relation other.Relation
                             | _ -> invalidArg "obj" "Cannot compare values of different types."
       
    let createOrderedRules rules  =
        rules 
        |> List.map (fun rule -> 
            { Relation = rule
              num_predecessors = 0u
              Successors = List.empty
              uses_intensional_negative_edge = false
              visited = false
              output = false })
        |> Seq.toArray
           
    let GetRuleHeadPattern (triple : RuleHead)  =
            match triple with
            | Contradiction  -> None
            | NormalHead pattern -> Some pattern
    let GetRuleAtomPattern (atom : RuleAtom)  =
        match atom with
                        | PositiveTriple t -> t
                        | NotTriple t -> t
                 
    let GetBodyTriplePatterns rules = rules
                                    |> Seq.collect (fun rule ->
                                    rule.Body |> Seq.map GetRuleAtomPattern
                                    )
    let GetHeadPattern rules = rules
                                |> Seq.choose (fun rule -> rule.Head |> GetRuleHeadPattern)
                            
                
    (* The intensional triple patterns are those that occur in the head of at least one rule *)       
    let GetIntentionalTriplePatterns (rules : Rule list) =
            GetHeadPattern rules
    
    (* The extensional relations (properties) are those that only occur in the body of rules *)       
    let GetExtentionalTriplePatterns (rules : Rule list)  =
        GetBodyTriplePatterns rules
        |> Seq.except (GetHeadPattern rules)
        |> Seq.distinct
  
    
    (* Returns any rules containing negations of intentional properties. These relations make the program not semipositive *)
    let NegativeIntentionalProperties (rules : Rule list) =
        let intentionalTriplePatterns =
                rules
                |> GetIntentionalTriplePatterns
        rules |> Seq.filter (fun rule ->
                            rule.Body |> Seq.exists (fun atom ->
                                match atom with
                                    | NotTriple t -> intentionalTriplePatterns |> Seq.exists ( triplePatternsUnifiable t)
                                    | _ -> false
                                ) 
                            )
          
    (* A datalog program is semi-positive if negations only occur on extentional relations (Relations that do not occur in the head of any rule) *)
    let IsSemiPositiveProgram (rules : Rule list) =
        NegativeIntentionalProperties rules |> Seq.isEmpty
  
    (* 
        The RulePartitioner creates a stratification of the program if it is stratifiable, and otherwise fails
        Based on the Alice book by Abiteboul, Hull and Vianu, the chapters on datalog with negation
        
        The main difference is that the rules, and not the relations are ordered. 
        This was inspired by a sentence in "Maintenance of datalog materialisations revisited" by Motik, Nenov, Piro and Horrocks
     *)
    type internal RulePartitioner (logger: ILogger, rules: Rule list, resources: DagSemTools.Rdf.GraphElementManager) =
        let ruleMap=
           rules
           |> Seq.mapi (fun i r -> r, (uint i))
           |> Map.ofSeq
        
        (* 
            This is the core of a topological sorting of the rules.
            Based on the algorithm in Knuths Art of Computer Programming, chapter 2
            
            This method is called on startup for each rule
        *)
        let updateRuleOrdering (_ordered : OrderedRule array) rule (edge : PatternEdge)   =
            let ruleNo = ruleMap.[rule]
            let depRule = edge.GetRule()
            let depRuleNo = ruleMap.[depRule]
            _ordered.[int ruleNo].Successors <- edge :: _ordered.[int ruleNo].Successors
            _ordered.[int depRuleNo].num_predecessors <- _ordered.[int depRuleNo].num_predecessors + 1u
            
        (* This is only run once on initialization to set up the data structure for topologial sorting of rules  *)
        let mutable orderedRules = 
            let _ordered = rules |> createOrderedRules
            rules
            |> Seq.choose (fun rule ->
                match rule.Head |> GetRuleHeadPattern with
                | None -> None
                | Some headPattern -> Some (rule, headPattern))
            |> Seq.iter (fun (rule, headPattern) ->
                DependingRules rules headPattern
                |> Seq.iter (updateRuleOrdering _ordered rule))
            _ordered
        
        let GetInitialPartition =
            orderedRules
                    |> Array.filter (fun rule -> rule.num_predecessors < 1u)
            //        |> Array.map (fun concept -> ruleMap.[concept.Relation])
                   
        
        (* The queue contains all relations that are not dependent on any relations (that have not already been output) *)
        let mutable ready_elements_queue =
            
            Array.fold (fun (queue : ImmutableQueue<OrderedRule>) element -> queue.Enqueue element) ImmutableQueue<OrderedRule>.Empty  GetInitialPartition
            
        (* The concepts that depended on a negation of a concept that is being output in the current stratification must wait till the next layer *)
        let mutable next_elements_queue = ImmutableQueue<OrderedRule>.Empty
        //let mutable n_unordered = Array.length orderedRules - ready_elements_queue.
        
        member internal this.printCycle cycle =
            cycle
            |> Seq.map (fun n -> $"Cycle element: {resources.GetGraphElement n}")
            |> String.concat ", "
            
        member internal this.find_cycle visited current relation is_negative =
                let relation_id = ruleMap.[relation]
                let cycleFinder = this.cycle_finder (Seq.append visited [current]) relation_id
                if is_negative && (cycleFinder |> Seq.isEmpty |> not) then
                    let cycleString = cycleFinder |> Seq.head |>  this.printCycle
                    logger.Error($"Datalog program contains a cycle with negation and is not stratifiable! {cycleString}")
                    failwith $"Datalog program contains a cycle with negation and is not stratifiable! {cycleString}"
                else
                    cycleFinder                
                
        
        (* Called when the topological sorting cannot proceed, hence assuming the existence of a cycle *)
        member internal this.cycle_finder visited current  =
            let current_element = orderedRules.[int current]
            if (visited |> Seq.contains current) then
                visited |> Seq.skipWhile (fun id -> id <> current) |> Seq.distinct |> Seq.singleton
            else if current_element.visited then Seq.empty
            else
                // TODO: Enable this optimisation when the cycle finder is working
                // ordered_relations.[current].visited <- true
                current_element.Successors |> Seq.collect
                                                    (fun edge ->
                                                        match edge with
                                                        | PositivePatternEdge relation_id -> 
                                                            this.find_cycle (Seq.append visited [current]) current relation_id false
                                                        | NegativePatternEdge relation_id ->
                                                            this.find_cycle (Seq.append visited [current]) current relation_id true
                                                    )
           
        member internal this.GetReadyElementsQueue() = ready_elements_queue    
        member internal this.GetOrderedRules() = orderedRules
            
        (* Between iterations, the switch about using negative edge must be reset,
            and all elements marked for next stratifications must be moved into the queue for the current stratification *)    
        member internal this.reset_stratification () =
            for i in 0 .. (Array.length orderedRules - 1) do
                orderedRules.[i].uses_intensional_negative_edge <- false
                orderedRules.[i].visited <- false
            while not next_elements_queue.IsEmpty do
                let mutable next_element = 0
                let (next_elements_queue_new, next_element_val) = next_elements_queue.Dequeue()
                next_elements_queue <- next_elements_queue_new
                ready_elements_queue <- ready_elements_queue.Enqueue(next_element_val)
            
            
        (* Called on all successors of a rule that is being output.
          Updates the successor rules, and if the relation is ready to be output, it is added to the queue *)
        member internal this.update_successor (removedRuleId : uint) (successor : PatternEdge) =
            let successorRuleId = 
                match successor with
                | PositivePatternEdge rule ->
                    ruleMap.[rule]
                | NegativePatternEdge rule ->
                    let removed_relation = orderedRules.[int removedRuleId]
                    let relation_id = ruleMap.[rule]
                    orderedRules.[int relation_id].uses_intensional_negative_edge <- true
                    relation_id
            let old_relation = orderedRules.[int successorRuleId]
            if not old_relation.output then        
                if old_relation.num_predecessors < 1u then failwith "Datalog program preprocessing failed. This is a bug, please report that topological ordering failed, num_predecessors < 1"
                let new_predecessors = old_relation.num_predecessors - 1u
                orderedRules.[int successorRuleId] <- { old_relation with num_predecessors = new_predecessors }
                if orderedRules.[int successorRuleId].num_predecessors = 0u && not orderedRules.[int successorRuleId].output then
                    orderedRules.[int successorRuleId].output <- true
                    if orderedRules.[int successorRuleId].uses_intensional_negative_edge then
                        next_elements_queue <- next_elements_queue.Enqueue(orderedRules[int successorRuleId])
                    else
                        ready_elements_queue <- ready_elements_queue.Enqueue(orderedRules[int successorRuleId])
                
            
        (* Gets all rules that are not part of a cycle. This will become a partition of the stratification
            After this is run, the remaining elements in "ordered" contain at least one cycle, and all elements are either in
            a cycle or depend on an element in a cycle *)
        member internal this.get_rule_partition()   =
            let mutable outputPartition = List.empty
            while not ready_elements_queue.IsEmpty do
                let (ready_elements_queue_new, ruleToOutput) = ready_elements_queue.Dequeue()
                ready_elements_queue <- ready_elements_queue_new
                let ruleToOutputId = ruleMap.[ruleToOutput.Relation]    
                orderedRules.[int ruleToOutputId].Successors |> Seq.iter (this.update_successor ruleToOutputId)
                // if ordered_relations.[relation_id].intensional then
                outputPartition <- [ ruleToOutput.Relation ] @  outputPartition 
                //orderedRules.[int ruleId].intensional <- false
            (outputPartition |> Seq.toList)
        
        (* Checks whether a rule is completely covered by a cycle (given the already output relations, which are treated as extensional/edb
            In other words, whether none of the remaining rules not output or not in a cycle can affect the rule body
        *)
        member internal this.RuleIsCoveredByCycle (cycle : uint seq) rule =
                let remainingRules =
                    orderedRules
                    |> Array.filter (fun rule ->
                        (not rule.output)
                        && (not (Seq.contains (ruleMap.[rule.Relation]) cycle)))
                    |> Array.map (fun rule -> rule.Relation)
                rule.Body
                |> Seq.forall (fun atom ->
                    let atomRelation = atom|> GetRuleAtomPattern 
                    IntentionalRules remainingRules atomRelation
                    |> Seq.isEmpty
                )
        (* 
            Only called when topological sorting stops, so there is a cycle
            Finds one cycle, if that contains a negative edge, reports error, otherwise,
            checks whether the first relation in the cycle is ready to be output, and if so, outputs it
            An example of where it is not ready, is if it depends on another cycle, which needs to be output first. 
            TODO: Checking for negative edge in cycle is gone
            The proof that these cycles / strongly connected components always exist is in the Alice book
         *)
        member internal this.handle_cycle()  =
              let predecessorsNotOutput =
                  orderedRules
                  |> Array.filter (fun rule -> rule.num_predecessors > 0u
                                                    && rule.output = false)
              
                  |> Array.map (fun rule -> ruleMap.[rule.Relation])
              let foundCycles = predecessorsNotOutput |> Seq.collect (this.cycle_finder [])
              let coveredCycles =
                  foundCycles
                  |> Seq.filter (fun cycle ->
                      cycle
                        |> Seq.map (fun ruleIndex ->
                            rules.[int ruleIndex] )
                        |> Seq.forall (this.RuleIsCoveredByCycle cycle)
                )         
              let cycles = coveredCycles
              cycles |> Seq.iter (fun cycle ->
                  cycle |> Seq.distinct |>(Seq.iter (fun rel ->
                      if not orderedRules.[int rel].output then 
                        orderedRules.[int rel].output <- true
                        ready_elements_queue <- ready_elements_queue.Enqueue orderedRules.[int rel])
                  ))
                
            
        (*  Catches some errors in stratification, to avoid a wrong stratification being returned
            TODO: Remove when stratification is stable and tests cover all corners
        *)
        member internal this.is_stratified (stratification) =
            ready_elements_queue.IsEmpty
            && next_elements_queue.IsEmpty
            // && (stratification |> Seq.sumBy Seq.length) >= rules.Length
            // && ordered_relations |> Array.forall (fun relation -> relation.intensional = false)

        (* Used in the while loop in orderRules to test whether stratification is finished *)
        member internal this.topological_sort_finished() =
            orderedRules |> Array.forall (fun relation -> relation.output || relation.num_predecessors = 0u)
                
        (* Order the rules topologically based on dependency. Used for stratification
            Each Rule seq in the outermost seq is a partition, and these partitions must be handled sequentially during materialization *)
        member this.orderRules()  :  Rule seq seq =
            let mutable stratification = []
            if ready_elements_queue.IsEmpty then
                    this.handle_cycle()
            while not ready_elements_queue.IsEmpty do
                stratification <- stratification @ [this.get_rule_partition() |> List.toSeq]
                this.reset_stratification()
                if ready_elements_queue.IsEmpty  && (not (this.topological_sort_finished ()))  then
                    this.handle_cycle()
                    
            if not (this.is_stratified stratification) then
                 failwith "Datalog program preprocessing created wrong stratification! This is a bug, please report"
            stratification |> List.toSeq
            