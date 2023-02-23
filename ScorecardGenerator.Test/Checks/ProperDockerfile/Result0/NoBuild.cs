using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.ProperDockerfile.Result0;

public class NoBuild : TestWithNeighboringCsprojAndDockerfileFixture
{
    [Test]
    public void Returns0Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.ProperDockerfile.Check(logger);
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        deductions.CountAndFinalScore(1, 0);
    }
}