using AlcTableau;
using Antlr4.Runtime;
using Xunit.Abstractions;

namespace ManchesterAntlr.Unit.Tests;

public class TestErrorListener : BaseErrorListener
{
    private ITestOutputHelper output;
    public string ErrorString { get; private set; }
    public TestErrorListener(ITestOutputHelper output)
    {
        this.output = output;
        this.ErrorString = "";
    }
    
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line,
        int charPositionInLine, string msg, RecognitionException e)
    {
        output.WriteLine($"line {line}:{charPositionInLine} {msg}");
        this.ErrorString = $"line {line}:{charPositionInLine} {msg}";
    }
}