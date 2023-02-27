# Service Scorecard Generator

[![codecov](https://codecov.io/gh/ViMaSter/service-scorecard-generator/branch/main/graph/badge.svg?token=T7ESI3L6ZN)](https://codecov.io/gh/ViMaSter/service-scorecard-generator) [![Build (and Release)](https://github.com/ViMaSter/service-scorecard-generator/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/ViMaSter/service-scorecard-generator/actions/workflows/build-and-release.yml)

Service Scorecard Generator is a dotnet app that generates Azure Wiki-friendly markdown files with scores for dotnet projects.  
![image](https://user-images.githubusercontent.com/1689033/218286805-7acdd1c5-e2be-4d69-92fb-8081f9e1d0a2.png)
For more details on individual checks and their scoring, take a look at the `README.md` files inside the subdirectories of `/Checks`.

## Usage

```bash

# clone the sources
git clone git@github.com:ViMaSter/service-scorecard-generator.git /service-scorecard-generator

# build the project
cd service-scorecard-generator
dotnet build

# this assumes your various .csproj files reside inside subdirectories of `/sources`
cd /sources

# run it with an Azure Personal Access Token to scan for open Renovate Pull Requests and output path
# https://github.com/renovatebot/renovate
# https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=Windows
/service-scorecard-generator/bin/Release/net7.0/ScorecardGenerator --azure-pat <azure-pat> --output-path /wiki
```

## Interactive tables
Due to limted HTML support, the table itself can't be made interactive. More info and a workaround can be found [in the wiki](https://github.com/ViMaSter/service-scorecard-generator/wiki/Interactive-table).
![video of how to sort and filter the table](https://user-images.githubusercontent.com/1689033/221364567-c5ca0a3d-9e00-4730-b33e-052cfc1aa521.gif)

## License

[GNU General Public License v3.0](https://choosealicense.com/licenses/gpl-3.0/)
