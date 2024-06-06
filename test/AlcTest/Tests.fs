module Tests

open System
open Xunit

[<Fact>]
let ``Alc Can Be Created`` () =
    let role = IriTools.IriReference("http://example.org/role")
    let concept = Alc.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let tbox = [ Alc.Inclusion(Alc.Top, Alc.Bottom) ]
    let abox = [ Alc.Member(IriTools.IriReference("http://example.org/individual"), concept) ]
    let kb = (tbox, abox)
    Assert.True(true)