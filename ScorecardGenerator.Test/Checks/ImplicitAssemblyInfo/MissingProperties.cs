using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.ImplicitAssemblyInfo;

public class MissingProperties : TestWithNeighboringFixture
{
    [Test]
    public void Returns60Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.ImplicitAssemblyInfo.Check(logger);
        var deductions = check.SetupLoggerAndRun(WorkingDirectory, RelativePathToServiceRoot);
        Assert.Multiple(() =>
        {
            deductions.CountAndFinalScore(2, 60);
            Assert.That(deductions.Any(deduction => deduction.Justification.Contains("UserSecretsId")));
            Assert.That(deductions.Any(deduction => deduction.Justification.Contains("Product")));
        });
    }
}