using ScorecardGenerator.Checks;
using ScorecardGenerator.Checks.RemainingDependencyUpgrades;
using ScorecardGenerator.Configuration;
using Logger = Serilog.Core.Logger;

namespace ScorecardGenerator.Test.Configuration;

public class CanLoadDefaults
{
    [Test]
    public void CanLoadDefaultChecks()
    {
        var parser = new ConfigurationParser(Logger.None, new List<object>(){ Logger.None, new Check.AzurePAT("")});
        var tempDirectory = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);
        Directory.SetCurrentDirectory(tempDirectory);
        var checks = parser.LoadChecks();
        Assert.That(checks.Gold, Is.Not.Null);
        Assert.That(checks.Silver, Is.Not.Null);
        Assert.That(checks.Bronze, Is.Not.Null);
    }
    [Test]
    public void FailsWithListOfAvailableChecksIfDefaultIsWrong()
    {
        var parser = new ConfigurationParser(Logger.None, new List<object>(){ Logger.None, new Check.AzurePAT("")});
        var tempDirectory = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);
        Directory.SetCurrentDirectory(tempDirectory);
        var checks = parser.LoadChecks();
        Assert.That(checks.Gold, Is.Not.Null);
        Assert.That(checks.Silver, Is.Not.Null);
        Assert.That(checks.Bronze, Is.Not.Null);
        
        var configJson = Path.Join(tempDirectory, "scorecard.config.json");
        var configJsonContents = File.ReadAllText(configJson);
        configJsonContents = configJsonContents.Replace("BuiltForAKS", "NonExistent");
        File.WriteAllText(configJson, configJsonContents);
        
        var exception = Assert.Throws<ArgumentException>(() => parser.LoadChecks())!;
        // ensure all checks are listed in exception message
        var availableChecks = typeof(BaseCheck).Assembly.GetTypes().Where(t => t.Name == "Check" && t.BaseType == typeof(BaseCheck)).Select(t => t.Namespace!);
        foreach (var check in availableChecks)
        {
            Assert.That(exception.Message, Does.Contain(check));
        }
        
    }
}