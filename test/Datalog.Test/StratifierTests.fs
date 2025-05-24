(*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.Datalog.Tests

open System.IO
open DagSemTools.Datalog.Stratifier
open DagSemTools.Rdf
open DagSemTools.Rdf.Ingress
open DagSemTools
open IriTools
open Serilog
open Serilog.Sinks.InMemory
open Xunit 
open DagSemTools.Datalog
open Faqt
open Xunit.Abstractions

module TermsUnifiableTests =
    
    type TermsUnifiableTests(output: ITestOutputHelper) =
        let _outputWriter = new TestUtils.TestOutputTextWriter(output)
        
        let inMemorySink = new InMemorySink()
        let logger =
            LoggerConfiguration()
                    .WriteTo.Sink(inMemorySink)
                    .CreateLogger()
        
        [<Fact>]
        let ``Variable and Resource - unifiable`` () =
            // Arrance
            let v = "x"
            let res = 1u 
            let term1 = Term.Variable v
            let term2 = Term.Resource res
            let constantMap = Map.empty
            let variableMap = Map.empty

            // Act
            let result = Unification.TermsUnifiable term1 term2 (constantMap, variableMap)

            // Assert
            match result with
            | Some (newConstantMap, _) ->
                Assert.Equal(res, newConstantMap[v])
            | None -> Assert.True(false, "Terms should be unifiable, but are not")

        [<Fact>]
        let ``Resource and Variable - unifiable`` () =
            // Arrange
            let v = "y"
            let res = 2u
            let term1 = Term.Resource res
            let term2 = Term.Variable v
            let constantMap = Map.empty
            let variableMap = Map.empty

            // Act
            let result = Unification.TermsUnifiable term1 term2 (constantMap, variableMap)

            // Assert
            match result with
            | Some (newConstantMap, _) ->
                Assert.Equal(res, newConstantMap[v])
            | None -> Assert.True(false, "Terms should be unifiable, but are not")

        [<Fact>]
        let ``Both Resources - same resource`` () =
            // Arrange
            let res = 3u
            let term1 = Term.Resource res
            let term2 = Term.Resource res
            let constantMap = Map.empty
            let variableMap = Map.empty

            // Act
            let result = Unification.TermsUnifiable term1 term2 (constantMap, variableMap)

            // Assert
            Assert.True(result.IsSome, "Terms should be unifiable, but are not")

        [<Fact>]
        let ``Both Resources - different resources`` () =
            // Arrange
            let res1 = 3u
            let res2 = 4u
            let term1 = Term.Resource res1
            let term2 = Term.Resource res2
            let constantMap = Map.empty
            let variableMap = Map.empty

            // Act
            let result = Unification.TermsUnifiable term1 term2 (constantMap, variableMap)

            // Assert
            Assert.True(result.IsNone, "Terms should not be unifiable, but are")

        [<Fact>]
        let ``Both Variables`` () =
            // Arrange
            let v1 = "x"
            let v2 = "y"
            let term1 = Term.Variable v1
            let term2 = Term.Variable v2
            let constantMap = Map.empty
            let variableMap = Map.empty

            // Act
            let result = Unification.TermsUnifiable term1 term2 (constantMap, variableMap)

            // Assert
            match result with
            | Some (_, newVariableMap) ->
                Assert.Equal(v2, newVariableMap[v1])
            | None -> Assert.True(false, "Terms should be unifiable, but are not")
            
            
    
        [<Fact>]
        let ``Triple patterns are fully unifiable`` () =
            // Arrange
            let subject = Term.Resource((1u))
            let predicate = Term.Resource((2u))
            let obj = Term.Resource((3u))

            let triple1 = { TriplePattern.Subject = subject; Predicate = predicate; Object = obj }
            let triple2 = { TriplePattern.Subject = subject; Predicate = predicate; Object = obj }

            // Act
            let result = Unification.triplePatternsUnifiable triple1 triple2

            // Assert
            Assert.True(result, "Triple patterns should be unifiable but are not")

        [<Fact>]
        let ``Triple patterns are not unifiable because of different subjects`` () =
            // Arrange
            let triple1 = { 
                TriplePattern.Subject = Term.Resource((1u))
                Predicate = Term.Resource((2u))
                Object = Term.Resource((3u)) }

            let triple2 = { 
                TriplePattern.Subject = Term.Resource((99u)) // Different subject
                Predicate = Term.Resource((2u))
                Object = Term.Resource((3u)) }

            // Act
            let result = Unification.triplePatternsUnifiable triple1 triple2

            // Assert
            Assert.False(result, "Triple patterns should not be unifiable due to different subjects")

        [<Fact>]
        let ``Triple patterns are not unifiable because of different predicates`` () =
            // Arrange
            let triple1 = { 
                TriplePattern.Subject = Term.Resource((1u))
                Predicate = Term.Resource((2u))
                Object = Term.Resource((3u)) }

            let triple2 = { 
                TriplePattern.Subject = Term.Resource((1u))
                Predicate = Term.Resource((99u)) // Different predicate
                Object = Term.Resource((3u)) }

            // Act
            let result = Unification.triplePatternsUnifiable triple1 triple2

            // Assert
            Assert.False(result, "Triple patterns should not be unifiable due to different predicates")

        [<Fact>]
        let ``Triple patterns are not unifiable because of different objects`` () =
            // Arrange
            let triple1 = { 
                TriplePattern.Subject = Term.Resource((1u))
                Predicate = Term.Resource((2u))
                Object = Term.Resource((3u)) }

            let triple2 = { 
                TriplePattern.Subject = Term.Resource((1u))
                Predicate = Term.Resource((2u))
                Object = Term.Resource((99u)) } // Different object

            // Act
            let result = Unification.triplePatternsUnifiable triple1 triple2

            // Assert
            Assert.False(result, "Triple patterns should not be unifiable due to different objects")

        [<Fact>]
        let ``Triple patterns with variable and resource are unifiable`` () =
            // Arrange
            let triple1 = { 
                TriplePattern.Subject = Term.Resource((1u))
                Predicate = Term.Variable("p") // Variable predicate
                Object = Term.Variable("o") } // Variable object

            let triple2 = { 
                TriplePattern.Subject = Term.Resource((1u))
                Predicate = Term.Resource((2u)) // Matching resource for predicate variable
                Object = Term.Resource((3u)) } // Matching resource for object variable

            // Act
            let result = Unification.triplePatternsUnifiable triple1 triple2

            // Assert
            Assert.True(result, "Triple patterns with variable and resource should be unifiable")


        [<Fact(Skip="Too long")>]
        let TestLargeFile() = 
            let fInfo = File.ReadAllText("TestData/large.datalog")
            let datastore = new Datastore(1000u);
            let rules = DagSemTools.Datalog.Parser.Parser.ParseString(fInfo, _outputWriter, datastore);
            rules.Should().NotBeNull() |> ignore
            let ruleList : Rule list = rules |> Seq.toList
            let stratifier = RulePartitioner (logger, ruleList, datastore.Resources)
            let stratification = stratifier.orderRules()
            stratification.Should().NotBeEmpty
    
        [<Fact(Skip="Too long time. https://github.com/daghovland/DagSemTools/issues/77")>]
        let TestImfDatalog() = 
            let fInfo = File.ReadAllText("TestData/imf.datalog")
            let datastore = new Datastore(1000u);
            let rules = DagSemTools.Datalog.Parser.Parser.ParseString(fInfo, _outputWriter, datastore);
            rules.Should().NotBeNull() |> ignore
            let ruleList : Rule list = rules |> Seq.toList
            let stratifier = RulePartitioner (logger, ruleList, datastore.Resources)
            let stratification = stratifier.orderRules()
            stratification.Should().NotBeEmpty
    