using System.Xml.Linq;
using System.Xml.XPath;
using Serilog;

namespace ScorecardGenerator.Checks.ProperDockerfile;

public class Check : BaseCheck
{
    public Check(ILogger logger) : base(logger)
    {
    }

    protected override IList<Deduction> Run(string absolutePathToProjectFile)
    {
        var dockerfile = Path.Join(Path.GetDirectoryName(absolutePathToProjectFile), "Dockerfile");
        if (!File.Exists(dockerfile))
        {
            return new List<Deduction> {Deduction.CreateDisqualification(Logger, "No Dockerfile found at {Location}", dockerfile)};
        }
        
        var dockerfileContent = File.ReadAllText(dockerfile);
        if (!dockerfileContent.Contains("dotnet build") && !dockerfileContent.Contains("dotnet publish"))
        {
            return new List<Deduction> {Deduction.Create(Logger, 100, "Dockerfile at {Location} does not contain 'dotnet build' or 'dotnet publish'", dockerfile)};
        }
        
        if (!dockerfileContent.Contains("dotnet sonarscanner"))
        {
            return new List<Deduction> {Deduction.Create(Logger, 50, "Dockerfile at {Location} doesn't contain 'dotnet sonarscanner'", dockerfile)};
        }
        
        if (dockerfileContent.Contains("--version"))
        {
            return new List<Deduction> {Deduction.Create(Logger, 10, "Dockerfile at {Location} contains pinned version that won't be caught by Renovate", dockerfile)};
        }

        return new List<Deduction>();
    }
}