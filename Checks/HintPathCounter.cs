using System.Xml.Linq;
using Serilog;

namespace ScorecardGenerator.Checks;

internal class HintPathCounter : IRunCheck
{
    private readonly ILogger _logger;

    public HintPathCounter(ILogger logger)
    {
        _logger = logger;
    }

    public int Run(string serviceRootDirectory)
    {
        var allowedCount = 10;
        var csprojFiles = Directory.GetFiles(serviceRootDirectory, "*.csproj", SearchOption.TopDirectoryOnly);
        if (!csprojFiles.Any())
        {
            return 0;
        }
        var csproj = XDocument.Load(csprojFiles.First());
        var hintPaths = csproj.Root.Descendants("HintPath");
        var currentCount = allowedCount - hintPaths.Count();
        if (currentCount < 0)
        {
            currentCount = 0;
        }

        return currentCount * 10;
    }
}