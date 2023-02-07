using ScorecardGenerator.Calculation;
using ScorecardGenerator.Checks;
using ScorecardGenerator.Visualizer;
using Serilog;

namespace ScorecardGenerator;

internal class GenerateScorecard
{
    private record Checks
    (
        List<IRunCheck> Gold, List<IRunCheck> Silver, List<IRunCheck> Bronze
    )
    {
        public const int GoldWeight = 10;
        public const int SilverWeight = 5;
        public const int BronzeWeight = 1;
    }
        
    private readonly Checks _checks;
    private readonly IEnumerable<string> _directoriesInWorkingDirectory;
    private readonly Dictionary<string,IList<string>> _listByGroup;
    private readonly ILogger _logger;

    public GenerateScorecard(ILogger logger)
    {
        _logger = logger;
        _directoriesInWorkingDirectory = Directory.GetDirectories(Directory.GetCurrentDirectory()).Where(Utilities.ContainsCsProj);
        _checks = new Checks
        (
            new List<IRunCheck>()
            {
                new HasNET7(logger),
                new HintPathCounter(logger)   
            },
            new List<IRunCheck>(),
            new List<IRunCheck>()
        );
        _listByGroup = new Dictionary<string, IList<string>>
        {
            { nameof(_checks.Gold), _checks.Gold.Select(check => check.GetType().Name).ToList() },
            { nameof(_checks.Silver), _checks.Silver.Select(check => check.GetType().Name).ToList() },
            { nameof(_checks.Bronze), _checks.Bronze.Select(check => check.GetType().Name).ToList() },
        };
    }
    
    public void Execute(string AzurePAT)
    {
        var scoreForServiceByCheck = _directoriesInWorkingDirectory.ToDictionary(Utilities.RootDirectoryToProjectNameFromCsproj, serviceRootDirectory =>
        {
            var goldScoreByCheck = _checks.Gold.ToDictionary(check => check.GetType().Name, check => check.Run(serviceRootDirectory));
            var silverScoreByCheck = _checks.Silver.ToDictionary(check => check.GetType().Name, check => check.Run(serviceRootDirectory));
            var bronzeScoreByCheck = _checks.Bronze.ToDictionary(check => check.GetType().Name, check => check.Run(serviceRootDirectory));
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

        var runInfo = new RunInfo(_listByGroup, scoreForServiceByCheck);

        File.WriteAllText("result.md", new ColorizedHTMLTableVisualizer(_logger).ToMarkdown(runInfo));
    }
}