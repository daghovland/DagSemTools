(*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.RdfOwlTranslator.Tests

open System.IO
open System.Numerics
open DagSemTools.OwlOntology
open Xunit.Abstractions
open Xunit
open IriTools
open Faqt
open Serilog
open Serilog.Sinks.InMemory
open TestUtils

type TestTurtle(output : ITestOutputHelper) =
    let mutable outputHelper = output
    let outputWriter = new TestOutputTextWriter(output);
    let inMemorySink = new InMemorySink()
    let logger =
        LoggerConfiguration()
                .WriteTo.Sink(inMemorySink)
                .CreateLogger()
        

    
    [<Fact>]
    let ``Subclass of three restrictions can be parsed from triples`` () =
        let ontology = File.ReadAllText("TestData/triple-subset-qualified-restriction.ttl")
        let rdf = DagSemTools.Turtle.Parser.Parser.ParseString(ontology, outputWriter)
        
        //Act
        let translator = new DagSemTools.RdfOwlTranslator.Rdf2Owl(rdf.Triples, rdf.Resources, logger)
        let ontology = translator.extractOntology

        //Assert
        let expectedAxioms = [
            AxiomClassAxiom(
                SubClassOf(
                    [],
                    ClassName(Iri.FullIri(new IriReference $"http://ns.imfid.org/imf#Aspect")),
                    ObjectExactQualifiedCardinality (BigInteger(1),
                                                   (NamedObjectProperty (Iri.FullIri "http://ns.imfid.org/imf#hasCharacteristic")),
                                                   ClassName(Iri.FullIri(new IriReference "http://ns.imfid.org/imf#InformationDomain")))
                )
            )
            AxiomClassAxiom(
                SubClassOf(
                    [],
                    ClassName(Iri.FullIri(new IriReference $"http://ns.imfid.org/imf#Aspect")),
                    ObjectExactQualifiedCardinality (BigInteger(1),
                                                   (NamedObjectProperty (Iri.FullIri "http://ns.imfid.org/imf#hasCharacteristic")),
                                                   ClassName(Iri.FullIri(new IriReference "http://ns.imfid.org/imf#Modality")))
                )
            )
            AxiomClassAxiom(
                SubClassOf(
                    [],
                    ClassName(Iri.FullIri(new IriReference $"http://ns.imfid.org/imf#Aspect")),
                    ObjectMaxQualifiedCardinality (BigInteger(1),
                                                   (NamedObjectProperty (Iri.FullIri "http://ns.imfid.org/imf#hasCharacteristic")),
                                                   ClassName(Iri.FullIri(new IriReference "http://ns.imfid.org/imf#Interest")))
                )
            )]

        let ontologyAxioms =
            ontology.Ontology.Axioms
            |> Seq.filter (fun ax ->
                match ax with
                | AxiomClassAxiom x -> true
                | _ -> false)

        ontologyAxioms.Should().BeSupersetOf(expectedAxioms)