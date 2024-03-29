# PendingRenovateAzurePRs

## About
This check represents outdated package inside the scanned project.

> **Warning**  
> This check asserts **unique filenames of projects inside the repository**.  
> If you have two projects named `Service.csproj` in different directories, this check will yield conflicting results.

> **Warning**  
> When using this check, run the tool with an additional argument: `--azure-pat={value}`.  
> Replace `{value}` with a [Personal Access Token](https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops) with access to the repositories you want to scan.


The scanned project starts at **100 points**.  
The check looks for open Pull Requests on Azure DevOps that include the scanned project file and relate to [Renovate](https://github.com/renovatebot/renovate).  
Each open Pull Request represents a not-yet integrated package update and results in a **20 point deduction**.

## How to achieve 100 points
Resolve all open Pull Requests on Azure DevOps that include the scanned project file and relate to [Renovate](https://github.com/renovatebot/renovate).

## Why this check exists
The nullable context guards against `System.NullReferenceException` at compile-time, rather than run-time.  
Whenever possible, errors, and issues should [shift left](https://en.wikipedia.org/wiki/Shift-left_testing).