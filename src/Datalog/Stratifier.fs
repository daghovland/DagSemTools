(*
    Copyright (C) 2024,2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)


namespace DagSemTools.Datalog

open System.Collections.Generic
open DagSemTools.Rdf.Ingress
open Serilog

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
    let VariableConstantUnifiable v res (constantMap : Map<string, GraphElementId>)  (variableMap : Map<string, string>)=
        match constantMap.TryGetValue (variableMap.GetValueOrDefault (v, v)) with
        | false, _ -> Some (Map.add v res constantMap, variableMap)
        | true, res2 ->  if res = res2 then Some (constantMap, variableMap) else None
        
    (* Two terms are unifiable if they can be mapped to the same constant *)
    let internal TermsUnifiable (term1 : Term) (term2 : Term) (constantMap : Map<string, GraphElementId>, variableMap : Map<string, string>)=
        match term1, term2 with
        | Term.Variable v, Term.Resource res1 ->
           VariableConstantUnifiable v res1 constantMap variableMap
        | Term.Resource res, Term.Variable v ->
           VariableConstantUnifiable v res constantMap variableMap
        | Term.Resource res1, Term.Resource res2 -> if res1 = res2 then (Some (constantMap, variableMap)) else None
        | Term.Variable v1, Term.Variable v2 -> (Some (constantMap, (Map.add v1 (v2) variableMap)))
    
    (* Two triple patterns are unifiable if there exists a mapping of the variables such that they are equal *)
    let internal triplePatternsUnifiable (triple1 : TriplePattern) (triple2 : TriplePattern)  =
        TermsUnifiable triple1.Subject triple2.Subject (Map.empty, Map.empty)
        |> Option.bind (TermsUnifiable triple1.Predicate triple2.Predicate)
        |> Option.bind (TermsUnifiable triple1.Object triple2.Object)
        |> Option.isSome
        
    (*let MatchRelations (rel1) (rel2) : bool =
        match rel1, rel2 with
        | AllRelations, _ -> true
        | _, AllRelations -> true
        | BinaryPredicate res1, BinaryPredicate res2 -> res1 = res2
        | UnaryPredicate (res1, obj1), UnaryPredicate (res2, obj2) -> res1 = res2 && obj1 = obj2
        | UnaryPredicate (res1p, res1o), BinaryPredicate res2 -> res1p = res2
        | BinaryPredicate res1, UnaryPredicate (res2p, res2o) -> res1 = res2p
    *)
    
    (*
        These are the edges in the dependency graph of triple patterns
        There is an edge from the pattern in the head of a rule to every atom in the body
        The edge is negative if the body atom is negative
        The stratification checks for cycles with negative edges
        See the Alice book for more info
    *)
    [<Struct>]
    [<StructuralEquality>]
    [<StructuralComparison>]
    type PatternEdge =
        | PositivePatternEdge of pedge: uint
        | NegativePatternEdge of nedge: uint
    
    [<Struct>]
    [<CustomEquality>]
    [<CustomComparison>]
    type TriplePatternEquivalenceClass = {
        Relation : TriplePattern
        mutable Successors : PatternEdge list
        mutable num_predecessors : uint
        mutable uses_intensional_negative_edge : bool
        mutable intensional : bool
        mutable visited: bool
        mutable output: bool
    }
    with
        override this.Equals (other) =
                       match other with
                       | :? TriplePatternEquivalenceClass as other ->
                           this.Relation.Equals other.Relation
                       | _ -> false
         override this.GetHashCode () =
               this.Relation.GetHashCode()

        interface System.IComparable with
            member this.CompareTo (obj: obj): int =
                            match obj with
                             | :? TriplePatternEquivalenceClass as other ->
                                 compare this.Relation other.Relation
                             | _ -> invalidArg "obj" "Cannot compare values of different types."
       
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
  
    let GetTriplePatterns (rules : Rule list) =
        Seq.concat [GetHeadPattern rules ; GetBodyTriplePatterns rules]
        |> Seq.map (fun r -> {
                                                                 Relation = r
                                                                 Successors = []
                                                                 num_predecessors = 0u
                                                                 uses_intensional_negative_edge = false
                                                                 intensional = false
                                                                 visited = false
                                                                 output = false
                                                             })
        |> Seq.distinct
    
    (* Returns any rules containing negations of intentional properties. These relations make the program not semipositive *)
    let NegativeIntentionalProperties (rules : Rule list) =
        let intentionalTriplePatterns =
                rules
                |> GetIntentionalTriplePatterns
        rules |> Seq.filter (fun rule ->
                            rule.Body |> Seq.exists (fun atom ->
                                match atom with
                                    | NotTriple t -> intentionalTriplePatterns |> Seq.exists (triplePatternsUnifiable t)
                                    | _ -> false
                                ) 
                            )
          
    (* A datalog program is semi-positive if negations only occur on extentional relations (Relations that do not occur in the head of any rule) *)
    let IsSemiPositiveProgram (rules : Rule list) =
        NegativeIntentionalProperties rules |> Seq.isEmpty
  
    (* The RulePartitioner creates a stratification of the program if it is stratifiable, and otherwise fails *)
    type internal RulePartitioner (logger: ILogger, rules: Rule list, resources: DagSemTools.Rdf.GraphElementManager) =
        
        let triplePatterns = GetTriplePatterns rules |> Seq.toArray
        let triplePatternMap  = triplePatterns |> Array.mapi (fun i r -> r, (uint i)) |> Map.ofArray
        
        (* 
            This is the core of a topoogical sorting of the triple-patterns.
            Based on the algorithm in Knuths Art of Computer Programming, chapter 2
            
            This method is called whenever a triple-pattern is removed from the queue of patterns ready for ouput
        *)
        let updateAtom (_ordered : TriplePatternEquivalenceClass array) relationEdgeType (headRelationNo : uint) (bodyTriplePattern) =
            let numRelations = Array.length _ordered
            let patternNo = triplePatternMap.[bodyTriplePattern]
            _ordered.[int patternNo].Successors <- relationEdgeType headRelationNo :: _ordered.[int patternNo].Successors
            _ordered.[int headRelationNo].num_predecessors <- _ordered.[int headRelationNo].num_predecessors + 1u
            
        (* Adds a dependency to and from the wildcard-relation 
        let updateWildcardRelation (_ordered: OrderedTriplePattern array) =
            match triplePatternMap.TryGetValue AllRelations with
            | false, _ -> ()
            | true, wildcardRelationNo ->
                for i in 0 .. (Array.length _ordered - 1) do
                    if (uint i) <> wildcardRelationNo then
                        _ordered.[i].Successors <- PositiveRelationEdge (uint wildcardRelationNo) :: _ordered.[i].Successors
                        _ordered.[int wildcardRelationNo].num_predecessors <- _ordered.[int wildcardRelationNo].num_predecessors + 1u
            _ordered
                    *)
        let updateRelation (_ordered : 'a array) (headRelationNo : uint) (ruleBodyAtom : RuleAtom) =
                _ordered.[int headRelationNo].intensional <- true
                match ruleBodyAtom with
                | NotTriple t ->
                    updateAtom _ordered NegativePatternEdge headRelationNo t
                | PositiveTriple t ->
                    updateAtom _ordered PositivePatternEdge headRelationNo t
        
        (* This is only run once on initialization to set up the data structure for topologial sorting of rules *)
        let mutable orderedTriplePatterns = 
            let _ordered = triplePatterns 
            rules |> Seq.iter (fun rule ->
                                        match rule.Head |> GetRuleHeadPattern with
                                            | None -> ()
                                            | Some headRelation ->
                                                let headRelationNo = triplePatternMap.[headRelation]
                                                rule.Body
                                                   |> Seq.iter (updateRelation _ordered headRelationNo)
                                )
            _ordered
        
        (* The queue contains all relations that are not dependent on any relations (that have not already been output) *)
        let mutable ready_elements_queue =
            Queue<uint>(orderedTriplePatterns
                    |> Array.filter (fun concept -> concept.num_predecessors = 0u)
                    |> Array.map (fun concept -> triplePatternMap.[concept.Relation]))
            
        (* The concepts that depended on a negation of a concept that is being output in the current stratification must wait till the next layer *)
        let mutable next_elements_queue = Queue<uint>()
        let mutable n_unordered = Array.length orderedTriplePatterns - ready_elements_queue.Count
        
        member internal this.printCycle cycle =
            cycle
            |> Seq.map (fun n -> $"Cycle element: {resources.GetGraphElement n}")
            |> String.concat ", "
            
        member internal this.find_cycle (visited : uint seq) (current : uint) (relation_id : uint) (is_negative : bool) : uint seq seq =
                let cycleFinder = this.cycle_finder (Seq.append visited [current]) relation_id
                if is_negative && (cycleFinder |> Seq.isEmpty |> not) then
                    let cycleString = cycleFinder |> Seq.head |> this.printCycle
                    logger.Error($"Datalog program contains a cycle with negation and is not stratifiable! {cycleString}")
                    failwith $"Datalog program contains a cycle with negation and is not stratifiable! {cycleString}"
                else
                    cycleFinder                
                
        
        (* Called when the topological sorting cannot proceed, hence assuming the existence of a cycle *)
        member internal this.cycle_finder (visited : uint seq) (current : uint) : uint seq seq =
            let current_element = orderedTriplePatterns.[int current]
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
                                                            this.find_cycle visited current relation_id false
                                                        | NegativePatternEdge relation_id ->
                                                            this.find_cycle visited current relation_id true
                                                    )
           
        member internal this.GetReadyElementsQueue() = ready_elements_queue    
        member internal this.GetOrderedTriplePatterns() = orderedTriplePatterns
            
        (* Between iterations, the switch about using negative edge must be reset,
            and all elements marked for next stratifications must be moved into the queue for the current stratification *)    
        member this.reset_stratification =
            for i in 0 .. (Array.length orderedTriplePatterns - 1) do
                orderedTriplePatterns.[i].uses_intensional_negative_edge <- false
                orderedTriplePatterns.[i].visited <- false
            while next_elements_queue.Count > 0 do
                ready_elements_queue.Enqueue(next_elements_queue.Dequeue())
            
            
        (* Updates a successor of a relation, and if the relation is ready to be output, it is added to the queue *)
        member this.update_successor (removed_relation_id : uint) (successor : PatternEdge) =
            let relation_id = 
                match successor with
                | PositivePatternEdge relation_id ->
                    relation_id
                | NegativePatternEdge relation_id ->
                    let removed_relation = orderedTriplePatterns.[int removed_relation_id]
                    if removed_relation.intensional then
                        orderedTriplePatterns.[int relation_id].uses_intensional_negative_edge <- true
                    relation_id
            let old_relation = orderedTriplePatterns.[int relation_id]
            if not old_relation.output then        
                if old_relation.num_predecessors < 1u then failwith "Datalog program preprocessing failed. This is a bug, please report that topological ordering failed, num_predecessors < 1"
                let new_predecessors = old_relation.num_predecessors - 1u
                orderedTriplePatterns.[int relation_id] <- { old_relation with num_predecessors = new_predecessors }
                if orderedTriplePatterns.[int relation_id].num_predecessors = 0u && not orderedTriplePatterns.[int relation_id].output then
                    orderedTriplePatterns.[int relation_id].output <- true
                    if orderedTriplePatterns.[int relation_id].uses_intensional_negative_edge then
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
                let relation = triplePatterns.[int relation_id]    
                orderedTriplePatterns.[int relation_id].Successors |> Seq.iter (this.update_successor relation_id)
                // if ordered_relations.[relation_id].intensional then
                let relation_rules = rules
                                        |> List.filter (fun rule ->
                                            (rule.Head |> GetRuleHeadPattern) = Some relation
                                            )
                ordered_rules <- Seq.append relation_rules ordered_rules
                orderedTriplePatterns.[int relation_id].intensional <- false
            Seq.distinct ordered_rules
        
        (* Checks whether a rule is completely covered by a cycle (given the already output relations, which are treated as extensional/edb
            In other words, whether all elements of the body are in the cycle, or are extensional*)
        member this.RuleIsCoveredByCycle cycle rule =
                rule.Body |> Seq.forall (fun atom ->
                                            let atomRelation = atom|> GetRuleAtomPattern 
                                            orderedTriplePatterns.[int triplePatternMap.[atomRelation]].intensional = false
                                            || (cycle |> Seq.exists (triplePatternsUnifiable atomRelation))
                                        )
        (* 
            Only called when topological sorting stops, so there is a cycle
            Finds one cycle, if that contains a negative edge, reports error, otherwise,
            checks whether the first relation in the cycle is ready to be output, and if so, outputs it
            An example of where it is not ready, is if it depends on another cycle, which needs to be output first. 
            
            The proof that these cycles / strongly connected components always exist is in the Alice book
         *)
        member this.handle_cycle()  =
            let cycles = (orderedTriplePatterns
                  |> Array.filter (fun relation -> relation.num_predecessors > 0u
                                                    && relation.output = false)
                  |> Array.map (fun relation -> triplePatternMap.[relation.Relation])
                  |> Seq.collect (this.cycle_finder [])
                  |> Seq.filter (fun cycle ->
                            let cycle_element = cycle |> Seq.head
                            let rules = rules
                                        |> List.filter (fun rule ->
                                            (rule.Head |> GetRuleHeadPattern) = Some triplePatterns.[int cycle_element]
                                            )
                            rules |> List.forall (this.RuleIsCoveredByCycle (cycle |> Seq.map (fun id -> triplePatterns.[int id])))
                            )
                            
                  )
            cycles |> Seq.iter (fun cycle ->
                  cycle |> Seq.distinct |>(Seq.iter (fun rel ->
                      if not orderedTriplePatterns.[int rel].output then 
                        orderedTriplePatterns.[int rel].output <- true
                        ready_elements_queue.Enqueue rel)
                  ))
                
            
        (*  Catches some errors in stratification, to avoid a wrong stratification being returned
            TODO: Remove when stratification is stable and tests cover all corners
        *)
        member this.is_stratified (stratification: Rule seq seq) =
            ready_elements_queue.Count = 0
            && next_elements_queue.Count = 0
            // && (stratification |> Seq.sumBy Seq.length) >= rules.Length
            // && ordered_relations |> Array.forall (fun relation -> relation.intensional = false)

        (* Used in the while loop in orderRules to test whether stratification is finished *)
        member this.topological_sort_finished() =
            orderedTriplePatterns |> Array.forall (fun relation -> relation.output || relation.num_predecessors = 0u)
                
        (* Order the rules topologically based on dependency. Used for stratification
            Each Rule seq in the outermost seq is a partition, and these partitions must be handled sequentially during materialization *)
        member this.orderRules  :  Rule seq seq =
            let mutable stratification = []
            if ready_elements_queue.Count = 0 then
                    this.handle_cycle()
            while ready_elements_queue.Count > 0 do
                stratification <- stratification @ [this.get_rule_partition()]
                this.reset_stratification
                if ready_elements_queue.Count = 0  && (not (this.topological_sort_finished ()))  then
                    this.handle_cycle()
                    
            if not (this.is_stratified stratification) then
                 failwith "Datalog program preprocessing created wrong stratification! This is a bug, please report"
            stratification
            