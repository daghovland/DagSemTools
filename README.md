# Dag's Semantic Technology Tools
A very incomplete collection of tools for using Rdf, Owl and semantic technology in dotnet. 

Currently it includes a Turtle parser, OWL Manchester syntax parser, a datalog engine over rdf, and a reasoner for acyclic ALC ontologies (imported from Manchester) and stratifiable datalog programs (over the imported Turtle)
Sparql is not supported, but single triple-pattern queries over the data are possible.

## Supported language
* OWL 2 Manchester Syntax. Only the OWL 2 DL subset is supported. Especially, axioms on annotations are not allowed (even though the manchester syntax allows them).

* Rdf-1.2 Turtle

* Stratifiable datalog over Rdf. Only triples (and restricted negation) is allowed, no other functions.

## Usage
See examples in [test/NugetTest](test/NugetTest)
