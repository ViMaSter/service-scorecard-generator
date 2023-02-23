using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.ProperDockerfile.ResultSkipped;

public class NoDockerfile : TestWithNeighboringFixture
{
    [Test]
    public void DisqualifiesCheck()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.ProperDockerfile.Check(logger);
        var deductions = check.SetupLoggerAndRun(WorkingDirectory, RelativePathToServiceRoot);
        deductions.CountAndFinalScore(1, null);
        Assert.That(deductions.First().IsDisqualification, Is.True);
    }
}