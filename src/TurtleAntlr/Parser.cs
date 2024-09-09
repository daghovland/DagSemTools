/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using AlcTableau;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IriTools;
using Rdf;

namespace AlcTableau.TurtleAntlr;

public static class Parser
{


    public static TripleTable ParseFile(string filename)
    {
        using TextReader textReader = File.OpenText(filename);
        return ParseReader(textReader, (uint)(new FileInfo(filename).Length));
    }

    public static TripleTable ParseFile(FileInfo fInfo)
    {
        using TextReader textReader = File.OpenText(fInfo.FullName);
        return ParseReader(textReader, (uint)(fInfo.Length));
    }
    
    public static TripleTable ParseReader(TextReader textReader, UInt32 init_size, Dictionary<string, IriReference> prefixes)
    {

        var input = new AntlrInputStream(textReader);
        var lexer = new TurtleLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new TurtleParser(tokens);
        parser.ErrorHandler = new BailErrorStrategy();
        IParseTree tree = parser.turtleDoc();
        ParseTreeWalker walker = new ParseTreeWalker();
        var listener = new TurtleListener(init_size);
        walker.Walk(listener, tree);
        return listener.TripleTable;
    }

    public static TripleTable ParseReader(TextReader textReader, UInt32 init_size) =>
        ParseReader(textReader, init_size, new Dictionary<string, IriReference>());

    public static TripleTable ParseString(string owl)
    {
        using TextReader textReader = new StringReader(owl);
        return ParseReader(textReader, (uint)owl.Length);
    }


}