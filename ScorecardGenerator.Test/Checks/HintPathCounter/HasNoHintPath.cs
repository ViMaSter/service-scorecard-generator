using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.HintPathCounter;

public class HasNoHintPath : TestWithNeighboringFixture
{
    [Test]
    public void Returns100Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.HintPathCounter.Check(logger);
        var deductions = check.SetupLoggerAndRun(WorkingDirectory, RelativePathToServiceRoot);
        deductions.CountAndFinalScore(0, 100);
    }
}