# Claude Code Development Guide

## Project Summary
DagSemTools is a .NET library for semantic web technologies (RDF, OWL, SPARQL, Datalog). All parsers are built using ANTLR4 grammars and must conform strictly to W3C specifications.

## Quick Reference

### Build & Test
```bash
dotnet build                    # Build solution
dotnet test                     # Run all tests
dotnet format                   # Format code
```

### Project Structure
- `src/Api/` - Core RDF graph, reasoning engine, query evaluator
- `src/*.Parser/` - ANTLR-based parsers (Turtle, Manchester, SPARQL, Datalog)
- `grammars/` - ANTLR grammar definitions (.g4 files)
- `test/` - Unit and integration tests

### Key Files
- Graph implementation: `src/Api/Graph.cs`
- SPARQL evaluation: `src/Api/SparqlQueryEvaluator.cs`
- Datalog engine: `src/Api/Datalog/`
- Parser visitors: `src/*/Visitor.cs`

## Development Workflow

### When Modifying Parsers
1. Update ANTLR grammar in `grammars/*.g4`
2. Regenerate parser: `antlr4 -Dlanguage=CSharp -visitor <grammar>.g4`
3. Update visitor class in corresponding `src/*.Parser/` project
4. Add test cases in `test/*.Tests/`
5. Run `dotnet test` to verify

### When Adding Features
1. Check W3C specification for correctness
2. Add unit tests first (TDD approach)
3. Implement in appropriate layer (parser → API → query evaluator)
4. Test with real-world ontologies (IMF, Gene Ontology, LIS-14)

### When Fixing Bugs
1. Check if it's a grammar issue or visitor implementation
2. Add regression test reproducing the bug
3. Fix and verify all tests pass

## Important Constraints

### Supported Features
- ✅ Turtle 1.2, TriG
- ✅ SPARQL SELECT with basic graph patterns and aggregates
- ✅ OWL 2 Manchester syntax (DL subset only)
- ✅ Stratifiable Datalog with negation
- ✅ OWL 2 RL reasoning

### Not Supported
- ❌ SPARQL CONSTRUCT/ASK/DESCRIBE, FILTER, OPTIONAL
- ❌ OWL annotation axioms
- ❌ Datalog built-in functions (only triples)
- ❌ SPARQL injection protection

## Testing Requirements
- All parser changes need tests
- Integration tests require external data (see BUILDING.md)
- Test data setup:
  ```bash
  curl -o test/Api.Tests/TestData/imf.ttl http://ns.imfid.org/20240531/imf-ontology.owl.ttl
  curl -o test/Api.Tests/TestData/go.owl.xml http://current.geneontology.org/ontology/go.owl
  curl -o test/Api.Tests/TestData/LIS-14.ttl https://rds.posccaesar.org/ontology/lis14/ont/core/4.0/LIS-14.ttl
  riot --output=TURTLE test/Api.Tests/TestData/go.owl.xml > test/Api.Tests/TestData/go.ttl
  ```

## Common Patterns

### Loading RDF
```csharp
var file = new FileInfo("graph.ttl");
var graph = TriGParser.Parse(file, Console.Error).GetDefaultGraph();
```

### Running SPARQL Queries
```csharp
var results = graph.AnswerSelectQuery("SELECT * WHERE {?s ?p ?o}");
// Returns IEnumerable<Dictionary<string, GraphElement>>
```

### Loading Datalog Rules
```csharp
var datalogFile = new FileInfo("rules.datalog");
graph.LoadDatalog(datalogFile);
```

### OWL Reasoning
```csharp
var ontology = Ontology.create(ontologyGraph);
graph.LoadDatalog(ontology.GetAxiomRules());
```

## Code Conventions
- Follow existing naming patterns in codebase
- Parser visitors should handle all grammar rules
- Error messages should reference W3C spec sections when applicable
- Use `Console.Error` for parser warnings/errors

## Current Development Focus
- Expanding SPARQL query support (see `story/sparql-tests` branch)
- Aggregate functions recently added
- Parser compliance with W3C specifications

## Getting Help
- See CONTRIBUTING.md for contribution guidelines
- See BUILDING.md for build/test setup details
- See ARCHITECTURE.md for system design overview
- Check W3C specs: https://www.w3.org/TR/ (Turtle, SPARQL, OWL2)
