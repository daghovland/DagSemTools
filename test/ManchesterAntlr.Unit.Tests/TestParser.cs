namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using AlcTableau.ManchesterAntlr;
using FluentAssertions;

public class TestParser
{


    public AlcTableau.ManchesterAntlr.ManchesterListener testFile(string filename){
        using TextReader text_reader = File.OpenText(filename);

           return testReader(text_reader);

    }

    public ManchesterListener testReader(TextReader text_reader){
        
            // Create an input character stream from standard in
            var input = new AntlrInputStream(text_reader);
            // Create an ExprLexer that feeds from that stream
            ManchesterLexer lexer = new ManchesterLexer(input);
            // Create a stream of tokens fed by the lexer
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            // Create a parser that feeds off the token stream
            ManchesterParser parser = new ManchesterParser(tokens);
            // Begin parsing at rule r
            
            IParseTree tree = parser.ontologyDocument();
            ParseTreeWalker walker = new ParseTreeWalker();
            var listener = new AlcTableau.ManchesterAntlr.ManchesterListener();
            walker.Walk(listener, tree);
            
            Console.WriteLine(tree.ToStringTree(parser)); // print LISP-style tree
            return listener;

    }


        public AlcTableau.ManchesterAntlr.ManchesterListener testString(string owl){
            using TextReader text_reader = new StringReader(owl);
                return testReader(text_reader);
    }


    [Fact]
    public void TestSmallestOntology()
    {
        var listener = testString("Prefix: Ontology:");
        Assert.Empty(listener.errors);
    }

    
    [Fact]
    public void TestOntologyWithIri()
    {
        var listener = testString("Prefix: Ontology: <https://example.com/ontology>");
        Assert.Empty(listener.errors);
    }

    
    [Fact]
    public void TurtleGivesEroor()
    {
        var listener = testString("<http://example.com/concecpt1> <http://example.com/role1> <http://example.com/concecpt2> .");
        listener.errors.Should().NotBeEmpty("The parser should not allow nquads");
    }


}