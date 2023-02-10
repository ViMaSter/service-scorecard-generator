# PendingRenovateAzurePRs

## About
This check represents outdated package inside the scanned project.
The scanned project starts at **100 points**.
The check looks for open Pull Requests on Azure DevOps that include the scanned project file and relate to [Renovate](https://github.com/renovatebot/renovate).
Each open Pull Request represents a not-yet integrated package update and results in a **20 point penalty**.

## How to achieve 100 points
Resolve all open Pull Requests on Azure DevOps that include the scanned project file and relate to [Renovate](https://github.com/renovatebot/renovate).

## Why this check exists
The nullable context guards against `System.NullReferenceException` at compile-time, rather than run-time.
Whenever possible, errors, and issues should [shift left](https://en.wikipedia.org/wiki/Shift-left_testing).