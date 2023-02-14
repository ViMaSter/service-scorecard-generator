using System.Xml.Linq;
using System.Xml.XPath;
using Serilog;

namespace ScorecardGenerator.Checks.BuiltForAKS;

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
        var csproj = XDocument.Load(csprojFiles.First());
        var usedSDK = csproj.Root?.Attribute("Sdk")?.Value;
        if (string.IsNullOrEmpty(usedSDK))
        {
            return new List<Deduction> { Deduction.CreateDisqualification(Logger, "No Sdk attribute found in {CsProj}", csprojFiles.First()) };
        }
        
        if (usedSDK != "Microsoft.NET.Sdk.Web")
        {
            return new List<Deduction> { Deduction.CreateDisqualification(Logger, "Only projects using 'Microsoft.NET.Sdk.Web' are considered deployable") };
        }
        
        var pipelineFiles = Directory.GetFiles(absolutePathToServiceRoot, "*.yml", SearchOption.TopDirectoryOnly);
        if (!pipelineFiles.Any())
        {
            return new List<Deduction>(new[] { Deduction.Create(Logger, 100, "No .yml file found inside {ServiceRoot}", relativePathToServiceRoot) });
        }

        var firstPath = pipelineFiles.First();
        if (pipelineFiles.Length > 1)
        {
            return pipelineFiles.Select(path => Deduction.Create(Logger, 5, "More than one pipeline file: {Path}", path)).ToList();
        }

        if (firstPath.Contains("build-"))
        {
            return new List<Deduction>();    
        }

        if (firstPath.Contains("azure-"))
        {
            return new List<Deduction>();    
        }

        if (firstPath.Contains("onprem-"))
        {
            return new List<Deduction>(new[] { Deduction.Create(Logger, 100, "Service pipeline file doesn't start with 'azure-'; actual: {Actual}", firstPath) });
        }

        return new List<Deduction> { Deduction.Create(Logger, 5, ".yml needs to start with either 'azure-' for services or 'build-' for libraries; actual: '{Path}'", firstPath) };
    }
}
