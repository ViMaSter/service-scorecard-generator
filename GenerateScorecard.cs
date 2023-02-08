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
        // find all directories in working directory that contain a csproj file
        _directoriesInWorkingDirectory = Directory.EnumerateDirectories(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories)
            .Where(directory => Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly).Any());
    }
    
    public void Execute(string azurePAT)
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
                new ScorecardGenerator.Checks.NullableSetup.Check(_logger),
                new ScorecardGenerator.Checks.WarningsAsError.Check(_logger)  
            },
            new List<BaseCheck>()
            {
                new ScorecardGenerator.Checks.HintPathCounter.Check(_logger),
            }
        );
        var listByGroup = new Dictionary<string, IList<string>>
        {
            { nameof(checks.Gold), checks.Gold.Select(Utilities.GetNameFromCheckClass).ToList() },
            { nameof(checks.Silver), checks.Silver.Select(Utilities.GetNameFromCheckClass).ToList() },
            { nameof(checks.Bronze), checks.Bronze.Select(Utilities.GetNameFromCheckClass).ToList() },
        };
        
        var scoreForServiceByCheck = _directoriesInWorkingDirectory.ToDictionary(entry=>Utilities.RootDirectoryToProjectNameFromCsproj(entry).Replace(Directory.GetCurrentDirectory(), "").Replace(Path.DirectorySeparatorChar, '/'), serviceRootDirectory =>
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

        File.WriteAllText("result.md", new ColorizedHTMLTableVisualizer(_logger).ToMarkdown(runInfo));
    }
}