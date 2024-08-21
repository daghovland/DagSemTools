﻿using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

// Thansk to Ken Domino to have created Antlr4BuildTasks
// This allows to use the latest Antlr4.Runtime.Standard, Version 4.10.1
// https://github.com/kaby76/Antlr4BuildTasks

namespace Install
{
    class Program
    {
        static void Main(string[] args)
        {
            using TextReader text_reader = File.OpenText("example1.ttl");

            // Create an input character stream from standard in
            var input = new AntlrInputStream(text_reader);
            // Create an ExprLexer that feeds from that stream
            TURTLELexer lexer = new TURTLELexer(input);
            // Create a stream of tokens fed by the lexer
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            // Create a parser that feeds off the token stream
            TURTLEParser parser = new TURTLEParser(tokens);
            // Begin parsing at rule r

            IParseTree tree = parser.turtleDoc();


            Console.WriteLine(tree.ToStringTree(parser)); // print LISP-style tree

            // Look in obj/Debug/net6.0/HelloParser.cs
            // public RContext r()
        }
    }
}