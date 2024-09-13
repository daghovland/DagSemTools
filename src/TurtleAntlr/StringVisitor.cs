namespace AlcTableau.TurtleAntlr;

internal class StringVisitor : TurtleBaseVisitor<string>
{
    public override string VisitString_single_quote(TurtleParser.String_single_quoteContext context)
    {
        return context.GetText()[1..^1];
    }

    public override string VisitString_triple_quote(TurtleParser.String_triple_quoteContext context)
    {
        return context.GetText()[3..^3];
    }
}