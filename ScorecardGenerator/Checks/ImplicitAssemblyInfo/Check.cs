using System.Xml.Linq;
using System.Xml.XPath;
using Serilog;

namespace ScorecardGenerator.Checks.ImplicitAssemblyInfo;

public class Check : BaseCheck
{
    private readonly IList<string> _requiredProperties = new List<string>
    {
        "Company",
        "Copyright",
        "Description",
        "FileVersion",
        "InformalVersion",
        "Product",
        "UserSecretsId"
    };
    
    public Check(ILogger logger) : base(logger)
    {
    }

    protected override IList<Deduction> Run(string absolutePathToProjectFile)
    {
        var csproj = XDocument.Load(absolutePathToProjectFile);

        var deductions = _requiredProperties
            .ToDictionary(propertyName => propertyName, propertyName => csproj.XPathSelectElement($"/Project/PropertyGroup/{propertyName}"))
            .Where(valueByPropertyName => valueByPropertyName.Value == null)
            .Select(valueByPropertyName => Deduction.Create(Logger, 20, "No <{ElementName}> element found in {CsProj}", valueByPropertyName.Key, absolutePathToProjectFile))
            .ToList();

        var generateAssemblyInfo = csproj.XPathSelectElement("/Project/PropertyGroup/GenerateAssemblyInfo")?.Value;
        if (string.IsNullOrEmpty(generateAssemblyInfo))
        {
            deductions.Add(Deduction.Create(Logger, 100, "No <GenerateAssemblyInfo> element found in {CsProj}", absolutePathToProjectFile));
        }
        else
        {
            const string expectedValue = "true";
            if (generateAssemblyInfo.ToLower() != expectedValue)
            {
                deductions.Add(Deduction.Create(Logger, 100, "Expected: <GenerateAssemblyInfo> should contain '{Expected}'. Actual: '{Actual}'", expectedValue, generateAssemblyInfo));
            }
        }

        return deductions;
    }
}