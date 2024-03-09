using ScorecardGenerator.Checks.LatestNET;
using Serilog;

namespace ScorecardGenerator.Test.Utilities;

public class GetNameFromCheckClass
{
    [TestCase]
    public void WorksForLatestNET()
    {
        var emptyLogger = new LoggerConfiguration().CreateLogger();
        var check = new Check(emptyLogger);
        var name = ScorecardGenerator.Utilities.GetNameFromCheckClass(check);
        Assert.That(name, Is.EqualTo(nameof(ScorecardGenerator.Checks.LatestNET)));
    }
}