module Tests

open System
open Xunit
open ManchesterOwl2
open ManchesterOwl2.Parser
open ManchesterOwl2.Syntax
open IriTools
open AlcTableau
open FParsec

    
[<Fact>]
let ``Manchester Syntax can be parsed`` () =
    let kb = ManchesterOwl2.Parser.parse "http://example.org/concept"
    Assert.True(kb.ToString().Length > 0)

    
[<Fact>]
let ``Manchester Iri can be parsed`` () =
    let parsedIri = run fullIri "<http://example.org/concept>"
    match parsedIri with
    | Failure(msg, _, _) -> Assert.True(false, msg)
    | Success(parsedIri, _, _) -> Assert.Equal(parsedIri, Syntax.FullIri ( IriReference "http://example.org/concept") )
    
[<Fact>]
let ``Prefixed Iri can be parsed`` () =
    let parsedIri = run abbreviatedIRI "ex:concept"
    match parsedIri with
    | Failure(msg, _, _) -> Assert.True(false, msg)
    | Success(parsedIri, _, _) -> Assert.Equal(parsedIri, Syntax.AbbreviatedIri ("ex", "concept"))
    