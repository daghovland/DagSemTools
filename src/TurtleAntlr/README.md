# RDF-1.2 Turtle Parser

This is a parser for the RDF-1.2 Turtle syntax, as defined in the [RDF 1,2 Turtle specification](https://www.w3.org/TR/rdf12-turtle/).

There is one major intended deviation from the standard: Only IRIs starting with http or https are allowed for absolute IRIs. 
This is a recommendation in the standard, but not a requirement. However, in all my usage this restriction has been useful.

The grammar files are almost verbatim taken from the spec. All mistakes in the translation to Antlr are mine. 


Copyright (C) 2024 Dag Hovland

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.

Contact: hovlanddag@gmail.com
