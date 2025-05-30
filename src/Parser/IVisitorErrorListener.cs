using Antlr4.Runtime;

namespace DagSemTools.Parser;

/// <summary>
/// The interface for the error listener used by the visitor patterns for the turtle and manchester parsers
/// Currently only ParserErrorListener implements this interface
/// </summary>
public interface IVisitorErrorListener
{
    /// <summary>
    /// The method that is called by the visitor when a syntax error is encountered
    /// </summary>
    /// <param name="offendingSymbol"></param>
    /// <param name="line"></param>
    /// <param name="charPositionInLine"></param>
    /// <param name="msg"></param>
    public void VisitorError(
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg);

    /// <summary>
    /// Returns true if VisitorError was never called
    /// </summary>
    public bool HasError { get; }
}