using ScorecardGenerator.Test.Helper;
using Serilog;
namespace ScorecardGenerator.Test.Checks.HintPathCounter;

public class EmptyProjectFile : TestWithNeighboringCsprojFixture
{
    [Test]
    public void Returns0Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.HintPathCounter.Check(logger);
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        deductions.CountAndFinalScore(1, 0);
    }
}