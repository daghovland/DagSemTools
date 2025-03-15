# Dag's Semantic Technology Tools
A very incomplete collection of tools for using Rdf, Owl and semantic technology in dotnet. 

Currently it includes a Turtle parser, OWL Manchester syntax parser, a datalog engine over Rdf, a reasoner for acyclic ALC ontologies (imported from Manchester), stratifiable datalog programs (over the imported Turtle), and OWL 2 RL reasoning over the imported Rdf.
Sparql is not supported, but single triple-pattern queries over the data are possible.

## Supported language
* OWL 2 Manchester Syntax. Only the OWL 2 DL subset is supported. Especially, axioms on annotations are not allowed (even though the manchester syntax allows them).

* Rdf-1.2 Turtle

* Stratifiable datalog over Rdf. Only triples (and restricted negation) is allowed, no other functions.

## Usage
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
See examples in [test/NugetTest](test/NugetTest)

## Copyright
Dag Hovland 2024,2025. 

# Gaza
This software is written at a time when the people of Gaza are the victims of an horrible and very asymmetric warfare, perhaps even genocide. Users and contributors are encouraged to find ways to support the people of Gaza.
