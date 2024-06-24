namespace ManchesterOwl2
open IriTools
open AlcTableau
open FParsec
open ManchesterOwl2.Syntax

// Parses OWL in Machester syntax https://www.w3.org/TR/owl2-manchester-syntax/
module Parser =
    let parse knowledgebase =
        ALC.KnowledgeBase ([], [])
        
    let RFC3987IRI s = (pstring s)
    
    let isPN_CHARS_BASE c =
        let uc = int c
        (uc >= 0x41 && uc <= 0x5A) || // A-Z
        (uc >= 0x61 && uc <= 0x7A) || // a-z
        (uc >= 0xC0 && uc <= 0xD6) || // #x00C0-#x00D6
        (uc >= 0xD8 && uc <= 0xF6) || // #x00D8-#x00F6
        (uc >= 0xF8 && uc <= 0x2FF) || // #x00F8-#x02FF
        (uc >= 0x370 && uc <= 0x37D) || // #x0370-#x037D
        (uc >= 0x37F && uc <= 0x1FFF) || // #x037F-#x1FFF
        (uc >= 0x200C && uc <= 0x200D) || // #x200C-#x200D
        (uc >= 0x2070 && uc <= 0x218F) || // #x2070-#x218F
        (uc >= 0x2C00 && uc <= 0x2FEF) || // #x2C00-#x2FEF
        (uc >= 0x3001 && uc <= 0xD7FF) || // #x3001-#xD7FF
        (uc >= 0xF900 && uc <= 0xFDCF) || // #xF900-#xFDCF
        (uc >= 0xFDF0 && uc <= 0xFFFD) || // #xFDF0-#xFFFD
        (uc >= 0x10000 && uc <= 0xEFFFF) // #x10000-#xEFFFF

    let isPN_CHARS_U c =
        isPN_CHARS_BASE c || c = '_'
    let isPN_CHARS c =
        isPN_CHARS_U c || c = '-' || c = '0' || c = '9' || c = '\u00B7' || (c >= '\u0300' && c <= '\u036F') || (c >= '\u203F' && c <= '\u2040')
    // let PN_CHARS_BASE = satisfy isPN_CHARS_BASE
    // let PN_CHARS = satisfy isPN_CHARS
    
    // This is the SPARQL gramar production PN_PREFIX
    let PN_PREFIX : Parser<string, Unit> =
        many1Satisfy2L isPN_CHARS_BASE isPN_CHARS "prefix" 

    // This is the PN_LOCAL production in the sparql grammar
    // And the simpleIRI production in the Manchester syntax
    let simpleIRI : Parser<string, Unit> =
        let isFirstLetter c = isPN_CHARS_U c || System.Char.IsDigit c
        let isLetter c = isPN_CHARS c || c = '.'
        many1Satisfy2L isFirstLetter isLetter "simple IRI" |>> (fun s -> s)
        
    // This is the SPARQL production PNAME_LN
    // Also the OWL 2 Manchester production abbreviatedIRI
    let abbreviatedIRI : Parser<IRI, Unit> =
        pipe2 PN_PREFIX ( pstring ":"  >>. simpleIRI) (fun p n -> AbbreviatedIri(p, n))
    
    
    let fullIri : Parser<IRI, Unit> =
        let IRIchar = satisfy (fun c -> c <> '>')
        pstring "<" >>. (manyChars IRIchar) .>> pstring ">" |>> (fun s -> FullIri (IriReference s))
        
    let IriParser: Parser<IRI, Unit> = fullIri <|> abbreviatedIRI
    
    let atomicRestrictionParser : Parser<restriction, Unit> =
        IriParser |>> (fun iri -> AtomicRestriction (Class iri))
    let negativeRestrictionParser : Parser<restriction, Unit> =
        pstring "not" >>. atomicRestrictionParser |>> (fun restriction -> NegativeRestriction restriction)
    // let rec restrictionParser : Parser<restriction, Unit> =
    //     existentialRestrictionParser <|> universalRestrictionParser <|> negativeRestrictionParser <|> atomicRestrictionParser
    // and existentialRestrictionParser : Parser<restriction, Unit> =
    //     IriParser .>> pstring "some" >>. restrictionParser |>> (fun restriction -> ExistentialRestriction restriction)
    //
    // and universalRestrictionParser : Parser<restriction, Unit> =
    //     IriParser .>> pstring "only" >>. restrictionParser |>> (fun restriction -> ExistentialRestriction restriction)
    //
    // and conjunctionParser : Parser<conjunction, Unit> =
    //     pipe2 IriParser restrictionListParser (fun iri restrictionList -> conjunction (iri, restrictionList))
    //
    // and restrictionListParser : Parser<restriction list, Unit> =
    //     pstring "that" >>. sepBy restrictionParser (pstring "and") |>> (fun  restrictionList -> restrictionList)
    //
    //
    // and descriptionParser : Parser<description, Unit> =
    //     sepBy conjunctionParser (pstring "or") |>> (fun conjunctionList -> ConjunctionList conjunctionList)
    //
    let SubClassOfParser : Parser<ClassFrameElement, Unit> =
        pstring "SubClassOf:" >>. sepBy IriParser (pstring ",") |>> (fun irilist -> SubClassOf irilist)       
    
    let test p str =
        match run p str with
        | Success(result, _, _)   -> printfn "Success: %A" result
        | Failure(errorMsg, _, _) -> printfn "Failure: %s" errorMsg

