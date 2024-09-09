# RDF-1.2 Turtle Parser

This is a parser for the RDF-1.2 Turtle syntax, as defined in the [RDF 1,2 Turtle specification](https://www.w3.org/TR/rdf12-turtle/).

There is one major intended deviation from the standard: Only IRIs starting with http or https are allowed for absolute IRIs. 
This is a recommendation in the standard, but not a requirement. However, in all my usage this restriction has been useful.

The grammar files are almost verbatim taken from the spec. All mistakes in the translation to Antlr are mine. 