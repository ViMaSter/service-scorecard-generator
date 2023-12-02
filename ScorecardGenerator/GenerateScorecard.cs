using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ScorecardGenerator.Calculation;
using ScorecardGenerator.Checks;
using ScorecardGenerator.Visualizer;
using Serilog;

namespace ScorecardGenerator;

[ExcludeFromCodeCoverage(Justification = "Opinionated execution of tested code")]
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
        
    private readonly IEnumerable<string> _projectsInWorkingDirectory;
    private readonly ILogger _logger;

    public GenerateScorecard(ILogger logger)
    {
        _logger = logger;
        _projectsInWorkingDirectory = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj", SearchOption.AllDirectories);
    }
    
    public void Execute(string azurePAT, string outputPath, string? excludePath = null)
    {
        var checks = new Checks
        (
            new List<BaseCheck>()
            {
                new ScorecardGenerator.Checks.BuiltForAKS.Check(_logger),
                new ScorecardGenerator.Checks.LatestNET.Check(_logger),
                new ScorecardGenerator.Checks.PendingRenovateAzurePRs.Check(_logger, azurePAT),
                new ScorecardGenerator.Checks.ProperDockerfile.Check(_logger)
            },
            new List<BaseCheck>()
            {
                new ScorecardGenerator.Checks.NullableSetup.Check(_logger),
                new ScorecardGenerator.Checks.ImplicitAssemblyInfo.Check(_logger)
            },
            new List<BaseCheck>()
            {
                new ScorecardGenerator.Checks.HintPathCounter.Check(_logger)
            }
        );
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
                (decimal?)goldDeductionsByCheck.Values.Sum(deductions=>deductions.CalculateFinalScore())   * Checks.GoldWeight,
                (decimal?)silverDeductionsByCheck.Values.Sum(deductions=>deductions.CalculateFinalScore()) * Checks.SilverWeight,
                (decimal?)bronzeDeductionsByCheck.Values.Sum(deductions=>deductions.CalculateFinalScore()) * Checks.BronzeWeight
            }.Sum();

            var totalChecks = goldDeductionsByCheck.Count(ThatDontHaveDisqualification) * Checks.GoldWeight + silverDeductionsByCheck.Count(ThatDontHaveDisqualification) * Checks.SilverWeight + bronzeDeductionsByCheck.Count(ThatDontHaveDisqualification) * Checks.BronzeWeight;
            var average = totalScore == null ? 0 : (int)Math.Round((decimal)totalScore / totalChecks);
            var deductionsByCheck = goldDeductionsByCheck
                                                    .Concat(silverDeductionsByCheck)
                                                    .Concat(bronzeDeductionsByCheck)
                                                    .ToDictionary(a => a.Key, a => a.Value);
            return new RunInfo.ServiceScorecard(deductionsByCheck, average);
        });

        var runInfo = new RunInfo(listByGroup, scoreForServiceByCheck);

        IVisualizer visualizer = new HTMLVisualizer(_logger, outputPath);
        visualizer.Visualize(runInfo);
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
