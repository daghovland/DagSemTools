# Architecture Overview

## Solution Structure
```
DagSemTools/
├── src/
│   ├── Api/              # Core RDF graph, reasoning engine
│   ├── Parser/           # Base parser utilities
│   ├── Turtle.Parser/    # Turtle/TriG parser
│   ├── Manchester.Parser/# OWL Manchester syntax parser
│   ├── Sparql.Parser/    # SPARQL query parser
│   └── Datalog.Parser/   # Datalog rules parser
├── test/                 # Unit and integration tests
└── grammars/            # ANTLR grammar definitions
```

## Key Entry Points
- **RDF Loading**: `TriGParser.Parse()` in Turtle.Parser
- **SPARQL Queries**: `graph.AnswerSelectQuery()` in Api
- **Reasoning**: `Ontology.create().GetAxiomRules()` in Api
- **Datalog**: `graph.LoadDatalog()` in Api

## Parser Architecture
All parsers use ANTLR4:
1. Grammar file (.g4) in grammars/
2. Generated parser/lexer classes
3. Visitor pattern for AST traversal
4. Output: semantic objects in Api namespace

## Data Flow
```
Input File (.ttl/.owl/.sparql)
    ↓
ANTLR Parser (grammars/)
    ↓
Visitor Pattern (src/*Parser/)
    ↓
Semantic Objects (src/Api/)
    ↓
Graph/Reasoning Engine
```

## Reasoning Layers
1. **RDF Graph**: Base triple store with indexing
2. **Datalog Engine**: Stratified evaluation with negation support
3. **OWL 2 RL**: Compiled to Datalog rules
4. **SPARQL**: Query evaluation over materialized graph

## Test Strategy
- **Unit Tests**: Individual parser components, grammar rules
- **Integration Tests**: Large real-world ontologies (IMF, GO, LIS-14)
- **Compliance**: W3C specification test suites where applicable
