using System.Xml.Linq;
using System.Xml.XPath;
using Serilog;

namespace ScorecardGenerator.Checks.HasNET7;

internal class Check : BaseCheck
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
        var targetFramework = csproj.XPathSelectElement("/Project/PropertyGroup/TargetFramework")?.Value;
        if (string.IsNullOrEmpty(targetFramework))
        {
            return new List<Deduction> { Deduction.Create(Logger, 100, "No <TargetFramework> element found in {CsProj}", csprojFiles.First()) };
        }

        const string expectedValue = "net7";
        if (!targetFramework.StartsWith(expectedValue))
        {
            return new List<Deduction> { Deduction.Create(Logger, 100, "Expected: <TargetFramework> should contain '{Expected}'. Actual: '{Actual}'", expectedValue, targetFramework) };
        }

        return new List<Deduction>();
    }
}