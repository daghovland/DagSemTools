
namespace DagSemTools.OWL2RL2Datalog
open DagSemTools.Datalog.Parser

module Reasoner =
    let enableEqualityReasoning tripleTable program errorOutput =
        let rules = Parser.ParseFile ("datalog/equality.datalog", errorOutput, tripleTable)
        Seq.concat [program; rules]
        
    let enableOwlReasoning tripleTable program errorOutput =
        let eqRules = Parser.ParseFile ("datalog/equality.datalog", errorOutput, tripleTable)
        let classRules = Parser.ParseFile ("datalog/class.datalog", errorOutput, tripleTable)
        let propertyRules = Parser.ParseFile ("datalog/property.datalog", errorOutput, tripleTable)
        Seq.concat [program; eqRules; classRules; propertyRules]  