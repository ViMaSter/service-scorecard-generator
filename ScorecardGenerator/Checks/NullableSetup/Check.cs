using System.Xml.Linq;
using System.Xml.XPath;
using Serilog;

namespace ScorecardGenerator.Checks.NullableSetup;

public class Check : BaseCheck
{
    public Check(ILogger logger) : base(logger)
    {
    }

    protected override IList<Deduction> Run(string absolutePathToProjectFile)
    {
        var csproj = XDocument.Load(absolutePathToProjectFile);
        var nullable = csproj.XPathSelectElement("/Project/PropertyGroup/Nullable")?.Value;
        if (string.IsNullOrEmpty(nullable))
        {
            return new List<Deduction> { Deduction.Create(Logger, 100, "No <Nullable> element found in {CsProj}", absolutePathToProjectFile) };
        }

        const string expectedValue = "enable";
        if (nullable.ToLower() != "enable")
        {
            return new List<Deduction> { Deduction.Create(Logger, 100, "Expected: <Nullable> should contain '{Expected}'. Actual: '{Actual}'", expectedValue, nullable) };
        }

        return new List<Deduction>();
    }
}