
namespace DagSemTools.OWL2RL2Datalog
open DagSemTools.Datalog.Parser

module Reasoner =
    (* Adds the axioms that handle owl:sameAs as an equality relation  *)
    let enableEqualityReasoning tripleTable program errorOutput =
        let rules = Parser.ParseFile ("datalog/equality.datalog", errorOutput, tripleTable)
        Seq.concat [program; rules]
        
    (* Adds a few axioms for a small fragment of OWL 2 RL
        Notably, equality is not added, as that can be added separately by calling enableEqualityReasoning
        *)
    let enableOwlReasoning tripleTable program errorOutput =
        let classRules = Parser.ParseFile ("datalog/class.datalog", errorOutput, tripleTable)
        let propertyRules = Parser.ParseFile ("datalog/property.datalog", errorOutput, tripleTable)
        Seq.concat [program
                    classRules
                    propertyRules
                    ]  