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
    
    [<Fact>]
    let ``Simple class union is translated`` () =
        let subclassIri = (IriReference "https://example.com/subclass")
        let class1 = (ClassName (FullIri subclassIri))
        let superclassIri = (IriReference "https://example.com/superclass")
        let class2 = (ClassName (FullIri superclassIri))
        let union = ObjectUnionOf [class1; class2]
        let translatedClass = Translator.translateClass logger union
        translatedClass.Should().Be(Disjunction (ConceptName subclassIri, ConceptName superclassIri))
    
    [<Fact>]
    let ``Triple class union is translated`` () =
        let subclassIri = (IriReference "https://example.com/subclass")
        let class1 = (ClassName (FullIri subclassIri))
        let superclassIri = (IriReference "https://example.com/superclass")
        let class2 = (ClassName (FullIri superclassIri))
        let classIri3 = (IriReference "https://example.com/class3")
        let class3 = (ClassName (FullIri classIri3))
        let union = ObjectUnionOf [class1; class2; class3]
        let translatedClass = Translator.translateClass logger union
        translatedClass.Should().Be(Disjunction (ConceptName subclassIri, Disjunction (ConceptName superclassIri, ConceptName classIri3)))
    
    [<Fact>]
    let ``Simple class intersection is translated`` () =
        let subclassIri = (IriReference "https://example.com/subclass")
        let class1 = (ClassName (FullIri subclassIri))
        let superclassIri = (IriReference "https://example.com/superclass")
        let class2 = (ClassName (FullIri superclassIri))
        let union = ObjectIntersectionOf [class1; class2]
        let translatedClass = Translator.translateClass logger union
        translatedClass.Should().Be(Conjunction (ConceptName subclassIri, ConceptName superclassIri))
    
    [<Fact>]
    let ``Triple class intersection is translated`` () =
        let subclassIri = (IriReference "https://example.com/subclass")
        let class1 = (ClassName (FullIri subclassIri))
        let superclassIri = (IriReference "https://example.com/superclass")
        let class2 = (ClassName (FullIri superclassIri))
        let classIri3 = (IriReference "https://example.com/class3")
        let class3 = (ClassName (FullIri classIri3))
        let union = ObjectIntersectionOf [class1; class2; class3]
        let translatedClass = Translator.translateClass logger union
        translatedClass.Should().Be(Conjunction (ConceptName subclassIri, Conjunction (ConceptName superclassIri, ConceptName classIri3)))
    
    [<Fact>]
    let ``Complement of class name is translated`` () =
        let classIri = (IriReference "https://example.com/class")
        let class1 = (ClassName (FullIri classIri))
        let complement = ObjectComplementOf class1
        let translatedClass = Translator.translateClass logger complement
        translatedClass.Should().Be(Negation (ConceptName classIri))
    
    [<Fact>]
    let ``Simple existential is translated`` () =
        let subclassIri = (IriReference "https://example.com/subclass")
        let class1 = (ClassName (FullIri subclassIri))
        let roleIri = (IriReference "https://example.com/role")
        let role = NamedObjectProperty (FullIri roleIri)
        let universal = ObjectSomeValuesFrom (role, class1)
        let translatedClass = Translator.translateClass logger universal
        translatedClass.Should().Be(Existential (Role.Iri roleIri  , ConceptName subclassIri))
    
    [<Fact>]
    let ``Simple universal is translated`` () =
        let subclassIri = (IriReference "https://example.com/subclass")
        let class1 = (ClassName (FullIri subclassIri))
        let roleIri = (IriReference "https://example.com/role")
        let role = NamedObjectProperty (FullIri roleIri)
        let universal = ObjectAllValuesFrom (role, class1)
        let translatedClass = Translator.translateClass logger universal
        translatedClass.Should().Be(Universal (Role.Iri roleIri  , ConceptName subclassIri))
    
    [<Fact>]
    let ``Simple object property fact is translated`` () =
        let leftIri = (IriReference "https://example.com/left")
        let rightIri = (IriReference "https://example.com/right")
        let roleIri = (IriReference "https://example.com/role")
        let role = NamedObjectProperty (FullIri roleIri)
        let assertion = ObjectPropertyAssertion ([], role, NamedIndividual (FullIri leftIri),
                                                 NamedIndividual (FullIri rightIri))
        let translatedClass = Translator.translateAssertion logger assertion
        translatedClass.Should().Be(ABoxAssertion.RoleAssertion (leftIri, rightIri, Role.Iri roleIri))
    
    [<Fact>]
    let ``Inverse object property fact is translated`` () =
        let leftIri = (IriReference "https://example.com/left")
        let rightIri = (IriReference "https://example.com/right")
        let roleIri = (IriReference "https://example.com/role")
        let role = InverseObjectProperty (NamedObjectProperty (FullIri roleIri))
        let assertion = ObjectPropertyAssertion ([], role, NamedIndividual (FullIri leftIri),
                                                 NamedIndividual (FullIri rightIri))
        let translatedClass = Translator.translateAssertion logger assertion
        translatedClass.Should().Be(ABoxAssertion.RoleAssertion (leftIri, rightIri, Role.Inverse roleIri))
    
    [<Fact>]
    let ``Negative object property fact is translated`` () =
        let leftIri = (IriReference "https://example.com/left")
        let rightIri = (IriReference "https://example.com/right")
        let roleIri = (IriReference "https://example.com/role")
        let role = NamedObjectProperty (FullIri roleIri)
        let assertion =  NegativeObjectPropertyAssertion ([], role, NamedIndividual (FullIri leftIri),
                                                 NamedIndividual (FullIri rightIri))
        let translatedClass = Translator.translateAssertion logger assertion
        translatedClass.Should().Be(ABoxAssertion.NegativeRoleAssertion (leftIri, rightIri, Role.Iri roleIri))
    
    
    [<Fact>]
    let ``Simple data property fact is translated`` () =
        let leftIri = (IriReference "https://example.com/left")
        let data = GraphElement.GraphLiteral (RdfLiteral.LiteralString "data")
        let roleIri = (IriReference "https://example.com/role")
        let role = FullIri roleIri
        let assertion = DataPropertyAssertion ([], role,
                                               NamedIndividual (FullIri leftIri),
                                                data)
        let translatedClass = Translator.translateAssertion logger assertion
        translatedClass.Should().Be(ABoxAssertion.LiteralAssertion (leftIri, roleIri, "(data)"))
    
    [<Fact>]
    let ``Negative data property fact is translated`` () =
        let leftIri = (IriReference "https://example.com/left")
        let data = GraphElement.GraphLiteral (RdfLiteral.LiteralString "data")
        let roleIri = (IriReference "https://example.com/role")
        let role = FullIri roleIri
        let assertion = NegativeDataPropertyAssertion ([], role,
                                               NamedIndividual (FullIri leftIri),
                                                data)
        let translatedClass = Translator.translateAssertion logger assertion
        translatedClass.Should().Be(ABoxAssertion.NegativeAssertion (ABoxAssertion.LiteralAssertion (leftIri, roleIri, "(data)")))
    
    