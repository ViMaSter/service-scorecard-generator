using ScorecardGenerator.Test.Helper;
using ScorecardGenerator.Test.Utilities;
using Serilog;

namespace ScorecardGenerator.Test.Checks.LatestNET;

public class NET6 : TestWithNeighboringCsprojFixture
{
    [Test]
    public void Returns86Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.LatestNET.Check(logger);
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        deductions.CountAndFinalScore(1, (int)Math.Round(6.0 / check.NewestMajor*100));
    }
}