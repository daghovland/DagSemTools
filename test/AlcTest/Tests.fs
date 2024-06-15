module Tests

open System
open Xunit
open AlcTableau
open IriTools

[<Fact>]
let ``Alc Can Be Created`` () =
    let role = IriTools.IriReference("http://example.org/role")
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let tbox = [ ALC.Inclusion(ALC.Top, ALC.Bottom) ]
    let abox = [ ALC.Member (IriReference "http://example.org/individual", concept),
                  ALC.RoleMember (IriReference "http://example.org/individual", role, IriReference "http://example.org/individual2")]
    let kb = (tbox, abox)
    Assert.True(kb.ToString().Length > 0)
    