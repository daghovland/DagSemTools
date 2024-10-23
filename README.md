# Dag's Semantic Technology Tools
A collection of tools for using Rdf, Owl and semantic technology in dotnet. 

Currently it only includes a Turtle parser, OWL Manchester syntax parser. A reasoner for acyclic alc ontologies (imported from Manchester)  and semi-positive datalog programs (over the imported Turtle)
Sparql is not supported, but single triple-pattern queries over the data are possible.

## Supported language
* OWL 2 Manchester Syntax. Only the OWL 2 DL subset is supported. Especially, axioms on annotations are not allowed (even though the manchester syntax allows them).

* Rdf-1.2 Turtle

* Semi-positive datalog over Rdf. Only triples (and restricted negation) is allowed, no other functions.

## Usage
See examples in [test/NugetTest](test/NugetTest)
