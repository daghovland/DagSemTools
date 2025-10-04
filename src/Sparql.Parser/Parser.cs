/*
    Copyright (C) 2025 Dag Hovland
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

namespace DagSemTools.Sparql.Parser;

/// <summary>
/// Parser for SPARQL queries
/// </summary>
public static class Parser
{
    internal static (Query.SelectQuery, GraphElementManager) ParseFile(string filename, TextWriter errorOutput)
    {
        using TextReader textReader = File.OpenText(filename);
        return ParseReader(textReader, (uint)(new FileInfo(filename).Length), errorOutput);
    }

    internal static (Query.SelectQuery, GraphElementManager) ParseFile(FileInfo fInfo, TextWriter errorOutput)
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
    /// <param name="elementManager">The mapper between rdf resources and integer indices</param>
    /// <returns></returns>
    public static (Query.SelectQuery, GraphElementManager) ParseReader(TextReader textReader, UInt32 initSize, Dictionary<string, IriReference> prefixes, TextWriter errorOutput, GraphElementManager? elementManager = null)
    {
        var input = new AntlrInputStream(textReader);
        var lexer = new SparqlLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new SparqlParser(tokens);
        var customErrorListener = new ParserErrorListener(errorOutput);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(customErrorListener);
        IParseTree tree = parser.queryUnit();
        ParseTreeWalker walker = new ParseTreeWalker();
        var listener = elementManager == null
            ? new SparqlListener(customErrorListener)
            : new SparqlListener(customErrorListener, elementManager);
        walker.Walk(listener, tree);
        if (customErrorListener.HasError)
        {
            throw new Exception("Syntax errors in Turtle file");
        }
        return (listener.Result, listener.ElementManager);
    }

    /// <summary>
    /// Parses the content of the TextReader containing RDF-1.2 Turtle.
    /// </summary>
    /// <param name="textReader"></param>
    /// <param name="initSize"></param>
    /// <param name="errorOutput"></param>
    /// <returns></returns>
    public static (Query.SelectQuery, GraphElementManager) ParseReader(TextReader textReader, UInt32 initSize, TextWriter errorOutput) =>
        ParseReader(textReader, initSize, new Dictionary<string, IriReference>(), errorOutput);

    /// <summary>
    /// Parses the content of the string containing RDF-1.2 Turtle.
    /// </summary>
    /// <param name="owl"></param>
    /// <param name="errorOutput"></param>
    /// <returns></returns>
    public static (Query.SelectQuery, GraphElementManager) ParseString(string owl, TextWriter errorOutput)
    {
        using TextReader textReader = new StringReader(owl);
        return ParseReader(textReader, (uint)owl.Length, errorOutput);
    }


}