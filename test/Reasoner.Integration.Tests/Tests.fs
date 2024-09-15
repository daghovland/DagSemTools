module Tests

open System
open AlcTableau
open Tableau
open TurtleParser.Unit.Tests
open Xunit
open ALC
open IriTools
open System.IO
open Xunit.Abstractions
    
type IntegrationTests(output : ITestOutputHelper) =
    let outputWriter = new TestOutputTextWriter(output)
    
    [<Fact>]
    member _.TestOntologyWithSubClassAndExistential() =
        // Arrange
        let doc = ManchesterAntlr.Parser.ParseFile("TestData/subclasses.owl", outputWriter)
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.True(reasoner_result)
        
    [<Fact>]
    member _.TestSimplestContradiction() =
        // Arrange
        let doc = ManchesterAntlr.Parser.ParseFile("TestData/simplest_contradiction.owl", outputWriter)
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.False(reasoner_result)
       
    [<Fact>]
    member  _.TestAlcBoolExample() =
        // Arrange
        let doc = ManchesterAntlr.Parser.ParseFile("TestData/alc_tableau_ex.owl", outputWriter)
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.True(reasoner_result)
        
        
    [<Fact>]
    member _.TestDisjunction() =
        // Arrange
        let doc = ManchesterAntlr.Parser.ParseFile("TestData/simple_disjunction.owl", outputWriter)
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.True(reasoner_result)
    [<Fact>]
    member _.TestSubclassContradiction() =
        // Arrange
        let doc = ManchesterAntlr.Parser.ParseFile("TestData/subclass_contradiction.owl", outputWriter)
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.False(reasoner_result)
        
    [<Fact>]
    member _.TestUniversalContradiction() =
        // Arrange
        let doc = ManchesterAntlr.Parser.ParseFile("TestData/simplest_universal_contradiction.owl", outputWriter)
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.False(reasoner_result)
        
    [<Fact>]
    member _.TestLongOrBranching() =
        // Arrange
        let doc = ManchesterAntlr.Parser.ParseFile("TestData/or-branching.owl", outputWriter)
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.False(reasoner_result)
        
        
    [<Fact(Skip= "Not implemented yet, See Issue https://github.com/daghovland/AlcTableau/issues/2")>]
    member _.TestCycleCOntradiction() =
        // Arrange
        let doc = ManchesterAntlr.Parser.ParseFile("TestData/cycle.owl", outputWriter)
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.False(reasoner_result)
        
            
    [<Fact>]
    member _.ExistUniv() =
        // Arrange
        let doc = ManchesterAntlr.Parser.ParseFile("TestData/exist_univ.owl", outputWriter)
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.False(reasoner_result)
     
             
    [<Fact>]
    member _.Dexpi() =
        // Arrange
        let doc = ManchesterAntlr.Parser.ParseFile("TestData/pandid.owl", outputWriter)
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.True(reasoner_result)
     