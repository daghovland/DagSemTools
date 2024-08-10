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
    
[<Fact>]
let  TestSimplestContradiction() =
    // Arrange
    let doc = ManchesterAntlr.Parser.TestFile("TestData/simplest_contradiction.owl")
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let reasoner_result = AlcTableau.Tableau.reasoner kb
        Assert.False(reasoner_result)
   
[<Fact>]
let  TestAlcBoolExample() =
    // Arrange
    let doc = ManchesterAntlr.Parser.TestFile("TestData/alc_tableau_ex.owl")
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let reasoner_result = AlcTableau.Tableau.reasoner kb
        Assert.True(reasoner_result)
    