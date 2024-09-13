using Antlr4.Runtime;

namespace AlcTableau.Parser;

public class ParserErrorListener : IAntlrErrorListener<IToken>, IVistorErrorListener
{
    private TextWriter output;
    private ConsoleErrorListener<IToken> consoleErrorListener;
    
    public ParserErrorListener()
    {
        this.output = Console.Error;
        this.consoleErrorListener = new ConsoleErrorListener<IToken>();
    }
    
    public ParserErrorListener(TextWriter output)
    {
        this.output = output;
        this.consoleErrorListener = new ConsoleErrorListener<IToken>();
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
        consoleErrorListener.SyntaxError(output, recognizer, offendingSymbol, line, charPositionInLine, msg, e);
    }
    
    /// <inheritdoc />
    public void SyntaxError(TextWriter output, IToken offendingSymbol, int line, int charPositionInLine, string msg,
        RecognitionException e)
    {
        output.WriteLine($"line {line}:{charPositionInLine} {msg}");
    }
}