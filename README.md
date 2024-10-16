# Dag's Semantic Technology Tools
A Turtle parser, OWL Manchester syntax parser. A reasoner for acyclic alc ontologies (imported from Manchester)  and semi-positive datalog programs (over the imported Turtle)

## Supported language
* OWL 2 Manchester Syntax. Only the OWL 2 DL subset is supported. Especially, axioms on annotations are not allowed (even though the manchester syntax allows them).

* Rdf-1.2 Turtle

* Semi-positive datalog over Rdf. Only triples (and restricted negation) is allowed, no other functions.

## Usage
See examples in [test/NugetTest](test/NugetTest)
