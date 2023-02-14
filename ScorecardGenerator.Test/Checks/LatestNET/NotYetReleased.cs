using Serilog;

namespace ScorecardGenerator.Test.Checks.LatestNET;

public class NotYetReleased : TestWithNeighboringFixture
{
    [Test]
    public void Returns95Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.LatestNET.Check(logger);
        var deductions = check.SetupLoggerAndRun(WorkingDirectory, RelativePathToServiceRoot);
        Assert.That(deductions, Has.Count.EqualTo(1));
        Assert.That(deductions.CalculateFinalScore(), Is.EqualTo(95));
    }
}