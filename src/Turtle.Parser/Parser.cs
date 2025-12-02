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

namespace DagSemTools.Turtle.Parser;

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
        var input = CharStreams.fromPath(filename);
        return ParseAntlrBuffered(input, (uint)(new FileInfo(filename).Length), new Dictionary<string, IriReference>(), errorOutput);
    }

    /// <summary>
    /// Parses the content of the file containing RDF-1.2 Turtle.
    /// </summary>
    /// <param name="fInfo"></param>
    /// <param name="errorOutput"></param>
    /// <returns></returns>
    public static Datastore ParseFile(FileInfo fInfo, TextWriter errorOutput)
    {
        var input = CharStreams.fromPath(fInfo.FullName);
        return ParseAntlrBuffered(input, (uint)(fInfo.Length), new Dictionary<string, IriReference>(), errorOutput);
    }

    /// <summary>
    /// Parses the content of the TextReader containing RDF-1.2 Turtle.
    /// </summary>
    /// <param name="textReader"></param>
    /// <param name="initSize"></param>
    /// <param name="prefixes"></param>
    /// <param name="errorOutput"></param>
    /// <returns></returns>
    public static Datastore ParseReader(TextReader textReader,
        Dictionary<string, IriReference> prefixes, TextWriter errorOutput, UInt32 initSize = 1024)
    {
        var input = CharStreams.fromTextReader(textReader);
        return ParseAntlrBuffered(input, initSize, prefixes, errorOutput);
    }

    private static Datastore ParseAntlrBuffered(ICharStream input, UInt32 initSize, Dictionary<string, IriReference> prefixes, TextWriter errorOutput)
    {

        var lexer = new TriGDocLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        lexer.TokenFactory = new CommonTokenFactory(true);
        var parser = new TriGDocParser(tokens);
        var customErrorListener = new ParserErrorListener(errorOutput);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(customErrorListener);
        IParseTree tree = parser.trigDoc();
        ParseTreeWalker walker = new ParseTreeWalker();
        var listener = new TurtleListener(initSize, customErrorListener);
        walker.Walk(listener, tree);
        if (customErrorListener.HasError)
        {
            throw new Exception("Syntax errors in Turtle file");
        }
        return listener.datastore;
    }

    
    
    private static Datastore ParseAntlrUnbuffered(Stream inputStream, UInt32 initSize, Dictionary<string, IriReference> prefixes, TextWriter errorOutput)
    {
        var input = new UnbufferedCharStream(inputStream);
        var lexer = new TriGDocLexer(input)
        {
            TokenFactory = new CommonTokenFactory(true)
        };
        var tokens = new UnbufferedTokenStream(lexer);
        var parser = new TriGDocParser(tokens);
        var customErrorListener = new ParserErrorListener(errorOutput);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(customErrorListener);
        IParseTree tree = parser.trigDoc();
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
    public static Datastore ParseReader(TextReader textReader, TextWriter errorOutput, UInt32 initSize = 1024) =>
        ParseReader(textReader, new Dictionary<string, IriReference>(), errorOutput);

    /// <summary>
    /// Parses the content of the Stream containing RDF-1.2 Turtle without full file buffering. 
    /// Use this for large files to avoid loading the entire file into memory.
    /// </summary>
    /// <param name="inputStream">The large or infinite stream of rdf</param>
    /// <param name="initSize">The assumed number of triples, just for a start</param>
    /// <param name="errorOutput">Where errors should be written</param>
    /// <returns></returns>
    public static Datastore ParseStream(Stream inputStream, TextWriter errorOutput, UInt32 initSize = 1024)
    {
        return ParseAntlrUnbuffered(inputStream, initSize, new Dictionary<string, IriReference>(), errorOutput);
    }


    /// <summary>
    /// Parses the content of the string containing RDF-1.2 Turtle.
    /// </summary>
    /// <param name="owl"></param>
    /// <param name="errorOutput"></param>
    /// <returns></returns>
    public static Datastore ParseString(string owl, TextWriter errorOutput)
    {
        using TextReader textReader = new StringReader(owl);
        return ParseReader(textReader, errorOutput, (uint)owl.Length);
    }


}