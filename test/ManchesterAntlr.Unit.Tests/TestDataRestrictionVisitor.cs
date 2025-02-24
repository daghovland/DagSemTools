/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using DagSemTools.Ingress;
using DagSemTools.OwlOntology;
using FluentAssertions;
using IriTools;
using DataRange = DagSemTools.AlcTableau.DataRange;

namespace DagSemTools.Manchester.Parser.Unit.Tests;

public class TestDataRestrictionVisitor
{

    public Tuple<Iri, GraphElement> testReader(TextReader text_reader, Dictionary<string, IriReference> prefixes)
    {

        var input = new AntlrInputStream(text_reader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        IParseTree tree = parser.datatype_restriction();
        var visitor = new DatatypeRestrictionVisitor();
        return visitor.Visit(tree);


    }

    public Tuple<DataRange.facet, string> testReader(TextReader text_reader) =>
        testReader(text_reader, new Dictionary<string, IriReference>());

    public Tuple<DataRange.facet, string> testString(string owl)
    {
        using TextReader text_reader = new StringReader(owl);
        return testReader(text_reader);
    }


    [Fact]
    public void TestLessThan0()
    {
        var parsedDataRange = testString("< 0");
        var expexted = new Tuple<DataRange.facet, string>(DataRange.facet.LessThan, "0");
        parsedDataRange.Should().BeEquivalentTo(expexted);
    }


    [Fact]
    public void TestGreaterThan9()
    {
        var parsedDataRange = testString("> 9");
        var expexted = new Tuple<DataRange.facet, string>(DataRange.facet.GreaterThan, "9");
        parsedDataRange.Should().BeEquivalentTo(expexted);
    }


    [Fact]
    public void TestLength()
    {
        var parsedDataRange = testString("length 2");
        var expexted = new Tuple<DataRange.facet, string>(DataRange.facet.Length, "2");
        parsedDataRange.Should().BeEquivalentTo(expexted);
    }
}