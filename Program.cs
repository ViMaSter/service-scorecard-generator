using ScorecardGenerator.Checks;

namespace ScorecardGenerator;

internal abstract class Program
{
    static void Main(string[] args)
    {
        var directoriesInWorkingDirectory = Directory.GetDirectories(Directory.GetCurrentDirectory()).Where(ContainsCsProj);
        var checks = new List<IRunCheck>
        {
            new HasNet7(),
            new HintPathCounter()
        };

        var headers = $"| ServiceName | {string.Join(" | ", checks.Select(check => check.GetType().Name))} | Average |";
        var divider = $"| --- | {string.Join(" | ", checks.Select(check => "---"))} | --- |";

        var serviceScores = directoriesInWorkingDirectory.ToDictionary(RootDirectoryToProjectNameFromCsproj, serviceRootDirectory =>
        {
            var scoreByCheck = checks.ToDictionary(check => check.GetType().Name, check => check.Run(serviceRootDirectory));
            scoreByCheck.Add("Average", (int)Math.Round((decimal)scoreByCheck.Values.Sum() / (decimal)checks.Count));
            return scoreByCheck;
        }).Select(ToProjectRow);
        
        File.WriteAllText("result.md", string.Join(Environment.NewLine, serviceScores.Prepend(divider).Prepend(headers)));
    }

    private static bool ContainsCsProj(string directory)
    {
        return Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly).Any();
    }

    private static string ToProjectRow(KeyValuePair<string, Dictionary<string, int>> arg)
    {
        return $"| {arg.Key} | {string.Join(" | ", arg.Value.Values)} |";
    }

    private static string RootDirectoryToProjectNameFromCsproj(string serviceRootDirectory)
    { 
        var csprojFiles = Directory.GetFiles(serviceRootDirectory, "*.csproj", SearchOption.TopDirectoryOnly);
        if (!csprojFiles.Any())
        {
            throw new FileNotFoundException("No csproj found to determine project name");
        }

        return Path.GetFileName(csprojFiles.First());
    }
}