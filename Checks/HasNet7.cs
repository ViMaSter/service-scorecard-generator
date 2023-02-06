using System.Xml.Linq;
using System.Xml.XPath;

namespace ScorecardGenerator.Checks;

internal class HasNet7 : IRunCheck
{
    public int Run(string serviceRootDirectory)
    {
        var csprojFiles = Directory.GetFiles(serviceRootDirectory, "*.csproj", SearchOption.TopDirectoryOnly);
        if (!csprojFiles.Any())
        {
            return 0;
        }
        var csproj = XDocument.Load(csprojFiles.First());
        var targetFramework = csproj.XPathSelectElement("/Project/PropertyGroup/TargetFramework")?.Value;
        if (string.IsNullOrEmpty(targetFramework))
        {
            return 0;
        }

        if (!targetFramework.StartsWith("net7"))
        {
            return 0;
        }

        return 100;
    }
}