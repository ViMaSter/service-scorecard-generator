using System.Xml.Linq;
using System.Xml.XPath;
using Serilog;

namespace ScorecardGenerator.Checks.NullableSetup;

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
        var nullable = csproj.XPathSelectElement("/Project/PropertyGroup/Nullable")?.Value;
        if (string.IsNullOrEmpty(nullable))
        {
            Logger.Warning("No <Nullable> element found in {CsProj}", csprojFiles.First());
            return 0;
        }

        const string expectedValue = "enable";
        if (nullable.ToLower() != "enable")
        {
            Logger.Information("Expected: <Nullable> should be set to '{Expected}'. Actual: '{Actual}'", expectedValue, nullable);
            return 0;
        }

        return 100;
    }
}