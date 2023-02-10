using System.Collections.Immutable;
using ScorecardGenerator.Calculation;
using ScorecardGenerator.Checks;
using ScorecardGenerator.Visualizer;
using Serilog;

namespace ScorecardGenerator;

internal class GenerateScorecard
{
    private record Checks
    (
        List<BaseCheck> Gold, List<BaseCheck> Silver, List<BaseCheck> Bronze
    )
    {
        public const int GoldWeight = 10;
        public const int SilverWeight = 5;
        public const int BronzeWeight = 1;
    }
        
    private readonly IEnumerable<string> _directoriesInWorkingDirectory;
    private readonly ILogger _logger;

    public GenerateScorecard(ILogger logger)
    {
        _logger = logger;
        _directoriesInWorkingDirectory = Directory.EnumerateDirectories(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories)
            .Where(directory => Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly).Any());
    }
    
    public void Execute(string azurePAT, string outputPath, string? excludePath = null)
    {
        var checks = new Checks
        (
            new List<BaseCheck>()
            {
                new ScorecardGenerator.Checks.HasNET7.Check(_logger),
                new ScorecardGenerator.Checks.PendingRenovateAzurePRs.Check(_logger, azurePAT)
            },
            new List<BaseCheck>()
            {
                new ScorecardGenerator.Checks.NullableSetup.Check(_logger)
            },
            new List<BaseCheck>()
            {
                new ScorecardGenerator.Checks.HintPathCounter.Check(_logger),
            }
        );
        var listByGroup = new Dictionary<string, IList<CheckInfo>>
        {
            { nameof(checks.Gold), checks.Gold.Select(GenerateCheckRunInfo).ToList() },
            { nameof(checks.Silver), checks.Silver.Select(GenerateCheckRunInfo).ToList() },
            { nameof(checks.Bronze), checks.Bronze.Select(GenerateCheckRunInfo).ToList() },
        };
        
        var scoreForServiceByCheck = _directoriesInWorkingDirectory.Where(DoesntMatchExcludePath(excludePath)).ToImmutableSortedDictionary(entry=>Utilities.RootDirectoryToProjectNameFromCsproj(entry).Replace(Directory.GetCurrentDirectory(), "").Replace(Path.DirectorySeparatorChar, '/'), serviceRootDirectory =>
        {
            var goldScoreByCheck = checks.Gold.ToDictionary(Utilities.GetNameFromCheckClass, check => check.SetupLoggerAndRun(Directory.GetCurrentDirectory(), serviceRootDirectory.Replace(Directory.GetCurrentDirectory(), "")));
            var silverScoreByCheck = checks.Silver.ToDictionary(Utilities.GetNameFromCheckClass, check => check.SetupLoggerAndRun(Directory.GetCurrentDirectory(), serviceRootDirectory.Replace(Directory.GetCurrentDirectory(), "")));
            var bronzeScoreByCheck = checks.Bronze.ToDictionary(Utilities.GetNameFromCheckClass, check => check.SetupLoggerAndRun(Directory.GetCurrentDirectory(), serviceRootDirectory.Replace(Directory.GetCurrentDirectory(), "")));
            var totalScore = new[]
            {
                (decimal)goldScoreByCheck.Values.Sum()   * Checks.GoldWeight,
                (decimal)silverScoreByCheck.Values.Sum() * Checks.SilverWeight,
                (decimal)bronzeScoreByCheck.Values.Sum() * Checks.BronzeWeight
            }.Sum();

            var totalChecks = goldScoreByCheck.Count * Checks.GoldWeight + silverScoreByCheck.Count * Checks.SilverWeight + bronzeScoreByCheck.Count * Checks.BronzeWeight;
            var average = (int)Math.Round(totalScore / totalChecks);
            var scoreByCheck = goldScoreByCheck
                                                    .Concat(silverScoreByCheck)
                                                    .Concat(bronzeScoreByCheck)
                                                    .ToDictionary(a => a.Key, a => a.Value);
            return new RunInfo.ServiceScorecard(scoreByCheck, average);
        });

        var runInfo = new RunInfo(listByGroup, scoreForServiceByCheck);

        IVisualizer visualizer = new AzureWikiTableVisualizer(_logger, outputPath);
        visualizer.Visualize(runInfo);
    }

    private CheckInfo GenerateCheckRunInfo(BaseCheck check)
    {
        return new CheckInfo(Utilities.GetNameFromCheckClass(check), check.InfoPageContent);
    }

    private Func<string, bool> DoesntMatchExcludePath(string? excludePath)
    {
        if (string.IsNullOrEmpty(excludePath))
        {
            return _ => true;
        }
        return path => !path.Contains(excludePath);
    }
}

public record CheckInfo(string Name, string InfoPageContent);