using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.ImplicitAssemblyInfo;

public class DisabledGenerateAssemblyInfo : TestWithNeighboringCsprojFixture
{
    [Test]
    public void Returns0Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.ImplicitAssemblyInfo.Check(logger);
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        Assert.Multiple(() =>
        {
            deductions.CountAndFinalScore(1, 0);
        });
    }
}