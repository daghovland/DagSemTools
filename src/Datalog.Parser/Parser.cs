/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using DagSemTools;
using DagSemTools.Parser;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IriTools;
using DagSemTools.Rdf;


namespace DagSemTools.Datalog.Parser;


/// <summary>
/// Parser for a semipositiv datalog lanugage for Rdf
/// </summary>
public class Parser
{
    /// <summary>
    /// Parser the content of the file containing the datalog program
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="errorOutput"></param>
    /// <param name="datastore"></param>
    /// <returns></returns>
    public static IEnumerable<Rule> ParseFile(string filename, TextWriter errorOutput, Datastore datastore)
    {
        using TextReader textReader = File.OpenText(filename);
        return ParseReader(textReader, errorOutput, datastore);
    }

    /// <summary>
    /// Parser the content of the file containing the datalog program
    /// </summary>
    /// <param name="fInfo"></param>
    /// <param name="errorOutput"></param>
    /// <param name="datastore"></param>
    /// <returns></returns>
    public static IEnumerable<Rule> ParseFile(FileInfo fInfo, TextWriter errorOutput, Datastore datastore)
    {
        using TextReader textReader = File.OpenText(fInfo.FullName);
        return ParseReader(textReader, errorOutput, datastore);
    }


    /// <summary>
    /// Parses the content of the string containing datalog 
    /// </summary>
    /// <param name="datalog"></param>
    /// <param name="errorOutput"></param>
    /// <param name="datastore"></param>
    /// <returns></returns>
    public static IEnumerable<Rule> ParseString(string datalog, TextWriter errorOutput, Datastore datastore)
    {
        using TextReader textReader = new StringReader(datalog);
        return ParseReader(textReader, errorOutput, datastore);
    }

    /// <summary>
    /// Parses the content of the TextReader containing Datalog.
    /// </summary>
    /// <param name="textReader"></param>
    /// <param name="errorOutput"></param>
    /// <param name="datastore"></param>
    /// <returns></returns>
    public static IEnumerable<Rule> ParseReader(TextReader textReader, TextWriter errorOutput,
        Datastore datastore)
    {
        var input = new AntlrInputStream(textReader);
        var lexer = new DatalogLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new DatalogParser(tokens);
        var customErrorListener = new ParserErrorListener(errorOutput);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(customErrorListener);
        IParseTree tree = parser.datalogProgram();
        ParseTreeWalker walker = new ParseTreeWalker();
        var listener = new DatalogListener(datastore, customErrorListener);
        walker.Walk(listener, tree);

        if (customErrorListener.HasError)
        {
            throw new Exception("Syntax errors in Datalog file");
        }
        return listener.DatalogProgram;
    }
}