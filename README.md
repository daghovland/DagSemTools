# AlcTableau
A toy tableau reasoner for the ALC subset of OWL 2 DL

## Supported language
OWL 2 Manchester Syntax. Only the OWL 2 DL subset is supported. Especially, axioms on annotations are not allowed (even though the manchester syntax allows them).

## Usage
dotnet run -- -f <input_file> -q <query_file>
