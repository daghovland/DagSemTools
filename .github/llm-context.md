# LLM Development Context

## Quick Start for AI Assistants
1. This is a C# and F# semantic web library targeting .NET 10
2. The intention is to create a correct and fast library for handling RDF in .NET projects
2. The reasoner and other core functionality is implemented in F#, while the parser and the client-facing API in src/Api is in C#
2. Build: `dotnet build`
3. Test: `dotnet test` (requires test data setup - see BUILDING.md)
4. Main areas: RDF parsing, SPARQL, OWL reasoning, Datalog

## Current Focus
- SPARQL query support expansion (check story/sparql-tests branch)
- Datalog engine extension with similar functions as SPARQL. Ideally similar implementation as for SPARQL
- Speed of Rdf parsing and parsing Rdf to Owl
- Parser correctness vs W3C specs

## When Working on Parsers
1. Grammar files in grammars/ are source of truth. The grammar files in the individual projects are softlinks to the top-level grammar directory
2. Regenerate parser: `antlr4 -Dlanguage=CSharp -visitor grammar.g4`
3. Update visitor and/or listener classes in corresponding parser project
4. Add test cases covering new syntax

## Common Pitfalls
- SPARQL injection: no built-in protection currently
- Stratification required for Datalog with negation

## Code Navigation Tips
- **Client-facing API**: src/Api
- **Graph implementation**: src/Rdf/QuadTable.fs
- **SPARQL query evaluation**: src/Rdf/QueryProcessor.fs
- **Datalog engine**: src/Datalog/
- **Parser visitors**: src/*/Visitor.cs files
- **Parser listeners**: src/*/Listener.cs files

## Supported Features
### RDF/Turtle
- ✅ Turtle 1.2
- ✅ TriG (named graphs)
- ✅ Blank nodes

### SPARQL
- ✅ SELECT queries
- ✅ Basic graph patterns
- ✅ Aggregate functions (recent addition)
- ❌ CONSTRUCT/ASK/DESCRIBE
- ❌ FILTER expressions
- ❌ OPTIONAL patterns

### OWL
- ✅ Manchester syntax
- ✅ Rdf syntax (Parser from Rdf to Owl in src/RdfOwlTransator)
- ✅ OWL 2 RL subset supported with rule-based reasoning (Parser from Owl to Datalog in src/OWL2RL2Datalog)
- ✅ The most basic Tableau-based reasoning in src/AlcTableau supporting acyclic ALC ontologies

### Datalog
- ✅ Supports negation and recursion of stratifiable datalog programs
- ✅ Rule engine in src/Datalog/Reasoner.fs
- ❌ Built-in functions beyond triples
