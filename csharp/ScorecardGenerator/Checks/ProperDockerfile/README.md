# ProperDockerfile

## About
This check returns **100 points** when a `Dockerfile` next to the `.csproj` exists and contains `dotnet build`.  
A `Dockerfile` that doesn't contain `dotnet sonarscanner` results in a **50 point deduction**.  
A `Dockerfile` that contains neither `dotnet build` nor `dotnet publish` results in a **100 point deduction**.  
The Scorecard **skips this check** if no `Dockerfile` exists next to the `.csproj` file.

## How to achieve 100 points
Ensure a `Dockerfile` exists in the same directory as the `.csproj` file.

## Why this check exists
Various forms of `Dockerfile`s to generate executables exist.  
This check asserts various qualities:

### Build an executable and not copy it from another location
This ensures:
 - executables generated locally match the ones running in generated images inside the Kubernetes cluster
 - the project compiles across platforms
 - local and CI/CD environments can run all build steps
 
### Run code analysis
This allows local runs of code analysis
