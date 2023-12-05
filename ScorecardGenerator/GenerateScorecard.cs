using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ScorecardGenerator.Calculation;
using ScorecardGenerator.Checks;
using ScorecardGenerator.Checks.PendingRenovateAzurePRs;
using ScorecardGenerator.Configuration;
using ScorecardGenerator.Visualizer;
using Serilog;

namespace ScorecardGenerator;

[ExcludeFromCodeCoverage(Justification = "Opinionated execution of tested code")]
internal class GenerateScorecard
{
        
    private readonly IEnumerable<string> _projectsInWorkingDirectory;
    private readonly ILogger _logger;

    public GenerateScorecard(ILogger logger)
    {
        _logger = logger;
        _projectsInWorkingDirectory = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj", SearchOption.AllDirectories);
    }
    
    public void Execute(string outputPath, string visualizer, string? excludePath = null, string azurePAT = "")
    {
        var configParser = new ConfigurationParser(_logger, new []{new Check.AzurePAT(azurePAT)});
        var checks = configParser.LoadChecks();
        var listByGroup = new Dictionary<string, IList<CheckInfo>>
        {
            { nameof(checks.Gold), checks.Gold.Select(Utilities.GenerateCheckRunInfo).ToList() },
            { nameof(checks.Silver), checks.Silver.Select(Utilities.GenerateCheckRunInfo).ToList() },
            { nameof(checks.Bronze), checks.Bronze.Select(Utilities.GenerateCheckRunInfo).ToList() }
        };
        
        var scoreForServiceByCheck = _projectsInWorkingDirectory.Where(DoesntMatchExcludePath(excludePath)).ToImmutableSortedDictionary(entry=>entry.Replace(Directory.GetCurrentDirectory(), "").Replace(Path.DirectorySeparatorChar, '/'), serviceRootDirectory =>
        {
            var goldDeductionsByCheck = checks.Gold.ToDictionary(Utilities.GetNameFromCheckClass, check => check.SetupLoggerAndRun(Path.Join(Directory.GetCurrentDirectory(), serviceRootDirectory.Replace(Directory.GetCurrentDirectory(), ""))));
            var silverDeductionsByCheck = checks.Silver.ToDictionary(Utilities.GetNameFromCheckClass, check => check.SetupLoggerAndRun(Path.Join(Directory.GetCurrentDirectory(), serviceRootDirectory.Replace(Directory.GetCurrentDirectory(), ""))));
            var bronzeDeductionsByCheck = checks.Bronze.ToDictionary(Utilities.GetNameFromCheckClass, check => check.SetupLoggerAndRun(Path.Join(Directory.GetCurrentDirectory(), serviceRootDirectory.Replace(Directory.GetCurrentDirectory(), ""))));
            var totalScore = new[]
            {
                (decimal?)goldDeductionsByCheck.Values.Sum(deductions=>deductions.CalculateFinalScore())   * Configuration.Checks.GOLD_WEIGHT,
                (decimal?)silverDeductionsByCheck.Values.Sum(deductions=>deductions.CalculateFinalScore()) * Configuration.Checks.SILVER_WEIGHT,
                (decimal?)bronzeDeductionsByCheck.Values.Sum(deductions=>deductions.CalculateFinalScore()) * Configuration.Checks.BRONZE_WEIGHT
            }.Sum();

            var totalChecks = goldDeductionsByCheck.Count(ThatDontHaveDisqualification) * Configuration.Checks.GOLD_WEIGHT + silverDeductionsByCheck.Count(ThatDontHaveDisqualification) * Configuration.Checks.SILVER_WEIGHT + bronzeDeductionsByCheck.Count(ThatDontHaveDisqualification) * Configuration.Checks.BRONZE_WEIGHT;
            var average = totalScore == null ? 0 : (int)Math.Round((decimal)totalScore / totalChecks);
            var deductionsByCheck = goldDeductionsByCheck
                                                    .Concat(silverDeductionsByCheck)
                                                    .Concat(bronzeDeductionsByCheck)
                                                    .ToDictionary(a => a.Key, a => a.Value);
            return new RunInfo.ServiceScorecard(deductionsByCheck, average);
        });

        var runInfo = new RunInfo(listByGroup, scoreForServiceByCheck);

        IVisualizer visualizerToUse = visualizer switch
        {
            "html" => new HTMLVisualizer(_logger, outputPath),
            "azurewiki" => new AzureWikiTableVisualizer(_logger, outputPath),
            _ => throw new ArgumentException($"Unknown visualizer {visualizer}")
        };
        visualizerToUse.Visualize(runInfo);
    }

    private bool ThatDontHaveDisqualification(KeyValuePair<string, IList<BaseCheck.Deduction>> arg)
    {
        return !arg.Value.Any(deduction => deduction.IsDisqualification);
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
