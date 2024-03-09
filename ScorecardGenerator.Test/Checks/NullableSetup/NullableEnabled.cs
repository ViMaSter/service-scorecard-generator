using ScorecardGenerator.Checks.NullableSetup;
using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.NullableSetup;

public class NullableEnabled : TestWithNeighboringCsprojFixture
{
    [Test]
    public void Returns100Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new Check(logger);
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        deductions.CountAndFinalScore(0, 100);
    }
}