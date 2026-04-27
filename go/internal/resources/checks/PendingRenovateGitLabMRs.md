# PendingRenovateGitLabMRs

## About
This check represents open Renovate merge requests in the scanned GitLab repository.

> **Warning**  
> When using this check, run the tool with an additional argument: `--pat={value}`.  
> Replace `{value}` with a GitLab Personal Access Token that can read merge requests in the repositories you want to scan.

The scanned repository starts at **100 points**.  
The check looks for open GitLab merge requests that relate to [Renovate](https://github.com/renovatebot/renovate).  
Each open merge request represents a not-yet integrated package update and results in a **20 point deduction**.

## How to achieve 100 points
Resolve all open Renovate merge requests in GitLab for the scanned repository.

## Why this check exists
Integrating dependency updates quickly reduces the time your services spend on outdated packages and shortens the window for known vulnerabilities.