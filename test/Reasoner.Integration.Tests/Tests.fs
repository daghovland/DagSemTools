(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)

module Tests

open Serilog
open Serilog.Sinks.InMemory
open TestUtils
open Xunit
open Xunit.Abstractions
open DagSemTools.Manchester.Parser
open DagSemTools.AlcTableau
open DagSemTools.OWL2ALC
open ALC
    
type IntegrationTests(output : ITestOutputHelper) =
    let outputWriter = new TestOutputTextWriter(output)
    let inMemorySink = new InMemorySink()
    let logger =
        LoggerConfiguration()
            .WriteTo.Sink(inMemorySink)
            .CreateLogger()
    
    [<Fact>]
    member _.TestOntologyWithSubClassAndExistential() =
        // Arrange
        let owldoc = Parser.ParseFile("TestData/subclasses.owl", outputWriter)
        let doc = Translator.translate logger owldoc.Ontology
        // Act
        match doc with
        | ALC.Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.True(reasoner_result)
        
    [<Fact>]
    member _.TestSimplestContradiction() =
        // Arrange
        let doc = Parser.ParseFile("TestData/simplest_contradiction.owl", outputWriter)
        let doc = Translator.translate logger doc.Ontology
        // Act
        match doc with
        | ALC.Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.False(reasoner_result)
       
    [<Fact>]
    member  _.TestAlcBoolExample() =
        // Arrange
        let doc = Parser.ParseFile("TestData/alc_tableau_ex.owl", outputWriter)
        let doc = Translator.translate logger doc.Ontology
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.True(reasoner_result)
        
        
    [<Fact>]
    member _.TestDisjunction() =
        // Arrange
        let doc = Parser.ParseFile("TestData/simple_disjunction.owl", outputWriter)
        let doc = Translator.translate logger doc.Ontology
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.True(reasoner_result)
    [<Fact>]
    member _.TestSubclassContradiction() =
        // Arrange
        let doc = Parser.ParseFile("TestData/subclass_contradiction.owl", outputWriter)
        let doc = Translator.translate logger doc.Ontology
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.False(reasoner_result)
        
    [<Fact>]
    member _.TestUniversalContradiction() =
        // Arrange
        let doc = Parser.ParseFile("TestData/simplest_universal_contradiction.owl", outputWriter)
        let doc = Translator.translate logger doc.Ontology
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.False(reasoner_result)
        
    [<Fact>]
    member _.TestLongOrBranching() =
        // Arrange
        let doc = Parser.ParseFile("TestData/or-branching.owl", outputWriter)
        let doc = Translator.translate logger doc.Ontology
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.False(reasoner_result)
        
        
    [<Fact(Skip= "Not implemented yet, See Issue https://github.com/daghovland/AlcTableau/issues/2")>]
    member _.TestCycleCOntradiction() =
        // Arrange
        let doc = Parser.ParseFile("TestData/cycle.owl", outputWriter)
        let doc = Translator.translate logger doc.Ontology
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.False(reasoner_result)
        
            
    [<Fact>]
    member _.ExistUniv() =
        // Arrange
        let doc = Parser.ParseFile("TestData/exist_univ.owl", outputWriter)
        let doc = Translator.translate logger doc.Ontology
        // Act
        match doc with
        | Ontology (prefixes, version, kb) ->
            let state = ReasonerService.init kb
            let reasoner_result = ReasonerService.is_consistent state
            Assert.False(reasoner_result)
     