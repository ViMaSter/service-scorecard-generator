# Service Scorecard Generator

[![NuGet](https://img.shields.io/nuget/v/ScorecardGenerator)](https://www.nuget.org/packages/ScorecardGenerator) [![codecov](https://codecov.io/gh/ViMaSter/service-scorecard-generator/branch/main/graph/badge.svg?token=T7ESI3L6ZN)](https://codecov.io/gh/ViMaSter/service-scorecard-generator) [![Build, Release, Publish](https://github.com/ViMaSter/service-scorecard-generator/actions/workflows/build-release-publish.yml/badge.svg)](https://github.com/ViMaSter/service-scorecard-generator/actions/workflows/build-release-publish.yml)

---

Service Scorecard Generator is a dotnet app that generates Azure Wiki-friendly markdown files with scores for dotnet projects.  
![image](https://user-images.githubusercontent.com/1689033/218286805-7acdd1c5-e2be-4d69-92fb-8081f9e1d0a2.png)  
For more details on individual checks and their scoring, take a look at the `README.md` files inside the subdirectories of `/Checks`.

## Usage

```bash
dotnet tool install --global ScorecardGenerator

# this assumes you want to scan all .csproj files residing inside `/sources` and subdirectories thereof
cd /sources

# this assumes you want to output the markdown files to `/wiki` and use the default configuration
ScorecardGenerator --output-path /wiki --visualizer azurewiki
```

## Configuration
The Service Scorecard Generator stores and loads configuration from a `scorecard.config.json` file in the current working directory.
If the file is missing when running, it will be created [with default values](https://github.com/ViMaSter/service-scorecard-generator/blob/main/ScorecardGenerator/Configuration/default.json) before the tool starts.

- 


## Interactive tables
Due to limted HTML support, the table itself can't be made interactive. More info and a workaround can be found [in the wiki](https://github.com/ViMaSter/service-scorecard-generator/wiki/Interactive-table).
![video of how to sort and filter the table](https://user-images.githubusercontent.com/1689033/221364567-c5ca0a3d-9e00-4730-b33e-052cfc1aa521.gif)

## License

[GNU General Public License v3.0](https://choosealicense.com/licenses/gpl-3.0/)
