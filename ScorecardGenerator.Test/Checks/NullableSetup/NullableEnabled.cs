using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.NullableSetup;

public class NullableEnabled : TestWithNeighboringFixture
{
    [Test]
    public void Returns100Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.NullableSetup.Check(logger);
        var deductions = check.SetupLoggerAndRun(WorkingDirectory, RelativePathToServiceRoot);
        deductions.CountAndFinalScore(0, 100);
    }
}