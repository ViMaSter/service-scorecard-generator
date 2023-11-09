using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.LatestNET;

public class NewestMajor : TestWithNeighboringCsprojFixture
{
    [Test]
    public void Returns100Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.LatestNET.Check(logger);
        File.WriteAllText(AbsolutePathToProjectFile, File.ReadAllText(AbsolutePathToProjectFile).Replace("__NEWEST_MAJOR__", $"{check.NewestMajor}.0"));        
        
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        deductions.CountAndFinalScore(0, 100);
    }
}