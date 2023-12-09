using ScorecardGenerator.Checks.ImplicitAssemblyInfo;
using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.ImplicitAssemblyInfo;

public class MissingProperties : TestWithNeighboringCsprojFixture
{
    [Test]
    public void Returns60Points()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new Check(logger);
        var deductions = check.SetupLoggerAndRun(AbsolutePathToProjectFile);
        Assert.Multiple(() =>
        {
            deductions.CountAndFinalScore(2, 60);
            Assert.That(deductions.Any(deduction => deduction.Justification.Contains("UserSecretsId")));
            Assert.That(deductions.Any(deduction => deduction.Justification.Contains("Product")));
        });
    }
}