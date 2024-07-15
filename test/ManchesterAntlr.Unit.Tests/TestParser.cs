namespace ManchesterAntlr.Unit.Tests;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;


public class UnitTest1
{


    public void testFile(string filename){
        using TextReader text_reader = File.OpenText(filename);

           testReader(text_reader);

    }

    public void testReader(TextReader text_reader){
        
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
            
            Console.WriteLine(tree.ToStringTree(parser)); // print LISP-style tree

    }


        public void testString(string owl){
            using TextReader text_reader = new StringReader(owl);
                testReader(text_reader);
    }


    [Fact]
    public void TestSmallestOntology()
    {
        testString("Prefix: Ontology:");
    }

    
    [Fact]
    public void TestOntologyWithIri()
    {
        testString("Prefix: Ontology: <https://example.com/ontology>");
    }

    
    [Fact]
    public void TurtleGivesEroor()
    {
        testString("<http://example.com/concecpt1> <http://example.com/role1> <http://example.com/concecpt2> .");
    }


}