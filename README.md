# Dag's Semantic Technology Tools
A very incomplete collection of tools for using Rdf, Owl and semantic technology in dotnet. 

Currently it includes a Turtle parser, OWL Manchester syntax parser, a datalog engine over Rdf, a reasoner for acyclic ALC ontologies (imported from Manchester), stratifiable datalog programs (over the imported Turtle), and OWL 2 RL reasoning over the imported Rdf.
Sparql with simple selects over basic graph patterns is supported.

## Supported language
* OWL 2 Manchester Syntax. Only the OWL 2 DL subset is supported. Especially, axioms on annotations are not allowed (even though the manchester syntax allows them).

* Rdf-1.2 Turtle

* Stratifiable datalog over Rdf. Only triples (and restricted negation) is allowed, no other functions.

* Sparql 1.2. Only simple selects over basic graph patterns are supported.

## Usage

### Rdf
Install the nuget package [DagSemTools.Api](https://www.nuget.org/packages/DagSemTools.Api/), f.ex. by `dotnet add package DagSemTools.Api`

To load an rdf graph in turtle format, try f.ex.
```csharp
var file = new FileInfo("graph.ttl");
var graph = TurtleParser.Parse(file, Console.Error);
```

To get answers to single basic graph patterns, use functions on the graph, like this:
```csharp
var tripleAnswers = graph.GetTriplesWithPredicate(new IriReference("https://exampe.com/some/predicate"));
```

### SPARQL
Continuing the example above, you can also run simple SPARQL select queries like this:
```csharp
var sparqlAnswerMap = graph.AnswerSelectQuery("SELECT * WHERE where{?s <https://example.com/some/predicate> ?o.}");
```
The output (called `sparqlAnswerMap` above) is an `IEnumerable<Dictionary<string, GraphElement>>`. Each dictonary maps variable names to values.
Only the basic syntax as shown in this example is supported.
There is no built-in protection against SPARQL injection.

### OWL RL Reasoning
To load an ontology and use that to reason over the data, first load it as rdf (as above) 
and then parse the rdf into an ontology with `Ontology.create`, extract it as datalog rules with `GetAxiomRules`, 
like this:
```csharp
var ontology_file = new FileInfo("graph.ttl");
var ontology_graph = TurtleParser.Parse(ontology_file, Console.Error);
graph.LoadDatalog(Ontology.create(ontology_graph).GetAxiomRules());
```
This materializes the new answers which can be fetched as before: 
```csharp
var tripleAnswersWithReasoning = graph.GetTriplesWithPredicate(new IriReference("https://exampe.com/some/predicate"));
```
### Datalog
Datalog with negation (and recursion) is supported, with the normal restriction on stratification when negation and recursion are combined. No extensions are implemented.
Triples are written `ex:predicate[?s, ex:object]` or `[?s, ex:predicate, ex:object]` in the datalog rules. Negation is written with `not`, like `not ex:predicate[?s, ex:object]`.
Example rules:
```prolog
prefix ex: <https://example.com/data#>

[?s, ex:predicate, ex:object2] :- ex:predicate[?s, ex:object].
```

Rules can be loaded directly from a file like this:
```csharp
var datalog_file = new FileInfo("rules.datalog");
graph.LoadDatalog(datalogfile);
```
After loading, the new inferred answers can be fetched as above.

See examples in [test/NugetTest](test/NugetTest)

## Contributing and/or Building from source
See [CONTRIBUTING.md](CONTRIBUTING.md) and [BUILDING.md](BUILDING.md)

## Copyright
Dag Hovland 2024,2025. 

# Gaza
This software is written at a time when the people of Gaza are the victims of an horrible and very asymmetric warfare, perhaps even genocide. Users and contributors are encouraged to find ways to support the people of Gaza.
