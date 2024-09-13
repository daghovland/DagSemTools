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
    
    
 
[<Fact>]
let  TestOntologyWithSubClassAndExistential(output : ITestOutputHelper) =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("TestData/subclasses.owl", new TestOutputTextWriter(output))
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let state = ReasonerService.init kb
        let reasoner_result = ReasonerService.is_consistent state
        Assert.True(reasoner_result)
    
[<Fact>]
let  TestSimplestContradiction(output : ITestOutputHelper) =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("1", new TestOutputTextWriter(output))
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let state = ReasonerService.init kb
        let reasoner_result = ReasonerService.is_consistent state
        Assert.False(reasoner_result)
   
[<Fact>]
let  TestAlcBoolExample(output : ITestOutputHelper) =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("1", new TestOutputTextWriter(output))
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let state = ReasonerService.init kb
        let reasoner_result = ReasonerService.is_consistent state
        Assert.True(reasoner_result)
    
    
[<Fact>]
let  TestDisjunction(output : ITestOutputHelper) =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("1", new TestOutputTextWriter(output))
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let state = ReasonerService.init kb
        let reasoner_result = ReasonerService.is_consistent state
        Assert.True(reasoner_result)
[<Fact>]
let  TestSubclassContradiction(output : ITestOutputHelper) =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("1", new TestOutputTextWriter(output))
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let state = ReasonerService.init kb
        let reasoner_result = ReasonerService.is_consistent state
        Assert.False(reasoner_result)
    
[<Fact>]
let  TestUniversalContradiction(output : ITestOutputHelper) =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("1", new TestOutputTextWriter(output))
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let state = ReasonerService.init kb
        let reasoner_result = ReasonerService.is_consistent state
        Assert.False(reasoner_result)
    
[<Fact>]
let  TestLongOrBranching(output : ITestOutputHelper) =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("1", new TestOutputTextWriter(output))
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let state = ReasonerService.init kb
        let reasoner_result = ReasonerService.is_consistent state
        Assert.False(reasoner_result)
    
    
[<Fact(Skip= "Not implemented yet, See Issue https://github.com/daghovland/AlcTableau/issues/2")>]
let  TestCycleCOntradiction(output : ITestOutputHelper) =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("1", new TestOutputTextWriter(output))
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let state = ReasonerService.init kb
        let reasoner_result = ReasonerService.is_consistent state
        Assert.False(reasoner_result)
    
        
[<Fact>]
let  ExistUniv(output : ITestOutputHelper) =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("1", new TestOutputTextWriter(output))
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let state = ReasonerService.init kb
        let reasoner_result = ReasonerService.is_consistent state
        Assert.False(reasoner_result)
 
         
[<Fact>]
let  Dexpi(output : ITestOutputHelper) =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("1", new TestOutputTextWriter(output))
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let state = ReasonerService.init kb
        let reasoner_result = ReasonerService.is_consistent state
        Assert.True(reasoner_result)

         
[<Fact>]
let  Boundaries(output : ITestOutputHelper) =
    // Arrange
    let doc = ManchesterAntlr.Parser.ParseFile("1", new TestOutputTextWriter(output))
    // Act
    match doc with
    | Ontology (prefixes, version, kb) ->
        let state = ReasonerService.init kb
        let reasoner_result = ReasonerService.is_consistent state
        Assert.True(reasoner_result) 