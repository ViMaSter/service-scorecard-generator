using ScorecardGenerator.Calculation;
using ScorecardGenerator.Checks;
using ScorecardGenerator.Visualizer;
using Serilog;

namespace ScorecardGenerator;

internal abstract class Program
{
    private record Checks
    (
        List<IRunCheck> Gold, List<IRunCheck> Silver, List<IRunCheck> Bronze
    )
    {
        public IDictionary<string, IList<string>> ListByGroup() => new Dictionary<string, IList<string>>
        {
            { nameof(Gold), Gold.Select(check => check.GetType().Name).ToList() },
            { nameof(Silver), Silver.Select(check => check.GetType().Name).ToList() },
            { nameof(Bronze), Bronze.Select(check => check.GetType().Name).ToList() },
        };
    }
    
    private static void Main()
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var directoriesInWorkingDirectory = Directory.GetDirectories(Directory.GetCurrentDirectory()).Where(Utilities.ContainsCsProj);
        var checks = new Checks
        (
            new List<IRunCheck>()
            {
                new HasNET7(logger),
                new HintPathCounter(logger)   
            },
            new List<IRunCheck>(),
            new List<IRunCheck>()
        );


        var scoreForServiceByCheck = directoriesInWorkingDirectory.ToDictionary(Utilities.RootDirectoryToProjectNameFromCsproj, serviceRootDirectory =>
        {
            var goldScoreByCheck = checks.Gold.ToDictionary(check => check.GetType().Name, check => check.Run(serviceRootDirectory));
            var silverScoreByCheck = checks.Silver.ToDictionary(check => check.GetType().Name, check => check.Run(serviceRootDirectory));
            var bronzeScoreByCheck = checks.Bronze.ToDictionary(check => check.GetType().Name, check => check.Run(serviceRootDirectory));
            var totalScore = new[]
            {
                (decimal)goldScoreByCheck.Values.Sum()   * ServiceInfo.GoldWeight,
                (decimal)silverScoreByCheck.Values.Sum() * ServiceInfo.SilverWeight,
                (decimal)bronzeScoreByCheck.Values.Sum() * ServiceInfo.BronzeWeight
            }.Sum();

            var totalChecks = goldScoreByCheck.Count * ServiceInfo.GoldWeight + silverScoreByCheck.Count * ServiceInfo.SilverWeight + bronzeScoreByCheck.Count * ServiceInfo.BronzeWeight;
            var average = (int)Math.Round(totalScore / totalChecks);
            return new ServiceInfo(goldScoreByCheck.Concat(silverScoreByCheck).Concat(bronzeScoreByCheck).ToDictionary(a=>a.Key, a=>a.Value), average);
        });

        var runInfo = new RunInfo(checks.ListByGroup(), scoreForServiceByCheck);

        File.WriteAllText("result.md", new ColorizedHTMLTableVisualizer(logger).ToMarkdown(runInfo));
    }
}