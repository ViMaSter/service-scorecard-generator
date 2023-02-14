using System.Collections.Immutable;
using ScorecardGenerator.Calculation;
using ScorecardGenerator.Checks;
using Serilog;

namespace ScorecardGenerator.Test.Visualizer.AzureWikiTableVisualizer;

public class Example : TestWithNeighboringDirectoryFixture
{
    [Test]
    public void Returns0Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var tempPath = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        var visualizer = new ScorecardGenerator.Visualizer.AzureWikiTableVisualizer(logger, tempPath);
        var checks = new Dictionary<string, IList<CheckInfo>>
        {
            { "Gold", new List<CheckInfo> { new("Check", "PageContent") } },
            { "Silver", new List<CheckInfo>() },
            { "Bronze", new List<CheckInfo>() }
        };

        var serviceInfo = new Dictionary<string, RunInfo.ServiceScorecard>
        {
            {"service", new RunInfo.ServiceScorecard(new Dictionary<string, IList<BaseCheck.Deduction>>
            {
                {"check", new List<BaseCheck.Deduction> {BaseCheck.Deduction.Create( logger, 10, "justification: {Value}", "value")}} 
            }, 10)}
        }.ToImmutableSortedDictionary();
        
        visualizer.Visualize(new RunInfo(checks, serviceInfo));
        CompareFilesInsideDirectoriesWithNUnitAsserts(tempPath, Path.Join(WorkingDirectory, RelativePathToServiceRoot));
    }

    private static void CompareFilesInsideDirectoriesWithNUnitAsserts(string absolutePathToDirectoryActual, string absolutePathtoDirectoryExpected)
    {
        var actualDirectoryInfo = new DirectoryInfo(absolutePathToDirectoryActual);
        var expectedDirectoryInfo = new DirectoryInfo(absolutePathtoDirectoryExpected);

        var actualFiles = actualDirectoryInfo.GetFiles("*", SearchOption.AllDirectories);
        var expectedFiles = expectedDirectoryInfo.GetFiles("*", SearchOption.AllDirectories);

        Assert.That(actualFiles.Length, Is.EqualTo(expectedFiles.Length));

        var expectedFilesDictionary = expectedFiles.ToDictionary(file => file.Name);

        foreach (var actualFile in actualFiles)
        {
            Assert.That(actualFile.Name, Is.EqualTo(expectedFilesDictionary[actualFile.Name].Name));
            Assert.That(File.ReadAllText(actualFile.FullName), Is.EqualTo(File.ReadAllText(expectedFilesDictionary[actualFile.Name].FullName).Replace("YYYY-MM-DD", DateTime.Now.ToString("yyyy-MM-dd"))));
        }
    }
}