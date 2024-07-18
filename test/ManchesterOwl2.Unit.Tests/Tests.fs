module Tests

open System
open Xunit
open AlcTableau
open IriTools
open System.IO
open FSharp.Text.Lexing
open Manchester.Printer

    
#nowarn "3391"
let testLexerAndParserFromString text = 
    let lexbuf = LexBuffer<char>.FromString text
    Parser.start Lexer.tokenstream lexbuf


let testLexerAndParserFromFile (fileName:string)  = 
    use textReader = new System.IO.StreamReader(fileName)
    let lexbuf = LexBuffer<char>.FromTextReader textReader
    Parser.start Lexer.tokenstream lexbuf


[<Fact>]
let ``Single empty class frame can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Ontology: <https://example.com/ontology> 
    Class: <https://example.com/concept>
    """
    Assert.Equal<ALC.TBox>(parseValue, List.empty)
    
[<Fact>]
let ``Conjunction can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Ontology: <https://example.com/ontology> 
    Class: <https://example.com/concept> 
    SubClassOf: <https://example.com/concept1> and <https://example.com/concept2>
    """
    Assert.Equal<ALC.TBox>(parseValue, 
    [ALC.Inclusion(
        ALC.Conjunction(ALC.ConceptName("https://example.com/concept1"), ALC.ConceptName("https://example.com/concept2")), 
        ALC.ConceptName("https://example.com/concept"))
    ])

    
[<Fact>]
let ``Equivalence can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Ontology: <https://example.com/ontology> 
    Class: <https://example.com/concept> 
    EquivalentTo: <https://example.com/concept1>
    """
    Assert.Equal<ALC.TBox>(parseValue, 
    [ALC.Equivalence(
        ALC.ConceptName("https://example.com/concept1"), 
        ALC.ConceptName("https://example.com/concept"))
    ])


[<Fact>]
let ``Disjunction can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Ontology: <https://example.com/ontology> 
    Class: <https://example.com/concept> 
    SubClassOf: <https://example.com/concept1> or <https://example.com/concept2>
    """
    Assert.Equal<ALC.TBox>(parseValue, 
    [ALC.Inclusion(
        ALC.Disjunction(ALC.ConceptName("https://example.com/concept1"), ALC.ConceptName("https://example.com/concept2")), 
        ALC.ConceptName("https://example.com/concept"))
    ])


[<Fact>]
let ``Parenthesized expression can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Ontology: <https://example.com/ontology> 
    Class: <https://example.com/concept> 
    SubClassOf: ( <https://example.com/concept1> )
    """
    Assert.Equal<ALC.TBox>(parseValue, 
    [ALC.Inclusion(
        ALC.ConceptName("https://example.com/concept1"),
        ALC.ConceptName("https://example.com/concept"))
    ])
    
    
[<Fact>]
let ``Negation can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Ontology: <https://example.com/ontology> 
    Class: <https://example.com/concept> 
    SubClassOf: not <https://example.com/concept1>
    """
    Assert.Equal<ALC.TBox>(parseValue, 
    [ALC.Inclusion(
        ALC.Negation(ALC.ConceptName("https://example.com/concept1")),
        ALC.ConceptName("https://example.com/concept"))
    ])

[<Fact>]
let ``Existential can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Ontology: <https://example.com/ontology> 
    Class: <https://example.com/concept> 
    SubClassOf: <https://example.com/property1> some <https://example.com/concept1>
    """
    Assert.Equal<ALC.TBox>(parseValue, 
    [ALC.Inclusion(
        ALC.Existential(ALC.Role("https://example.com/property1"), ALC.ConceptName("https://example.com/concept1")),
        ALC.ConceptName("https://example.com/concept"))
    ])
    


[<Fact>]
let ``Universal can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Ontology: <https://example.com/ontology> 
    Class: <https://example.com/concept> 
    SubClassOf: <https://example.com/property1> only <https://example.com/concept1>
    """
    Assert.Equal<ALC.TBox>(parseValue, 
    [ALC.Inclusion(
        ALC.Universal(ALC.Role("https://example.com/property1"), ALC.ConceptName("https://example.com/concept1")),
        ALC.ConceptName("https://example.com/concept"))
    ])

[<Fact>]
let ``Iri with query can be parsed`` () =
    let parseValue = testLexerAndParserFromString "Ontology: <https://example.com/concept?query=1>"
    Assert.Equal<ALC.TBox>(parseValue, [])
    
    
[<Fact>]
let ``Iri with fragment can be parsed`` () =
    let parseValue = testLexerAndParserFromString "Ontology: <https://example.com/ontology#concept>"
    Assert.Equal<ALC.TBox>(parseValue,[] )
    
    
[<Fact>]
let ``Mail Iri cannot be parsed`` () =
    let parseValue () : obj =
        testLexerAndParserFromString "Ontology: <mailto://example.com/concept>" |> ignore :> obj
    let exc = Assert.Throws<System.Exception>(parseValue)
    Assert.NotNull(exc)
    
    
[<Fact>]
let ``Space Iri cannot be parsed`` () =
    let parseValue () : obj =
         testLexerAndParserFromString "Ontology: <https://example.com/concept with space>" |> ignore :> obj
            
    let exc = Assert.Throws<System.Exception>(parseValue)
    Assert.NotNull(exc)
    
    
    
[<Fact>]
let ``Prefixed iri can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Prefix: ex: <http://example.com> 
    Ontology: ex:ontology"""
    Assert.Equal<ALC.TBox>(parseValue, [])
    
[<Fact>]
let ``Versioned ontology IRI can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Prefix: : <http://ex.com/owl/families#>
    Prefix: g: <http://ex.com/owl2/families#>

    Ontology: <http://example.com/owl/families> <http://example.com/owl/families-v1>
    """
    Assert.Equal<ALC.TBox>(parseValue, [])


[<Fact>]
let ``Import can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Ontology: 
    Import: <http://ex.com/owl2/families.owl>
    """
    Assert.Equal<ALC.TBox>(parseValue, [])

[<Fact>]
let ``Multiple annotations can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Prefix: : <http://ex.com/owl/families#>
    Ontology: 
    Annotations: creator John, 
               mainClass Person
    """
    Assert.Equal<ALC.TBox>(parseValue, [])

[<Fact>]
let ``Annotations on  annotations can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Prefix: : <http://ex.com/owl/families#>
    Ontology: 
    Annotations: creator John, 
               Annotations: 
                 rdfs:comment "Creation Year"
                 creationYear 2008, 
               mainClass Person
    """
    Assert.Equal<ALC.TBox>(parseValue, [])


[<Fact>]
let ``Single annotation can be parsed`` () =
    let parseValue = testLexerAndParserFromString """
    Prefix: : <http://ex.com/owl/families#>
    Ontology: 
    Annotations: creator John
    """
    Assert.Equal<ALC.TBox>(parseValue, [])


    
[<Fact>]
let ``Reference example can be parsed`` () =
    let parseValue = testLexerAndParserFromFile "TestData/manchester-ref.owl"
    Assert.IsType<ALC.TBox>(parseValue)
    