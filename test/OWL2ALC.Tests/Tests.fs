(*
    Copyright (C) 2025 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*)
namespace DagSemTools.OWL2ALC
open DagSemTools.AlcTableau
open DagSemTools.AlcTableau.ALC
open DagSemTools.OwlOntology
open IriTools
open OwlOntology
open ALC
open Serilog
open Serilog.Sinks.InMemory
open Xunit
open Faqt

module Tests =

    let inMemorySink = new InMemorySink()
    let logger =
        LoggerConfiguration()
                .WriteTo.Sink(inMemorySink)
                .CreateLogger()
        

    [<Fact>]
    let ``Empty Owl ontology is translated`` () =
        let emptyOntology = new DagSemTools.OwlOntology.Ontology([],
                                                                 DagSemTools.OwlOntology.ontologyVersion.UnNamedOntology,
                                                                 [],
                                                                 [])
        let translatedOntology = DagSemTools.OWL2ALC.Translator.translate logger emptyOntology
        let expectedOntology = OntologyDocument.Ontology ([], ontologyVersion.UnNamedOntology, ([],[]))
        translatedOntology.Should().Be(expectedOntology)
      

    
    [<Fact>]
    let ``Single className is translated`` () =
        let subclassIri = (IriReference "https://example.com/subclass")
        let subClass = (ClassName (FullIri subclassIri))
        let translatedClass = Translator.translateClass logger subClass
        translatedClass.Should().Be(ConceptName subclassIri)
    
    [<Fact>]
    let ``Single axiom Owl ontology is translated`` () =
        let subclassIri = (IriReference "https://example.com/subclass")
        let subClass = (ClassName (FullIri subclassIri))
        let superclassIri = (IriReference "https://example.com/superclass")
        let superClass = (ClassName (FullIri superclassIri))
        let axiom = AxiomClassAxiom (SubClassOf ([],subClass, superClass)) 
        let emptyOntology = new DagSemTools.OwlOntology.Ontology([],
                                                                 DagSemTools.OwlOntology.ontologyVersion.UnNamedOntology,
                                                                 [],
                                                                 [axiom])
        let translatedOntology = Translator.translate logger emptyOntology
        let expectedAxiom = Inclusion (ConceptName subclassIri, ConceptName superclassIri) 
        let expectedOntology = OntologyDocument.Ontology ([], ontologyVersion.UnNamedOntology, ([expectedAxiom],[]))
        translatedOntology.Should().Be(expectedOntology)