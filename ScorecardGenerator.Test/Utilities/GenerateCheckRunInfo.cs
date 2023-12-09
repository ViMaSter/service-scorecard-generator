using ScorecardGenerator.Checks;
using ScorecardGenerator.Checks.LatestNET;
using Serilog;

namespace ScorecardGenerator.Test.Utilities;

public class GenerateCheckRunInfo
{
    private class CheckWithoutMarkdownPage : BaseCheck
    {
        public CheckWithoutMarkdownPage(ILogger logger) : base(logger)
        {
        }

        protected override IList<Deduction> Run(string absolutePathToProjectFile)
        {
            throw new NotSupportedException();
        }
    }
    [TestCase]
    public void WorksForLatestNET()
    {
        var emptyLogger = new LoggerConfiguration().CreateLogger();
        var check = new Check(emptyLogger);
        var checkInfo = ScorecardGenerator.Utilities.GenerateCheckRunInfo(check);
        Assert.Multiple(() =>
        {
            Assert.That(checkInfo.Name, Is.EqualTo(nameof(ScorecardGenerator.Checks.LatestNET)));
            Assert.That(check.InfoPageContent, Is.EqualTo(checkInfo.InfoPageContent));
        });
    }

    [TestCase]
    public void FailsForCheckWithoutMarkdownPage()
    {
        var emptyLogger = new LoggerConfiguration().CreateLogger();
        Assert.Throws<FileNotFoundException>(() =>
        {
            _ = new CheckWithoutMarkdownPage(emptyLogger);
        });
    }
}