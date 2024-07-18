namespace AlcTableau.ManchesterAntlr;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;


public class Program
{


    static void Main(string[] args)
    {
            using TextReader text_reader = File.OpenText(args[0]);

        
            // Create an input character stream from standard in
            var input = new AntlrInputStream(text_reader);
            // Create an ExprLexer that feeds from that stream
            ManchesterLexer lexer = new ManchesterLexer(input);
            // Create a stream of tokens fed by the lexer
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            // Create a parser that feeds off the token stream
            ManchesterParser parser = new ManchesterParser(tokens);
            // tell ANTLR to build a parse tree
            parser.BuildParseTree = true; 
            // Begin parsing at rule file
            IParseTree tree = parser.ontologyDocument();
            ManchesterVisitor visitor = new ManchesterVisitor();
            

            Console.WriteLine(tree.ToStringTree(parser)); // print LISP-style tree

            visitor.Visit(tree);

            // ParseTreeWalker walker = new ParseTreeWalker();
            // var listener = new ManchesterListener();
            // walker.Walk(listener, tree);
            // foreach (var error in listener.errors)
            // {
            //         Console.WriteLine($"Error {error.ToString()}");
            // }
            
    }
    
}
