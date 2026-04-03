# ImplicitAssemblyInfo

## About
This check detects **[enabled `AssemblyInfo` attribute generation](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#generateassemblyinfo)** and that all required properties exist.  
Values other than `true` **reduce 100 points**.  
Existence of `Properties/AssemblyInfo.cs` **reduces 100 points**.  
Each required property missing inside the `.csproj` file **reduces 20 points**.

## How to achieve 100 points
The scanned project file contains `<GenerateAssemblyInfo>true</GenerateAssemblyInfo>` **and** the following XML elements inside a `<PropertyGroup>`:
- `<Company>`
- `<Copyright>`
- `<Description>`
- `<FileVersion>`
- `<InformalVersion>`
- `<Product>`
- `<UserSecretsId>`

To migrate the data from a `AssemblyInfo.cs` file to a `.csproj` file, use [this reference table of `Assembly attribute` to `MSBuild property`](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#generateassemblyinfo), ensure `<GenerateAssemblyInfo>true</GenerateAssemblyInfo>` exists in the project file and remove the `AssemblyInfo.cs` file.

## Why this check exists
### Why are these properties important?
Having exhaustive metadata like versioning info, names, and descriptions proves useful, when inspecting binaries after deployments to hosting environments or package registries.  

### Why should the properties be part of the project file?
The preceding properties are static and describe the Assembly, rather than affect its behavior.  
Having `.csproj` files contain static data and `.cs` files contain dynamic data allows for good separation of concerns.