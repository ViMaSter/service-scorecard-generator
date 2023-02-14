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
                new ScorecardGenerator.Checks.BuiltForAKS.Check(_logger),
                new ScorecardGenerator.Checks.LatestNET.Check(_logger),
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
        
        var scoreForServiceByCheck = _directoriesInWorkingDirectory.Where(DoesntMatchExcludePath(excludePath)).ToImmutableSortedDictionary(entry=>Utilities.RootDirectoryToAbsolutePathToFirstCsproj(entry).Replace(Directory.GetCurrentDirectory(), "").Replace(Path.DirectorySeparatorChar, '/'), serviceRootDirectory =>
        {
            var goldDeductionsByCheck = checks.Gold.ToDictionary(Utilities.GetNameFromCheckClass, check => check.SetupLoggerAndRun(Directory.GetCurrentDirectory(), serviceRootDirectory.Replace(Directory.GetCurrentDirectory(), "")));
            var silverDeductionsByCheck = checks.Silver.ToDictionary(Utilities.GetNameFromCheckClass, check => check.SetupLoggerAndRun(Directory.GetCurrentDirectory(), serviceRootDirectory.Replace(Directory.GetCurrentDirectory(), "")));
            var bronzeDeductionsByCheck = checks.Bronze.ToDictionary(Utilities.GetNameFromCheckClass, check => check.SetupLoggerAndRun(Directory.GetCurrentDirectory(), serviceRootDirectory.Replace(Directory.GetCurrentDirectory(), "")));
            var totalScore = new[]
            {
                (decimal)goldDeductionsByCheck.Values.Sum(deductions=>deductions.CalculateFinalScore())   * Checks.GoldWeight,
                (decimal)silverDeductionsByCheck.Values.Sum(deductions=>deductions.CalculateFinalScore()) * Checks.SilverWeight,
                (decimal)bronzeDeductionsByCheck.Values.Sum(deductions=>deductions.CalculateFinalScore()) * Checks.BronzeWeight
            }.Sum();

            var totalChecks = goldDeductionsByCheck.Count * Checks.GoldWeight + silverDeductionsByCheck.Count * Checks.SilverWeight + bronzeDeductionsByCheck.Count * Checks.BronzeWeight;
            var average = (int)Math.Round(totalScore / totalChecks);
            var deductionsByCheck = goldDeductionsByCheck
                                                    .Concat(silverDeductionsByCheck)
                                                    .Concat(bronzeDeductionsByCheck)
                                                    .ToDictionary(a => a.Key, a => a.Value);
            return new RunInfo.ServiceScorecard(deductionsByCheck, average);
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