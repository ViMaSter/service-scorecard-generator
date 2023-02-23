FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY ["CoolService.csproj", "CoolService/"]
RUN dotnet restore "CoolService.csproj"
COPY . .
WORKDIR "CoolService"
RUN dotnet build "CoolService.csproj" -c Release -o /app/build
    
FROM build AS test
WORKDIR "CoolService"
RUN dotnet test "CoolService.csproj" -c Release -o /app/test

FROM build AS publish
RUN dotnet build "CoolService.csproj" -c Release -o /app/build
    
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoolService.dll"]
