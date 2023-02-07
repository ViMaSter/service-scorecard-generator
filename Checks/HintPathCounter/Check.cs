using System.Xml.Linq;
using Serilog;

namespace ScorecardGenerator.Checks.HintPathCounter;

internal class Check : BaseCheck
{
    public Check(ILogger logger) : base(logger)
    {
    }
    
    const int PenaltyPerHintPath = 10;

    protected override int Run(string workingDirectory, string relativePathToServiceRoot)
    {
        const int allowedCount = 10;
        var absolutePathToServiceRoot = Path.Join(workingDirectory, relativePathToServiceRoot);
        var csprojFiles = Directory.GetFiles(Path.Join(workingDirectory, relativePathToServiceRoot), "*.csproj", SearchOption.TopDirectoryOnly);
        if (!csprojFiles.Any())
        {
            Logger.Warning("No csproj file found at {Location}", absolutePathToServiceRoot);
            return 0;
        }
        var csproj = XDocument.Load(csprojFiles.First());
        if (csproj.Root == null)
        {
            Logger.Warning("Couldn't parse {CsProj}", csprojFiles.First());
            return 0;
        }

        var hintPaths = csproj.Root.Descendants("HintPath");
        var currentCount = allowedCount - hintPaths.Count();
        if (currentCount < 0)
        {
            currentCount = 0;
        }
        
        Logger.Information("Deducting {Penalty} points per HintPath. Current count: {CurrentCount}", PenaltyPerHintPath, hintPaths.Count());
        return currentCount * PenaltyPerHintPath;
    }
}