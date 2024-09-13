using System.Text;
using Xunit.Abstractions;

namespace TurtleParser.Unit.Tests;

public class TestOutputTextWriter(ITestOutputHelper output) : TextWriter
{
    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? value)
    {
        output.WriteLine(value);
    }

    public override void Write(char value)
    {
        output.WriteLine(value.ToString());
    }
}