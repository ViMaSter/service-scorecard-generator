using ScorecardGenerator.Test.Helper;
using Serilog;
namespace ScorecardGenerator.Test.Checks.HintPathCounter;

public class HasElevenHintPaths : TestWithNeighboringCsprojFixture
{
    [Test]
    public void Returns0Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.HintPathCounter.Check(logger);
        var deductions = check.SetupLoggerAndRun(WorkingDirectory, RelativePathToServiceRoot);
        deductions.CountAndFinalScore(11, 0);
    }
}