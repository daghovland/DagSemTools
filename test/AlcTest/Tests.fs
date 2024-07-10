module Tests

open System
open Xunit
open AlcTableau
open IriTools
open System.IO
open FSharp.Text.Lexing
open Manchester.Printer

[<Fact>]
let ``Alc Can Be Created`` () =
    let role = IriTools.IriReference("http://example.org/role")
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let tbox = [ ALC.Inclusion(ALC.Top, ALC.Bottom) ]
    let abox = [ ALC.Member (IriReference "http://example.org/individual", concept),
                  ALC.RoleMember (IriReference "http://example.org/individual", role, IriReference "http://example.org/individual2")]
    let kb = (tbox, abox)
    Assert.True(kb.ToString().Length > 0)
    

let testLexerAndParserFromString text = 
    let lexbuf = LexBuffer<char>.FromString text

    Parser.start Lexer.tokenstream lexbuf



[<Fact>]
let ``Iri can be parsed`` () =
    let parseValue = testLexerAndParserFromString "<https://example.com/concept>"
    Assert.Equal(parseValue, ALC.ConceptName("https://example.com/concept"))
    
[<Fact>]
let ``Conjunction can be parsed`` () =
    let parseValue = testLexerAndParserFromString "<https://example.com/concept1> and <https://example.com/concept2>"
    Assert.Equal(parseValue, ALC.Conjunction(ALC.ConceptName("https://example.com/concept1"), ALC.ConceptName("https://example.com/concept2")))

    
[<Fact>]
let ``Disjunction can be parsed`` () =
    let parseValue = testLexerAndParserFromString "<https://example.com/concept1> or <https://example.com/concept2>"
    Assert.Equal(parseValue, ALC.Disjunction(ALC.ConceptName("https://example.com/concept1"), ALC.ConceptName("https://example.com/concept2")))

    
[<Fact>]
let ``Universal can be parsed`` () =
    let parseValue = testLexerAndParserFromString "<https://example.com/concept1> or <https://example.com/concept2>"
    Assert.Equal(parseValue, ALC.Disjunction(ALC.ConceptName("https://example.com/concept1"), ALC.ConceptName("https://example.com/concept2")))


[<Fact>]
let ``Parenthesized expression can be parsed`` () =
    let parseValue = testLexerAndParserFromString "( <https://example.com/concept1> )"
    Assert.Equal(parseValue, ALC.ConceptName("https://example.com/concept1"))
    
[<Fact>]
let ``Negation can be parsed`` () =
    let parseValue = testLexerAndParserFromString "not <https://example.com/concept1>"
    Assert.Equal(parseValue, ALC.Negation(ALC.ConceptName("https://example.com/concept1")))


[<Fact>]
let ``Existential can be parsed`` () =
    let parseValue = testLexerAndParserFromString "<https://example.com/property1> some <https://example.com/concept1>"
    Assert.Equal(parseValue, ALC.Existential(ALC.Role("https://example.com/property1"), ALC.ConceptName("https://example.com/concept1")))

[<Fact>]
let ``Universal can be parsed`` () =
    let parseValue = testLexerAndParserFromString "<https://example.com/property1> only <https://example.com/concept1>"
    Assert.Equal(parseValue, ALC.Universal(ALC.Role("https://example.com/property1"), ALC.ConceptName("https://example.com/concept1")))


[<Fact>]
let ``Iri with query can be parsed`` () =
    let parseValue = testLexerAndParserFromString "<https://example.com/concept?query=1>"
    Assert.Equal(parseValue, ALC.ConceptName("https://example.com/concept?query=1"))
    

[<Fact>]
let ``Iri with fragment can be parsed`` () =
    let parseValue = testLexerAndParserFromString "<https://example.com/ontology#concept>"
    Assert.Equal(parseValue,ALC.ConceptName("https://example.com/ontology#concept") )
    
[<Fact>]
let ``Mail Iri cannot be parsed`` () =
    let parseValue () : obj =
        testLexerAndParserFromString "<mailto://example.com/concept>" |> ignore :> obj
    let exc = Assert.Throws<System.Exception>(parseValue)
    Assert.NotNull(exc)
    
    
[<Fact>]
let ``Space Iri cannot be parsed`` () =
    let parseValue () : obj =
         testLexerAndParserFromString "<https://example.com/concept with space>" |> ignore :> obj
            
    let exc = Assert.Throws<System.Exception>(parseValue)
    Assert.NotNull(exc)
    
    
    
[<Fact>]
let ``Prefixed iri can be parsed`` () =
    let parseValue = testLexerAndParserFromString "ex:concept"
    Assert.Equal(parseValue, ALC.ConceptName("ex:concept"))
    