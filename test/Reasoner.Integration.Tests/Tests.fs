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
    let doc = ManchesterAntlr.Parser.ParseFile("TestData/subclasses.owl")
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let reasoner_result = AlcTableau.Tableau.is_consistent kb
        Assert.True(reasoner_result)
    
[<Fact>]
let  TestSimplestContradiction() =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("TestData/simplest_contradiction.owl")
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let reasoner_result = AlcTableau.Tableau.is_consistent kb
        Assert.False(reasoner_result)
   
[<Fact>]
let  TestAlcBoolExample() =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("TestData/alc_tableau_ex.owl")
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let reasoner_result = AlcTableau.Tableau.is_consistent kb
        Assert.True(reasoner_result)
    
[<Fact>]
let  TestSubclassContradiction() =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("TestData/subclass_contradiction.owl")
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let reasoner_result = AlcTableau.Tableau.is_consistent kb
        Assert.False(reasoner_result)
    
[<Fact>]
let  TestUniversalContradiction() =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("TestData/simplest_universal_contradiction.owl")
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let reasoner_result = AlcTableau.Tableau.is_consistent kb
        Assert.False(reasoner_result)
    
[<Fact>]
let  TestLongOrBranching() =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("TestData/or-branching.owl")
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let reasoner_result = AlcTableau.Tableau.is_consistent kb
        Assert.False(reasoner_result)
    
    
// This test does not halt. Needs a cycle test, or different rules
[<Fact>]
let  TestCycleCOntradiction() =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("TestData/cycle.owl")
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let reasoner_result = AlcTableau.Tableau.is_consistent kb
        Assert.False(reasoner_result)
    
        
[<Fact>]
let  ExistUniv() =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("TestData/exist_univ.owl")
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let reasoner_result = AlcTableau.Tableau.is_consistent kb
        Assert.False(reasoner_result)
 
         
[<Fact>]
let  Dexpi() =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("TestData/pandid.owl")
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let reasoner_result = AlcTableau.Tableau.is_consistent kb
        Assert.True(reasoner_result)
 