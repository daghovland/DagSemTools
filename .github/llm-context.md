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
- Parser correctness vs W3C specs

## When Working on Parsers
1. Grammar files in grammars/ are source of truth
2. Regenerate parser: `antlr4 -Dlanguage=CSharp -visitor grammar.g4`
3. Update visitor classes in corresponding parser project
4. Add test cases covering new syntax

## Common Pitfalls
- SPARQL injection: no built-in protection currently
- Stratification required for Datalog with negation
- Only OWL 2 DL subset supported (no annotation axioms)

## Code Navigation Tips
- **Graph implementation**: src/Api/Graph.cs
- **SPARQL query evaluation**: src/Api/SparqlQueryEvaluator.cs
- **Datalog engine**: src/Api/Datalog/
- **Parser visitors**: src/*/Visitor.cs files

## Supported Features
### RDF/Turtle
- ✅ Turtle 1.2
- ✅ TriG (named graphs)
- ✅ Basic triple patterns

### SPARQL
- ✅ SELECT queries
- ✅ Basic graph patterns
- ✅ Aggregate functions (recent addition)
- ❌ CONSTRUCT/ASK/DESCRIBE
- ❌ FILTER expressions
- ❌ OPTIONAL patterns

### OWL
- ✅ Manchester syntax
- ✅ OWL 2 DL subset
- ✅ OWL 2 RL reasoning
- ❌ Annotation axioms

### Datalog
- ✅ Stratifiable programs
- ✅ Negation (with stratification)
- ✅ Recursion
- ❌ Built-in functions beyond triples
