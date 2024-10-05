/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IriTools;
using DagSemTools.Rdf;
using DagSemTools.Parser;

namespace DagSemTools.TurtleAntlr;

/// <summary>
/// Parser for the Turtle language.
/// </summary>
public static class Parser
{
    /// <summary>
    /// Parses the content of the file containing RDF-1.2 Turtle. 
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="errorOutput"></param>
    /// <returns></returns>
    public static Datastore ParseFile(string filename, TextWriter errorOutput)
    {
        using TextReader textReader = File.OpenText(filename);
        return ParseReader(textReader, (uint)(new FileInfo(filename).Length), errorOutput);
    }

    /// <summary>
    /// Parses the content of the file containing RDF-1.2 Turtle.
    /// </summary>
    /// <param name="fInfo"></param>
    /// <param name="errorOutput"></param>
    /// <returns></returns>
    public static Datastore ParseFile(FileInfo fInfo, TextWriter errorOutput)
    {
        using TextReader textReader = File.OpenText(fInfo.FullName);
        return ParseReader(textReader, (uint)(fInfo.Length), errorOutput);
    }

    /// <summary>
    /// Parses the content of the TextReader containing RDF-1.2 Turtle.
    /// </summary>
    /// <param name="textReader"></param>
    /// <param name="initSize"></param>
    /// <param name="prefixes"></param>
    /// <param name="errorOutput"></param>
    /// <returns></returns>
    public static Datastore ParseReader(TextReader textReader, UInt32 initSize, Dictionary<string, IriReference> prefixes, TextWriter errorOutput)
    {
        var input = new AntlrInputStream(textReader);
        var lexer = new TurtleLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new TurtleParser(tokens);
        var customErrorListener = new ParserErrorListener(errorOutput);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(customErrorListener);
        IParseTree tree = parser.turtleDoc();
        ParseTreeWalker walker = new ParseTreeWalker();
        var listener = new TurtleListener(initSize, customErrorListener);
        walker.Walk(listener, tree);
        if (customErrorListener.HasError)
        {
            throw new Exception("Syntax errors in Turtle file");
        }
        return listener.datastore;
    }

    /// <summary>
    /// Parses the content of the TextReader containing RDF-1.2 Turtle.
    /// </summary>
    /// <param name="textReader"></param>
    /// <param name="initSize"></param>
    /// <param name="errorOutput"></param>
    /// <returns></returns>
    public static Datastore ParseReader(TextReader textReader, UInt32 initSize, TextWriter errorOutput) =>
        ParseReader(textReader, initSize, new Dictionary<string, IriReference>(), errorOutput);

    /// <summary>
    /// Parses the content of the string containing RDF-1.2 Turtle.
    /// </summary>
    /// <param name="owl"></param>
    /// <param name="errorOutput"></param>
    /// <returns></returns>
    public static Datastore ParseString(string owl, TextWriter errorOutput)
    {
        using TextReader textReader = new StringReader(owl);
        return ParseReader(textReader, (uint)owl.Length, errorOutput);
    }


}