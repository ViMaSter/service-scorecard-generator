using ScorecardGenerator.Checks.LatestNET;
using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.LatestNET;

public class NETCore31 : TestWithNeighboringCsprojFixture
{
    [Test]
    public void Returns43Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new Check(logger);
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        deductions.CountAndFinalScore(1, (int)Math.Round(3.0 / check.NewestMajor*100));
    }
}