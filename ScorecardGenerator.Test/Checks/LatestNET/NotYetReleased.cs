using ScorecardGenerator.Checks.LatestNET;
using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.LatestNET;

public class NotYetReleased : TestWithNeighboringCsprojFixture
{
    [Test]
    public void Returns95Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new Check(logger);
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        deductions.CountAndFinalScore(1, 95);
    }
}