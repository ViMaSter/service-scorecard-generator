# SonarQubeCoverage

## About
This check uses the coverage percentage of SonarQube as points.  
An unavailable coverage report or unexpected response from SonarQube deducts **100 points**.

The project key used consists of the last two three names joined by `.`.  
Example:
 - Absolute path to project file: `/absolutePathTo/repository/src/ScorecardGenerator/project.csproj`
 - Project key: `repository.src.ScorecardGenerator`

## How to achieve 100 points
Achieve 100% code coverage.

## Why this check exists
Code Coverage ensures that covered code runs and behaves as expected.  
To adjust to business requirements developers change code. Executing all parts of the code as part of deployments, ensures these changes introduce the new behavior without introducing unintended side effects.