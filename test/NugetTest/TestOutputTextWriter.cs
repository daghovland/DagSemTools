using System.Text;
using Xunit.Abstractions;

namespace NugetTest;

public class TestOutputTextWriter(ITestOutputHelper output) : TextWriter
{
    public string LastError { get; private set; } = "";
    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? value)
    {
        LastError = value ?? "";
        output.WriteLine(value);
    }

    public override void Write(char value)
    {
        output.WriteLine(value.ToString());
    }
}