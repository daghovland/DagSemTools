module Tests

open System
open AlcTableau.ALC
open Xunit
open AlcTableau
open IriTools
open System.IO
open Manchester.Printer

[<Fact>]
let ``Alc Can Be Created`` () =
    let role = IriTools.IriReference("http://example.org/role")
    let concept = ALC.ConceptName(IriTools.IriReference("http://example.org/concept"))
    let tbox = [ ALC.Inclusion(ALC.Top, ALC.Bottom) ]
    let abox = [ ALC.ConceptAssertion (IriReference "http://example.org/individual", concept),
                  ALC.ABoxAssertion.RoleAssertion (IriReference "http://example.org/individual", role, Role.Iri( IriReference "http://example.org/individual2"))]
    let kb = (tbox, abox)
    Assert.True(kb.ToString().Length > 0)
    

let exConcept = ALC.ConceptName(new IriReference "https://example.com/concept")
let exConcept2 = ALC.ConceptName(new IriReference "https://example.com/concept2")
let exConcept3 = ALC.ConceptName(new IriReference "https://example.com/concept3")
let exRole = ALC.Role.Iri (new IriReference "https://example.com/role")

[<Fact>]
let ``ConceptsNames can be compared``() =
    let result1 = (exConcept :> IComparable).CompareTo(exConcept2)
    Assert.True(result1 < 0)
    let result2 = (exConcept2 :> IComparable).CompareTo(exConcept)
    Assert.True(result2 > 0)
    let equal_result = exConcept.Equals(exConcept2)
    Assert.False(equal_result)
    let hashCode1 = exConcept.GetHashCode()
    let hashCode2 = exConcept2.GetHashCode()
    Assert.NotEqual(hashCode1, hashCode2)


[<Fact>]
let ``Conjunctions can be compared``() =
    let conj1 = ALC.Conjunction(exConcept, exConcept2)
    let conj2 = ALC.Conjunction(exConcept, exConcept3)
    let result1 = (conj1 :> IComparable).CompareTo(conj2)
    Assert.True(result1 < 0)
    let result2 = (conj2 :> IComparable).CompareTo(conj1)
    Assert.True(result2 > 0)
    let equal_result = conj1.Equals(conj2)
    Assert.False(equal_result)
    let hashCode1 = conj1.GetHashCode()
    let hashCode2 = conj2.GetHashCode()
    Assert.NotEqual(hashCode1, hashCode2)


[<Fact>]
let ``Disjunctions can be compared``() =
    let conj1 = ALC.Disjunction(exConcept, exConcept2)
    let conj2 = ALC.Disjunction(exConcept, exConcept3)
    let result1 = (conj1 :> IComparable).CompareTo(conj2)
    Assert.True(result1 < 0)
    let result2 = (conj2 :> IComparable).CompareTo(conj1)
    Assert.True(result2 > 0)
    let equal_result = conj1.Equals(conj2)
    Assert.False(equal_result)
    let hashCode1 = conj1.GetHashCode()
    let hashCode2 = conj2.GetHashCode()
    Assert.NotEqual(hashCode1, hashCode2)


[<Fact>]
let ``Universals can be compared``() =
    let conj1 = ALC.Universal(exRole, exConcept)
    let conj2 = ALC.Universal(exRole, exConcept2)
    let result1 = (conj1 :> IComparable).CompareTo(conj2)
    Assert.True(result1 < 0)
    let result2 = (conj2 :> IComparable).CompareTo(conj1)
    Assert.True(result2 > 0)
    let equal_result = conj1.Equals(conj2)
    Assert.False(equal_result)
    let hashCode1 = conj1.GetHashCode()
    let hashCode2 = conj2.GetHashCode()
    Assert.NotEqual(hashCode1, hashCode2)

[<Fact>]
let ``Existentials can be compared``() =
    let conj1 = ALC.Existential(exRole, exConcept)
    let conj2 = ALC.Existential(exRole, exConcept2)
    let result1 = (conj1 :> IComparable).CompareTo(conj2)
    Assert.True(result1 < 0)
    let result2 = (conj2 :> IComparable).CompareTo(conj1)
    Assert.True(result2 > 0)
    let equal_result = conj1.Equals(conj2)
    Assert.False(equal_result)
    let hashCode1 = conj1.GetHashCode()
    let hashCode2 = conj2.GetHashCode()
    Assert.NotEqual(hashCode1, hashCode2)


[<Fact>]
let ``Double negation can be pushed`` () =
    let c = ALC.Negation(ALC.Negation(exConcept))
    let c_nnf = NNF.nnf_concept c
    Assert.Equal(exConcept, c_nnf)


[<Fact>]
let ``Conjunction can be pushed`` () =
    let c = ALC.Negation(ALC.Conjunction(exConcept, exConcept2))
    let c_nnf = NNF.nnf_concept c
    Assert.Equal(ALC.Disjunction(ALC.Negation exConcept, ALC.Negation exConcept2), c_nnf)

[<Fact>]
let ``Disjunction can be pushed`` () =
    let c = ALC.Negation(ALC.Disjunction(exConcept, exConcept2))
    let c_nnf = NNF.nnf_concept c
    Assert.Equal(ALC.Conjunction(ALC.Negation exConcept, ALC.Negation exConcept2), c_nnf)

[<Fact>]
let ``Existential can be pushed`` () =
    let c = ALC.Negation(ALC.Existential(exRole, exConcept))
    let c_nnf = NNF.nnf_concept c
    Assert.Equal(ALC.Universal(exRole, ALC.Negation exConcept), c_nnf)


[<Fact>]
let ``Universal can be pushed`` () =
    let c = ALC.Negation(ALC.Universal(exRole, exConcept))
    let c_nnf = NNF.nnf_concept c
    Assert.Equal(ALC.Existential(exRole, ALC.Negation exConcept), c_nnf)