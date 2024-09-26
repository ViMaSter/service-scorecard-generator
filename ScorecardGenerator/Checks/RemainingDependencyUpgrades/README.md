# RemainingDependencyUpgrades

## About
This check represents outdated package inside the scanned project.

> **Caution**  
> This check requires **unique project filenames**.  
> Having two projects named `Service.csproj` in different directories, yields conflicting results.

The scanned project starts at **100 points**.  
The check looks for open Pull Requests on either Azure DevOps or GitHub that include the scanned project file and relate to [Renovate](https://github.com/renovatebot/renovate) or [Dependabot](https://docs.github.com/en/code-security/dependabot).  
Each open Pull Request represents a not-yet integrated package update and results in a **20 point deduction**.

> **Information**  
> Set up Renovate or Dependabot for the affected repositories, then append one of these arguments:
> - For Renovate on Azure use `--azure-pat={value}`    
> - For Dependabot on GitHub use `--github-pat={value}`
>
> Replace `{value}` with an [Azure](https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops) or [GitHub](https://docs.github.com/en/rest/overview/authenticating-to-the-rest-api?apiVersion=2022-11-28#authenticating-with-a-personal-access-token) Personal Access Token with access to the repositories you want to scan.

## How to achieve 100 points
Resolve all open Pull Requests on Azure DevOps that include the scanned project file and relate to [Renovate](https://github.com/renovatebot/renovate).

## Why this check exists
The nullable context guards against `System.NullReferenceException` at compile-time, rather than run-time.  
Whenever possible, errors, and issues should [shift left](https://en.wikipedia.org/wiki/Shift-left_testing).