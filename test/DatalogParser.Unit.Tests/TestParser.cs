/*
 Copyright (C) 2024 Dag Hovland
 This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
 Contact: hovlanddag@gmail.com
*/
using DagSemTools.Ingress;
using DagSemTools.Datalog;
using DagSemTools.Rdf;
using FluentAssertions;
using IriTools;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.InMemory;
using TestUtils;
using Xunit.Abstractions;

namespace DatalogParser.Unit.Tests;

public class TestParser
{

    private ITestOutputHelper _output;
    private TextWriter _outputWriter;
    private InMemorySink _inMemorySink;

    private Logger _logger;


    public TestParser(ITestOutputHelper output)
    {
        _output = output;
        _outputWriter = new TestOutputTextWriter(_output);
        _inMemorySink = new InMemorySink();

        _logger =
        new LoggerConfiguration()
            .WriteTo.Sink(_inMemorySink)
            .CreateLogger();


    }

    public IEnumerable<Rule> TestProgram(string datalog)
    {
        var datastore = new Datastore(1000);
        return DagSemTools.Datalog.Parser.Parser.ParseString(datalog, _outputWriter, datastore);

    }



    public IEnumerable<Rule> TestProgramFile(FileInfo datalog)
    {
        var datastore = new Datastore(1000);
        return DagSemTools.Datalog.Parser.Parser.ParseFile(datalog, _outputWriter, datastore);

    }


    [Fact]
    public void TestSingleRule()
    {
        var fInfo = File.ReadAllText("TestData/rule1.datalog");
        var ont = TestProgram(fInfo).ToList();
        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
    }


    [Fact]
    public void TestRuleWithAnd()
    {
        var fInfo = File.ReadAllText("TestData/ruleand.datalog");
        var ont = TestProgram(fInfo).ToList();
        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
        ont.First().Body.Count().Should().Be(2);
    }

    [Fact]
    public void TestTwoRules()
    {
        var fInfo = File.ReadAllText("TestData/tworules.datalog");
        var ont = TestProgram(fInfo).ToList();
        ont.Should().NotBeNull();
        ont.Should().HaveCount(2);
        ont.First().Body.Count().Should().Be(2);
    }


    [Fact]
    public void TestNot()
    {
        var fInfo = File.ReadAllText("TestData/rulenot.datalog");
        var ont = TestProgram(fInfo).ToList();
        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
        ont.First().Body.Count().Should().Be(2);
    }


    [Fact]
    public void TestTypeAtom()
    {
        var fInfo = File.ReadAllText("TestData/ruletypeatom.datalog");
        var ont = TestProgram(fInfo).ToList();
        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
        ont.First().Body.Count().Should().Be(2);
    }

    [Fact]
    public void TestPrefixes()
    {
        //Arrange
        var datastore = new Datastore(1000);
        var fInfo = File.ReadAllText("TestData/prefixes.datalog");

        //Act
        var ont = DagSemTools.Datalog.Parser.Parser.ParseString(fInfo, _outputWriter, datastore).ToList();

        //Assert
        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
        ont.First().Body.Count().Should().Be(1);
        var ruleAtom = ont.First().Body.First();
        ruleAtom.Should().NotBeNull();
        ruleAtom.IsPositiveTriple.Should().BeTrue();
        var ruleTriplePattern = ((RuleAtom.PositiveTriple)ruleAtom).Item;
        ruleTriplePattern.Subject.Should().Be(Term.NewVariable("?s"));

        var predicateResource = Term
            .NewResource(datastore.AddNodeResource(RdfResource
                .NewIri(new IriReference("https://example.com/data#predicate2"))));
        ruleTriplePattern.Predicate.Should().Be(predicateResource);

        var objectResource = Term
            .NewResource(datastore.AddNodeResource(RdfResource
                .NewIri(new IriReference("https://example.com/data3#obj"))));
        ruleTriplePattern.Object.Should().Be(objectResource);

        ruleAtom.Should().Be(RuleAtom.NewPositiveTriple(new TriplePattern(
            Term.NewVariable("?s"),
            predicateResource,
            objectResource)));
    }

    [Fact]
    public void TestRuleWithAllVariables()
    {
        var datastore = new Datastore(1000);
        var fInfo = File.ReadAllText("TestData/properties.datalog");
        var ont = DagSemTools.Datalog.Parser.Parser.ParseString(fInfo, _outputWriter, datastore).ToList();

        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
        var parsedDatalogRule = ont.First();
        parsedDatalogRule.Body.Count().Should().Be(2);
        parsedDatalogRule.Body.First().Should().Be(RuleAtom.NewPositiveTriple(new TriplePattern(
            Term.NewVariable("?x"),
            Term.NewVariable("?p"),
            Term.NewVariable("?y"))));
        var rdfTypeResource = Term
            .NewResource(datastore.AddNodeResource(RdfResource
                .NewIri(new IriReference(Namespaces.RdfType))));
        var expectedHead = RuleHead.NewNormalHead(new TriplePattern(
            Term.NewVariable("?x"),
            rdfTypeResource,
            Term.NewVariable("?c")));
        expectedHead.Should().NotBeNull();
        parsedDatalogRule.Head.Should().NotBeNull();
        parsedDatalogRule.Head.Should().Be(expectedHead);

    }

    [Fact]
    public void TestTypeAtom2()
    {
        var fInfo = File.ReadAllText("TestData/typeatom2.datalog");
        var datastore = new Datastore(1000);
        var ont = DagSemTools.Datalog.Parser.Parser.ParseString(fInfo, _outputWriter, datastore).ToList();

        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);
        ont.First().Body.Count().Should().Be(1);
        ont.First().Head.Should().Be(RuleHead.NewNormalHead(new TriplePattern(
            Term.NewVariable("?new_node"),
            Term
                .NewResource(datastore.GetGraphNodeId(RdfResource.NewIri(new IriReference("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")))),
            Term
                .NewResource(datastore.GetGraphNodeId(RdfResource
                    .NewIri(new IriReference("https://example.com/data#type")))))));

        ont.First().Body.First().Should().Be(RuleAtom.NewPositiveTriple(new TriplePattern(
            Term.NewVariable("?node"),
            Term
                .NewResource(datastore.GetGraphNodeId(RdfResource
                    .NewIri(new IriReference("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")))),
            Term
                .NewResource(datastore.GetGraphNodeId(RdfResource
                    .NewIri(new IriReference("https://example.com/data#type")))))));
    }

    
    [Fact]
    public void TestContradictionRule()
    {
        var fInfo = File.ReadAllText("TestData/contradiction.datalog");
        var ont = TestProgram(fInfo).ToList();
        ont.Should().NotBeNull();
        ont.Should().HaveCount(1);

    }


    [Fact]
    public void TestLargeFile()
    {
        var fInfo = File.ReadAllText("TestData/loop.datalog");
        var ont = TestProgram(fInfo).ToList();
        ont.Should().NotBeNull();
        ont.Should().HaveCountGreaterThan(100);    
    }

}