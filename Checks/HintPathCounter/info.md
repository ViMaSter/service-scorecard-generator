# HintPathCounter

## About
This check returns **100 points by default**.  
**Each `<HintPath>`** in the scanned `.csproj` results in a **10 point penalty**.

## How to achieve 100 points
Clear any `<HintPath>` elements present in the scanned project file.

## Why this check exists
`<HintPath>` entries become relevant, if libraries can't resolve using the NuGet package registry.
Packages should resolve globally across local systems and build pipeline alike.