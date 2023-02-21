using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.ImplicitAssemblyInfo;

public class MissingGenerateAssemblyInfo : TestWithNeighboringFixture
{
    [Test]
    public void Returns0Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.ImplicitAssemblyInfo.Check(logger);
        var deductions = check.SetupLoggerAndRun(WorkingDirectory, RelativePathToServiceRoot);
        Assert.Multiple(() =>
        {
            deductions.CountAndFinalScore(1, 0);
        });
    }
}