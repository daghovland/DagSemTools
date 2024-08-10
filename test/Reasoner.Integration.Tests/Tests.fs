module Tests

open System
open AlcTableau
open Tableau
open Xunit
open ALC
open IriTools
open System.IO
    
    
[<Fact>]
let  TestOntologyWithSubClassAndExistential() =
    // Arrange
    let doc = ManchesterAntlr.Parser.TestFile("TestData/subclasses.owl")
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let reasoner_result = AlcTableau.Tableau.reasoner kb
        Assert.True(reasoner_result)
    
    