using System.Xml.Linq;
using Serilog;

namespace ScorecardGenerator.Checks.HintPathCounter;

internal class Check : BaseCheck
{
    public Check(ILogger logger) : base(logger)
    {
    }
    
    const int DeductionPerHintPath = 10;

    protected override List<Deduction> Run(string workingDirectory, string relativePathToServiceRoot)
    {
        const int allowedCount = 10;
        var absolutePathToServiceRoot = Path.Join(workingDirectory, relativePathToServiceRoot);
        var csprojFiles = Directory.GetFiles(Path.Join(workingDirectory, relativePathToServiceRoot), "*.csproj", SearchOption.TopDirectoryOnly);
        if (!csprojFiles.Any())
        {
            return new List<Deduction> {Deduction.Create(Logger, 100, "No csproj file found at {Location}", absolutePathToServiceRoot)};
        }
        var csproj = XDocument.Load(csprojFiles.First());
        if (csproj.Root == null)
        {
            return new List<Deduction> { Deduction.Create(Logger, 100, "Couldn't parse {CsProj}", csprojFiles.First()) };
        }

        var hintPaths = csproj.Root.Descendants("HintPath");
        return hintPaths.Select(hintPath => Deduction.Create(Logger, DeductionPerHintPath, "HintPath: {HintPath}", hintPath.Value)).ToList();
    }
}