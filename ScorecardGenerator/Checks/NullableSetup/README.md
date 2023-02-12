# NullableSetup

## About
This check returns **100 points when [opting in to the nullable context](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references#nullable-contexts)**.  
Values other than `enable` points result in **0 points**.

## How to achieve 100 points
The scanned project file contains `<Nullable>enable</Nullable>`.

## Why this check exists
The nullable context guards against `System.NullReferenceException` at compile-time, rather than run-time.
Whenever possible, errors, and issues should [shift left](https://en.wikipedia.org/wiki/Shift-left_testing).