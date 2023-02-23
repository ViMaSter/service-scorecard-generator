using System.Xml.Linq;
using System.Xml.XPath;
using Serilog;

namespace ScorecardGenerator.Checks.ProperDockerfile;

public class Check : BaseCheck
{
    public Check(ILogger logger) : base(logger)
    {
    }

    protected override IList<Deduction> Run(string workingDirectory, string relativePathToServiceRoot)
    {
        var absolutePathToServiceRoot = Path.Join(workingDirectory, relativePathToServiceRoot);
        var csprojFiles = Directory.GetFiles(absolutePathToServiceRoot, "*.csproj", SearchOption.TopDirectoryOnly);
        if (!csprojFiles.Any())
        {
            return new List<Deduction> {Deduction.Create(Logger, 100, "No csproj file found at {Location}", absolutePathToServiceRoot)};
        }

        var dockerfile = Path.Join(Path.GetDirectoryName(csprojFiles.First()) ?? string.Empty, "Dockerfile");
        if (!File.Exists(dockerfile))
        {
            return new List<Deduction> {Deduction.CreateDisqualification(Logger, "No Dockerfile found at {Location}", dockerfile)};
        }
        
        var dockerfileContent = File.ReadAllText(dockerfile);
        if (!dockerfileContent.Contains("dotnet build"))
        {
            return new List<Deduction> {Deduction.Create(Logger, 100, "Dockerfile at {Location} does not contain 'dotnet build'", dockerfile)};
        }
        
        if (!dockerfileContent.Contains("dotnet sonarscanner"))
        {
            return new List<Deduction> {Deduction.Create(Logger, 50, "Dockerfile at {Location} contains 'sonarqube'", dockerfile)};
        }

        return new List<Deduction>();
    }
}