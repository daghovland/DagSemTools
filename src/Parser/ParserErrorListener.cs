using Antlr4.Runtime;

namespace AlcTableau.Parser;

public class ParserErrorListener : IAntlrErrorListener<IToken>, IVistorErrorListener
{
    private TextWriter output;
    public bool HasError { get; private set; } = false;

    public ParserErrorListener()
        : this(Console.Error)
    {
    }
    
    public ParserErrorListener(TextWriter output)
    {
        this.output = output;
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
        output.WriteLine($"line {line}:{charPositionInLine} {msg}");
    }
    
    /// <inheritdoc />
    public void VisitorError(IToken offendingSymbol, int line, int charPositionInLine, string msg)
    {
        output.WriteLine($"line {line}:{charPositionInLine} {msg}");
        HasError = true;
    }


}