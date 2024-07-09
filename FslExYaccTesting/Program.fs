module FslExYaccTesting.Program

// Learn more about F# at http://fsharp.net

open System.IO
open FSharp.Text.Lexing
open Manchester.Printer

let testLexerAndParserFromString text expectedCount = 
    let lexbuf = LexBuffer<char>.FromString text

    let countFromParser = Parser.start Lexer.tokenstream lexbuf

    printfn "countFromParser: result = %s, expected %s" (Manchester.Printer.toString countFromParser) expectedCount



let testLexerAndParserFromFile (fileName:string) expectedCount = 
    use textReader = new System.IO.StreamReader(fileName)
    let lexbuf = LexBuffer<char>.FromTextReader textReader

    let countFromParser = Parser.start Lexer.tokenstream lexbuf

    printfn "countFromParser: result = %s, expected %s" (toString countFromParser) expectedCount

testLexerAndParserFromString "ex:hello" "ex:hello"
testLexerAndParserFromString "ex:hello and ex:hello" "ex:hello and ex:hello"

testLexerAndParserFromString "ex:prop some ex:hello" "ex:prop some ex:hello"

let testFile = Path.Combine(__SOURCE_DIRECTORY__, "test.txt")
File.WriteAllText(testFile, "<https://example.com/concept>")
testLexerAndParserFromFile testFile "<https://example.com/concept>"

printfn "Press any key to continue..."
System.Console.ReadLine() |> ignore