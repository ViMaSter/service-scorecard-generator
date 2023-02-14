using Serilog;

namespace ScorecardGenerator.Test.Checks.LatestNET;

public class NETCore31 : TestWithNeighboringFixture
{
    [Test]
    public void Returns43Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.LatestNET.Check(logger);
        var deductions = check.SetupLoggerAndRun(WorkingDirectory, RelativePathToServiceRoot);
        Assert.That(deductions, Has.Count.EqualTo(1));
        Assert.That(deductions.CalculateFinalScore(), Is.EqualTo((int)Math.Round(3.0 / 7.0*100)));
    }
}