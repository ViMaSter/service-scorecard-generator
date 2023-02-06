using ScorecardGenerator.Calculation;
using ScorecardGenerator.Checks;
using ScorecardGenerator.Visualizer;
using Serilog;

namespace ScorecardGenerator;

internal abstract class Program
{
    private static void Main()
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var directoriesInWorkingDirectory = Directory.GetDirectories(Directory.GetCurrentDirectory()).Where(Utilities.ContainsCsProj);
        var checks = new List<IRunCheck>
        {
            new HasNET7(logger),
            new HintPathCounter(logger)
        };

        var serviceScores = directoriesInWorkingDirectory.ToDictionary(Utilities.RootDirectoryToProjectNameFromCsproj, serviceRootDirectory =>
        {
            var scoreByCheck = checks.ToDictionary(check => check.GetType().Name, check => check.Run(serviceRootDirectory));
            scoreByCheck.Add("Average", (int)Math.Round((decimal)scoreByCheck.Values.Sum() / checks.Count));
            return scoreByCheck;
        });

        var runInfo = new RunInfo(checks.Select(check => check.GetType().Name), serviceScores);

        File.WriteAllText("result.md", new MarkdownVisualizer(logger).ToMarkdown(runInfo));
    }
}