using System.Xml.Linq;
using System.Xml.XPath;
using Serilog;

namespace ScorecardGenerator.Checks.HasNET7;

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
        var targetFramework = csproj.XPathSelectElement("/Project/PropertyGroup/TargetFramework")?.Value;
        if (string.IsNullOrEmpty(targetFramework))
        {
            Logger.Warning("No <TargetFramework> element found in {CsProj}", csprojFiles.First());
            return 0;
        }

        if (!targetFramework.StartsWith("net7"))
        {
            Logger.Information("Expected: <TargetFramework> should contain 'net7'. Actual: {Content}", targetFramework);
            return 0;
        }

        return 100;
    }
}