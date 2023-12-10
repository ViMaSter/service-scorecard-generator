using System.Net;
using ScorecardGenerator.Checks.RemainingDependencyUpgrades;
using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.RemainingDependencyUpgrades;

public class HTTPS
{
    private class NeighboringDirectoryStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var directory = request.RequestUri?.Host.Split(".")[^2];
            var fileName = request.RequestUri?.AbsolutePath.Split("/").Last();
            
            var file = Path.Join(Directory.GetCurrentDirectory(), nameof(Checks), nameof(RemainingDependencyUpgrades), nameof(HTTPS), directory, fileName);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(file))
            };
            return Task.FromResult(response);
        }
    }
    
    [Test]
    [TestCase("git@ssh.dev.azure.com:v3/vimaster/ScorecardGenerator/TestService1")]
    [TestCase("vimaster@vs-ssh.visualstudio.com:v3/vimaster/ScorecardGenerator/TestService1")]
    [TestCase("https://dev.azure.com/vimaster/ScorecardGenerator/_git/TestService1")]
    [TestCase("https://vimaster.visualstudio.com/ScorecardGenerator/_git/TestService1")]
    public void Deducts20PointsIfOnePRIsOpenOnAzure(string gitRepo)
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new Check(logger, new Check.AzurePAT(""),  new Check.GitHubPAT(""), new NeighboringDirectoryStub());
        var subdirectory = Guid.NewGuid().ToString();
        var source = Path.Join(Directory.GetCurrentDirectory(), nameof(Checks), nameof(RemainingDependencyUpgrades), nameof(HTTPS), "_git");
        var target = Path.Join(Path.GetTempPath(), subdirectory, ".git");
        foreach (var sourceFilePath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
        {
            var relativeDirectory = Path.GetDirectoryName(sourceFilePath)!.Replace(source, "");
            Directory.CreateDirectory(Path.Join(target, relativeDirectory));
            var content = File.ReadAllText(sourceFilePath);
            content = content.Replace("{{URL}}", gitRepo);
            File.WriteAllText(Path.Join(target, relativeDirectory, Path.GetFileName(sourceFilePath)), content);
        }

        var deductions = check.SetupLoggerAndRun(Path.Join(Path.GetTempPath(), subdirectory, "Cik.Magazine.CategoryService.csproj"));
        deductions.CountAndFinalScore(1, 80);
    }
    
    [Test]
    [TestCase("https://github.com/ScorecardGenerator/TestService1.git")]
    [TestCase("git@github.com:ScorecardGenerator/TestService1.git")]
    public void Deducts20PointsIfOnePRIsOpenOnGitHub(string gitRepo)
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new Check(logger, new Check.AzurePAT(""),  new Check.GitHubPAT(""), new NeighboringDirectoryStub());
        var subdirectory = Guid.NewGuid().ToString();
        var source = Path.Join(Directory.GetCurrentDirectory(), nameof(Checks), nameof(RemainingDependencyUpgrades), nameof(HTTPS), "_git");
        var target = Path.Join(Path.GetTempPath(), subdirectory, ".git");
        foreach (var sourceFilePath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
        {
            var relativeDirectory = Path.GetDirectoryName(sourceFilePath)!.Replace(source, "");
            Directory.CreateDirectory(Path.Join(target, relativeDirectory));
            var content = File.ReadAllText(sourceFilePath);
            content = content.Replace("{{URL}}", gitRepo);
            File.WriteAllText(Path.Join(target, relativeDirectory, Path.GetFileName(sourceFilePath)), content);
        }

        var deductions = check.SetupLoggerAndRun(Path.Join(Path.GetTempPath(), subdirectory, "Cik.Magazine.ProcessManager.csproj"));
        deductions.CountAndFinalScore(1, 80);
    }
    
    [Test]
    public void Deducts0PointsIfNoGitPathIsFound()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new Check(logger, new Check.AzurePAT(""), new Check.GitHubPAT(""), new NeighboringDirectoryStub());
        var subdirectory = Guid.NewGuid().ToString();
        var target = Path.Join(Path.GetTempPath(), subdirectory);
        Directory.CreateDirectory(target);
        
        var deductions = check.SetupLoggerAndRun(Path.Join(Path.GetTempPath(), subdirectory, "Cik.Magazine.ProcessManager.csproj"));
        deductions.CountAndFinalScore(1, 0);
    }
}