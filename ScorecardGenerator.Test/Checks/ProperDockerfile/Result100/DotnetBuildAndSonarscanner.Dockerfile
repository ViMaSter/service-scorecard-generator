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
RUN dotnet tool install --global dotnet-sonarscanner
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet sonarscanner begin /k:"ScorecardGenerator" /d:sonar.host.url="http://sonarqube:9000" /d:sonar.login="admin" /d:sonar.password="admin"
RUN dotnet build "CoolService.csproj" -c Release -o /app/build
RUN dotnet sonarscanner end /d:sonar.login="admin" /d:sonar.password="admin"

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoolService.dll"]
