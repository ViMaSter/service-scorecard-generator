# LatestNET

## About
This check returns **100 points, if the scanned service runs on [the latest supported .NET version](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)**.  
Any major version below the latest supported version will deduct **10 points**.  
Using .NET Framework will deduct 100 points. 

## How to achieve 100 points
The scanned `.csproj` needs an `<TargetFramework>` element set to the latest .NET framework major version.

## Why this check exists
Keeping up-to-date with releases is important for security and performance reasons.  
Less supported runtimes per language also means less effort for maintenance and less risk of bugs, ultimately allowing more time for new features.