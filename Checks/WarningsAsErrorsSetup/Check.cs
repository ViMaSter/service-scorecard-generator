using System.Xml.Linq;
using System.Xml.XPath;
using Serilog;

namespace ScorecardGenerator.Checks.WarningsAsError;

internal class Check : BaseCheck
{
    public Check(ILogger logger) : base(logger)
    {
    }

    protected override int Run(string workingDirectory, string relativePathToServiceRoot)
    {
        var absolutePathToServiceRoot = Path.Join(workingDirectory, relativePathToServiceRoot);
        var csprojFiles = Directory.GetFiles(absolutePathToServiceRoot, "*.csproj", SearchOption.TopDirectoryOnly);
        if (!csprojFiles.Any())
        {
            Logger.Warning("No csproj file found at {Location}", absolutePathToServiceRoot);
            return 0;
        }
        var csproj = XDocument.Load(csprojFiles.First());
        var warningsAsErrors = csproj.XPathSelectElement("/Project/PropertyGroup/WarningsAsErrors")?.Value;
        if (string.IsNullOrEmpty(warningsAsErrors))
        {
            Logger.Warning("No <WarningsAsErrors> element found in {CsProj}", csprojFiles.First());
            return 0;
        }

        const string expectedValue = "true";
        if (warningsAsErrors.ToLower() != expectedValue)
        {
            Logger.Information("Expected: <WarningsAsErrors> should be set to '{Expected}'. Actual: '{Actual}'", expectedValue, warningsAsErrors);
            return 0;
        }

        return 100;
    }
}