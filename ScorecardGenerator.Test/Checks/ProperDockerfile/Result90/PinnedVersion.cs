using ScorecardGenerator.Checks.ProperDockerfile;
using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.ProperDockerfile.Result90;

public class PinnedVersion : TestWithNeighboringCsprojAndDockerfileFixture
{
    [Test]
    public void Returns90Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new Check(logger);
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        deductions.CountAndFinalScore(1, 90);
    }
}