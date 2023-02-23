using Serilog;

namespace ScorecardGenerator.Test.Utilities;

public class GetNameFromCheckClass
{
    [TestCase]
    public void WorksForLatestNET()
    {
        var emptyLogger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.LatestNET.Check(emptyLogger);
        var name = ScorecardGenerator.Utilities.GetNameFromCheckClass(check);
        Assert.That(name, Is.EqualTo(nameof(ScorecardGenerator.Checks.LatestNET)));
    }
}