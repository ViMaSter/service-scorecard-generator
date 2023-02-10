# HasNET7

## About
This check returns **100 points, if the scanned service runs on NET7**.

## How to achieve 100 points
The scanned `.csproj` needs to contain `<TargetFramework>net7</TargetFramework>`.

## Why this check exists
Keeping up-to-date with releases is important for security and performance reasons.  
Less differences between services also means less effort for maintenance and less risk of bugs, ultimately allowing more time for new features.