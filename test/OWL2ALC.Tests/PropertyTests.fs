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
open DagSemTools.AlcTableau.DataRange
open DagSemTools.Ingress
open DagSemTools.OwlOntology
open IriTools
open OwlOntology
open ALC
open Serilog
open Serilog.Sinks.InMemory
open Xunit
open Faqt

module PropertyTests =

    let inMemorySink = new InMemorySink()
    let logger =
        LoggerConfiguration()
                .WriteTo.Sink(inMemorySink)
                .CreateLogger()
        
    [<Fact>]
    let ``Object Property Range is translated`` () =
        let rangeIri = (IriReference "https://example.com/range")
        let rangeClass = (ClassName (FullIri rangeIri))
        let roleIri = (IriReference "https://example.com/role")
        let role = NamedObjectProperty (FullIri roleIri)
        let rangeAxiom = ObjectPropertyRange (role, rangeClass)
        let translatedAxiom = Translator.translateObjectPropertyAxiom logger rangeAxiom
        translatedAxiom.Should().Be(Inclusion(Universal (Role.Iri roleIri, Top), ConceptName rangeIri))
    
    [<Fact>]
    let ``Object Property Domain is translated`` () =
        let domainIri = (IriReference "https://example.com/domain")
        let domainClass = (ClassName (FullIri domainIri))
        let roleIri = (IriReference "https://example.com/role")
        let role = NamedObjectProperty (FullIri roleIri)
        let rangeAxiom = ObjectPropertyDomain (role, domainClass)
        let translatedAxiom = Translator.translateObjectPropertyAxiom logger rangeAxiom
        translatedAxiom.Should().Be(Inclusion(Universal (Role.Inverse roleIri, Top), ConceptName domainIri))
    
    [<Fact>]
    let ``Data Property Domain is translated`` () =
        let domainIri = (IriReference "https://example.com/domain")
        let domainClass = (ClassName (FullIri domainIri))
        let propertyIri = (IriReference "https://example.com/role")
        let property = FullIri propertyIri
        let axiom = DataPropertyDomain ([], property, domainClass)
        let translatedAxiom = Translator.translateDataPropertyAxiom logger axiom
        translatedAxiom.Should().Be(Inclusion(Universal (Role.Inverse propertyIri, Top), ConceptName domainIri))
    