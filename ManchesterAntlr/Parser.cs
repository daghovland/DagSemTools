/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using AlcTableau;
using AlcTableau.ManchesterAntlr;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IriTools;

namespace ManchesterAntlr;

public static class Parser
{

    public static ALC.OntologyDocument ParseFile(string filename)
    {
        using TextReader textReader = File.OpenText(filename);
        return ParseReader(textReader);
    }

    public static ALC.OntologyDocument ParseReader(TextReader textReader, Dictionary<string, IriReference> prefixes)
    {

        var input = new AntlrInputStream(textReader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        parser.ErrorHandler = new BailErrorStrategy();
        IParseTree tree = parser.ontologyDocument();
        var visitor = new ManchesterVisitor();
        return visitor.Visit(tree);
    }

    public static ALC.OntologyDocument ParseReader(TextReader textReader) =>
        ParseReader(textReader, new Dictionary<string, IriReference>());

    public static ALC.OntologyDocument ParseString(string owl)
    {
        using TextReader textReader = new StringReader(owl);
        return ParseReader(textReader);
    }


}