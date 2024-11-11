
namespace DagSemTools.OWL2RL2Datalog
open System.IO
open System.Reflection
open DagSemTools.Datalog.Parser

module Reasoner =
    let readEmbeddedDatalog (resourceName: string) errorOutput tripleTable =
        let assembly = Assembly.GetExecutingAssembly()
        use stream = assembly.GetManifestResourceStream(resourceName)
        if stream = null then
            failwith ("The embedded datalog file " + resourceName + " was not found. This is a bug, please report")
        use reader = new StreamReader(stream)
        Parser.ParseReader (reader, errorOutput, tripleTable)
    
    let listEmbeddedResources () =
        let assembly = Assembly.GetExecutingAssembly()
        assembly.GetManifestResourceNames()
    
    (* Adds the axioms that handle owl:sameAs as an equality relation  *)
    let enableEqualityReasoning tripleTable program errorOutput =
        let rules = readEmbeddedDatalog "OWL2RL2Datalog.datalog.equality.datalog" errorOutput tripleTable
        Seq.concat [program; rules]
        
    (* Adds a few axioms for a small fragment of OWL 2 RL
        Notably, equality is not added, as that can be added separately by calling enableEqualityReasoning
        *)
    let enableOwlReasoning tripleTable program (errorOutput : TextWriter) =
        let classRules = readEmbeddedDatalog "OWL2RL2Datalog.datalog.class.datalog" errorOutput tripleTable
        let propertyRules = readEmbeddedDatalog "OWL2RL2Datalog.datalog.property.datalog" errorOutput tripleTable
        Seq.concat [program
                    classRules
                    propertyRules
                    ]  