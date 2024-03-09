using ScorecardGenerator.Checks.ProperDockerfile;
using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.ProperDockerfile.Result50;

public class DotnetBuildOnly : TestWithNeighboringCsprojAndDockerfileFixture
{
    [Test]
    public void Returns50Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new Check(logger);
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        deductions.CountAndFinalScore(1, 50);
    }
}