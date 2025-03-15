/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools;
using DagSemTools.ManchesterAntlr;
using DagSemTools.Parser;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IriTools;
using DagSemTools.OwlOntology;

namespace DagSemTools.Manchester.Parser;

internal static class Parser
{
    public static OntologyDocument ParseFile(string filename, TextWriter errorOutput)
    {
        using TextReader textReader = File.OpenText(filename);
        return ParseReader(textReader, errorOutput);
    }

    public static OntologyDocument ParseReader(TextReader textReader, TextWriter errorOutput)
    {
        var input = new AntlrInputStream(textReader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        var customErrorListener = new ParserErrorListener(errorOutput);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(customErrorListener);
        IParseTree tree = parser.ontologyDocument();
        var visitor = new ManchesterVisitor(customErrorListener);
        return visitor.Visit(tree);
    }

    public static OntologyDocument ParseString(string owl, TextWriter errorOutput)
    {
        using TextReader textReader = new StringReader(owl);
        return ParseReader(textReader, errorOutput);
    }


}