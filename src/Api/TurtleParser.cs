using AlcTableau.TurtleAntlr;

namespace Api;

/// <summary>
/// Parses a Turtle file into an RDF graph. https://www.w3.org/TR/rdf12-turtle/
/// It is not complete, and f.ex. does not support blank nodes and reification.
/// </summary>
public static class TurtleParser
{
    /// <summary>
    /// Parses a Turtle file into an RDF graph.
    /// </summary>
    /// <param name="rdf_file"></param>
    /// <returns></returns>
    public static IGraph Parse(FileInfo rdf_file)
    {
        var tt = Parser.ParseFile(rdf_file);
        return new Graph(tt);
    }
}