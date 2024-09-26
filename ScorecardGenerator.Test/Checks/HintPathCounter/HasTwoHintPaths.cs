using ScorecardGenerator.Checks.HintPathCounter;
using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.HintPathCounter;

public class HasTwoHintPaths : TestWithNeighboringCsprojFixture
{
    [Test]
    public void Returns80Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new Check(logger);
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        deductions.CountAndFinalScore(2, 80);
    }
}