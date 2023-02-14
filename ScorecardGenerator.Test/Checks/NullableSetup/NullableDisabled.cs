using Serilog;

namespace ScorecardGenerator.Test.Checks.NullableSetup;

public class NullableDisabled : TestWithNeighboringFixture
{
    [Test]
    public void Returns0Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.NullableSetup.Check(logger);
        var deductions = check.SetupLoggerAndRun(WorkingDirectory, RelativePathToServiceRoot);
        Assert.That(deductions, Has.Count.EqualTo(1));
        Assert.That(deductions.CalculateFinalScore(), Is.EqualTo(0));
    }
}