using Antlr4.Runtime;

namespace AlcTableau.Parser;

/// <inheritdoc />
public class ParserErrorListener : IAntlrErrorListener<IToken>, IVistorErrorListener
{
    private readonly TextWriter _output;

    /// <inheritdoc />
    public bool HasError { get; private set; } = false;

    /// <summary>
    /// 
    /// </summary>
    public ParserErrorListener()
        : this(Console.Error)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="output"></param>
    public ParserErrorListener(TextWriter output)
    {
        this._output = output;
    }

    /// <inheritdoc />
    public void SyntaxError(
        TextWriter _,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        _output.WriteLine($"line {line}:{charPositionInLine} {msg}");
    }

    /// <inheritdoc />
    public void VisitorError(IToken offendingSymbol, int line, int charPositionInLine, string msg)
    {
        _output.WriteLine($"line {line}:{charPositionInLine} {msg}");
        HasError = true;
    }


}