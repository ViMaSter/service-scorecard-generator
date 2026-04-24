# CodeOwners

## About
This check returns **100 points** if 2 or more unique owners are defined in the `CODEOWNERS` file at the repository root.  
**50 points** are awarded if exactly 1 owner is defined.  
**0 points** if no owners are defined or no `CODEOWNERS` file exists.

## How to achieve 100 points
Create a `CODEOWNERS` file in the root of the repository with at least 2 distinct owners.

## Why this check exists
A `CODEOWNERS` file ensures that pull requests are reviewed by the right people.  
Having at least 2 owners reduces the bus factor and ensures code review coverage when one owner is unavailable.
