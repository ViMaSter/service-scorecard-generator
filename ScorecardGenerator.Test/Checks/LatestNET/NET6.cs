using ScorecardGenerator.Checks.LatestNET;
using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.LatestNET;

public class NET6 : TestWithNeighboringCsprojFixture
{
    [Test]
    public void Returns86Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new Check(logger);
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        deductions.CountAndFinalScore(1, (int)Math.Round(6.0 / check.NewestMajor*100));
    }
}