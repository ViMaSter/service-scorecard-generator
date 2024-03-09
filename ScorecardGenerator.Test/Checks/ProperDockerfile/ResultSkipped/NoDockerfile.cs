using ScorecardGenerator.Checks.ProperDockerfile;
using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.ProperDockerfile.ResultSkipped;

public class NoDockerfile : TestWithNeighboringCsprojFixture
{
    [Test]
    public void DisqualifiesCheck()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new Check(logger);
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        deductions.CountAndFinalScore(1, null);
        Assert.That(deductions.First().IsDisqualification, Is.True);
    }
}