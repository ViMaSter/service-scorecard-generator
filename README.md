# Service Scorecard Generator [![codecov](https://codecov.io/gh/ViMaSter/service-scorecard-generator/branch/main/graph/badge.svg?token=T7ESI3L6ZN)](https://codecov.io/gh/ViMaSter/service-scorecard-generator)

Service Scorecard Generator is a dotnet app that generates Azure Wiki-friendly markdown files with scores for dotnet projects.  
![image](https://user-images.githubusercontent.com/1689033/218286805-7acdd1c5-e2be-4d69-92fb-8081f9e1d0a2.png)
For more details on individual checks and their scoring, take a look at the `README.md` files inside the subdirectories of `/Checks`.

## Usage

```bash

# clone the sources
git clone git@github.com:ViMaSter/code-scanning-scoreboard.git /scoreboard-generator

# build the project
cd scoreboard-generator
dotnet build

# this assumes your various .csproj files reside inside subdirectories of `/sources`
cd /sources

# run it with an Azure Personal Access Token to scan for open Renovate Pull Requests and output path
# https://github.com/renovatebot/renovate
# https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=Windows
/scoreboard-generator/bin/Release/net7.0/ScorecardGenerator --azure-pat <azure-pat> --output-path /wiki
```

## License

[GNU General Public License v3.0](https://choosealicense.com/licenses/gpl-3.0/)
