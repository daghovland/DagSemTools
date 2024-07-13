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
    

let exConcept = ALC.ConceptName("https://example.com/concept")
let exConcept2 = ALC.ConceptName("https://example.com/concept2")
let exRole = ALC.Role("https://example.com/role")

[<Fact>]
let ``Double negation can be pushed`` () =
    let c = ALC.Negation(ALC.Negation(exConcept))
    let c_nnf = NNF.nnf c
    Assert.Equal(exConcept, c_nnf)


[<Fact>]
let ``Conjunction can be pushed`` () =
    let c = ALC.Negation(ALC.Conjunction(exConcept, exConcept2))
    let c_nnf = NNF.nnf c
    Assert.Equal(ALC.Disjunction(ALC.Negation exConcept, ALC.Negation exConcept2), c_nnf)

[<Fact>]
let ``Disjunction can be pushed`` () =
    let c = ALC.Negation(ALC.Disjunction(exConcept, exConcept2))
    let c_nnf = NNF.nnf c
    Assert.Equal(ALC.Conjunction(ALC.Negation exConcept, ALC.Negation exConcept2), c_nnf)

[<Fact>]
let ``Existential can be pushed`` () =
    let c = ALC.Negation(ALC.Existential(exRole, exConcept))
    let c_nnf = NNF.nnf c
    Assert.Equal(ALC.Universal(exRole, ALC.Negation exConcept), c_nnf)


[<Fact>]
let ``Universal can be pushed`` () =
    let c = ALC.Negation(ALC.Universal(exRole, exConcept))
    let c_nnf = NNF.nnf c
    Assert.Equal(ALC.Existential(exRole, ALC.Negation exConcept), c_nnf)