

namespace DagSemTools.Api;

using DagSemTools.Turtle.Parser;

/// <summary>
/// Parses a Turtle file into an RDF graph. https://www.w3.org/TR/rdf12-turtle/
/// It is not complete, and f.ex. does not support blank nodes and reification.
/// </summary>
public static class TurtleParser
{
    /// <summary>
    /// Parses a Turtle file into an RDF graph.
    /// </summary>
    /// <param name="rdfFile">A file with Turtle RDf content</param>
    /// <param name="errorOutput">The stream where parser errors are output. Try Console.Error</param>
    /// <returns></returns>
    public static IGraph Parse(FileInfo rdfFile, TextWriter errorOutput)
    {
        var tt = DagSemTools.Turtle.Parser.Parser.ParseFile(rdfFile, errorOutput);
        return new Graph(tt);
    }

    /// <summary>
    /// Parses a Turtle string into an RDF graph.
    /// </summary>
    /// <param name="rdfString">A string with Turtle RDf content</param>
    /// <param name="errorOutput">The stream where parser errors are output. Try Console.Error</param>
    /// <returns></returns>
    public static IGraph Parse(string rdfString, TextWriter errorOutput)
    {
        var tt = DagSemTools.Turtle.Parser.Parser.ParseString(rdfString, errorOutput);
        return new Graph(tt);
    }
}