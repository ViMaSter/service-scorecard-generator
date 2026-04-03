using System.Net;
using System.Text.RegularExpressions;
using ScorecardGenerator.Checks.PendingRenovateAzurePRs;
using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.PendingRenovateAzurePRs;

public class HTTPS
{
    private class NeighboringDirectoryStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var directory = request.RequestUri != null && request.RequestUri.Host.Contains("dev.azure") ? "azure" : "visualstudio";
            var fileName = request.RequestUri?.AbsolutePath.Split("/").Last();
            
            var file = Path.Join(Directory.GetCurrentDirectory(), nameof(Checks), nameof(PendingRenovateAzurePRs), nameof(HTTPS), directory, fileName);
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
    public void Deducts20PointsIfOnePRIsOpen(string gitRepo)
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.PendingRenovateAzurePRs.Check(logger, new Check.AzurePAT(""), new NeighboringDirectoryStub());
        var subdirectory = Guid.NewGuid().ToString();
        var source = Path.Join(Directory.GetCurrentDirectory(), nameof(Checks), nameof(PendingRenovateAzurePRs), nameof(HTTPS), "_git");
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
    public void Deducts0PointsIfNoGitPathIsFound()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.PendingRenovateAzurePRs.Check(logger, new Check.AzurePAT(""), new NeighboringDirectoryStub());
        var subdirectory = Guid.NewGuid().ToString();
        var target = Path.Join(Path.GetTempPath(), subdirectory);
        Directory.CreateDirectory(target);
        
        var deductions = check.SetupLoggerAndRun(Path.Join(Path.GetTempPath(), subdirectory, "Cik.Magazine.CategoryService.csproj"));
        deductions.CountAndFinalScore(1, 0);
    }
}