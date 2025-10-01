# Building and Testing
The github workflow file [../](.github/workflows/shared_build_workflow.yml) contains all commands needed to build and test the project on ubuntu, and
is always up to date. The instructions below are a bit more verbose, and may be out of date.
## Prerequisites
* DotNet v9.0 SDK or later

## Building
Building is just plain `dotnet build`, unless you want to build for the Release configuration. See the github workflow file for details.

## Testing
### Prerequisites
* Java v17 or later 
* Apache Jena commandline tools, spefically riot

Download the external test data (if not already done) (These commands assume curl. Any other download method is fine):
```bash
curl -o test/Api.Tests/TestData/imf.ttl http://ns.imfid.org/20240531/imf-ontology.owl.ttl 
curl -o test/Api.Tests/TestData/go.owl.xml http://current.geneontology.org/ontology/go.owl 
curl -o "test/Api.Tests/TestData/LIS-14.ttl" https://rds.posccaesar.org/ontology/lis14/ont/core/4.0/LIS-14.ttl 
```
Translate the gene ontology to Turtle
```bash
riot --output=TURTLE test/Api.Tests/TestData/go.owl.xml > test/Api.Tests/TestData/go.ttl
```
After this it is plain `dotnet test` to run all tests (again, unless you built for release, which uses Nuget packages and is different)