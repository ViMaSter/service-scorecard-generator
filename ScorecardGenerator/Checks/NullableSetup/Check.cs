using System.Xml.Linq;
using System.Xml.XPath;
using Serilog;

namespace ScorecardGenerator.Checks.NullableSetup;

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
        var nullable = csproj.XPathSelectElement("/Project/PropertyGroup/Nullable")?.Value;
        if (string.IsNullOrEmpty(nullable))
        {
            return new List<Deduction> { Deduction.Create(Logger, 100, "No <Nullable> element found in {CsProj}", csprojFiles.First()) };
        }

        const string expectedValue = "enable";
        if (nullable.ToLower() != "enable")
        {
            return new List<Deduction> { Deduction.Create(Logger, 100, "Expected: <Nullable> should contain '{Expected}'. Actual: '{Actual}'", expectedValue, nullable) };
        }

        return new List<Deduction>();
    }
}