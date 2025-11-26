

namespace DagSemTools.Api;

using DagSemTools.Turtle.Parser;

/// <summary>
/// Parses a Turtle file into an RDF Dataset. https://www.w3.org/TR/rdf12-turtle/
/// </summary>
public static class TriGParser
{
    /// <summary>
    /// Parses a TriG file into an RDF Dataset.
    /// </summary>
    /// <param name="rdfFile">A file with TriG RDf content</param>
    /// <param name="errorOutput">The stream where parser errors are output. Try Console.Error</param>
    /// <returns></returns>
    public static IDataset Parse(FileInfo rdfFile, TextWriter errorOutput)
    {
        var tt = Parser.ParseFile(rdfFile, errorOutput);
        return new Dataset(tt);
    }

    /// <summary>
    /// Parses a TriG string into an RDF Dataset.
    /// </summary>
    /// <param name="rdfString">A string with TriG RDf content</param>
    /// <param name="errorOutput">The stream where parser errors are output. Try Console.Error</param>
    /// <returns></returns>
    public static IDataset Parse(string rdfString, TextWriter errorOutput)
    {
        var tt = Parser.ParseString(rdfString, errorOutput);
        return new Dataset(tt);
    }
}