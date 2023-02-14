using Serilog;
namespace ScorecardGenerator.Test.Checks.BuiltForAKS;

public class HandlesAllScenarios
{
    private static void CreateFilesAndExpectResult(Action<string> createPipelineFiles, int expectedDeductionCount, int expectedFinalScore)
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new ScorecardGenerator.Checks.BuiltForAKS.Check(logger);
        
        var randomDirectoryName = Guid.NewGuid().ToString();
        try
        {
            var tempDirectory = Path.Join(Path.GetTempPath(), randomDirectoryName);
            Directory.CreateDirectory(tempDirectory);
            
            var projectFile = Path.Join(tempDirectory, "test.csproj");
            using var projectFileStream = File.Create(projectFile);
            createPipelineFiles(tempDirectory);
            
            var deductions = check.SetupLoggerAndRun(Path.GetTempPath(), randomDirectoryName);
            if (deductions.Any())
            {
                TestContext.WriteLine($"Occured deductions: {Environment.NewLine}{string.Join(Environment.NewLine, deductions)}");
            }
            Assert.That(deductions, Has.Count.EqualTo(expectedDeductionCount));
            Assert.That(deductions.CalculateFinalScore(), Is.EqualTo(expectedFinalScore));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            Directory.Delete(Path.Join(Path.GetTempPath(), randomDirectoryName), true);
        }
    }
    [Test]
    public void Returns100PointsForAzurePipelinesFile()
    {
        CreateFilesAndExpectResult(tempDirectory =>
        {
            var pipelineFile = Path.Join(tempDirectory, "azure-pipelines.yml");
            using var pipelineFileStream = File.Create(pipelineFile);
        }, 0, 100);
    }
    
    [Test]
    public void Returns100PointsForBuildPipelinesFile()
    {
        CreateFilesAndExpectResult(tempDirectory =>
        {
            var pipelineFile = Path.Join(tempDirectory, "build-pipelines.yml");
            using var pipelineFileStream = File.Create(pipelineFile);
        }, 0, 100);
    }
    
    [Test]
    public void Returns0PointsForOnPremPipelinesFile()
    {
        CreateFilesAndExpectResult(tempDirectory =>
        {
            var pipelineFile = Path.Join(tempDirectory, "onprem-pipelines.yml");
            using var pipelineFileStream = File.Create(pipelineFile);
        }, 1, 0);
    }
    
    [Test]
    public void Returns75PointsFor5PipelineFiles()
    {
        CreateFilesAndExpectResult(tempDirectory =>
        {
            for (var i = 0; i < 5; i++)
            {
                var pipelineFile = Path.Join(tempDirectory, $"azure-pipelines{i}.yml");
                using var pipelineFileStream = File.Create(pipelineFile);
            }
        }, 5, 75);
    }
    
    [Test]
    public void Returns0PointsForNoPipelineFiles()
    {
        CreateFilesAndExpectResult(tempDirectory =>
        {
        }, 1, 0);
    }
    
    [Test]
    public void Returns95PointsForUnknownPipelineFiles()
    {
        CreateFilesAndExpectResult(tempDirectory =>
        {
            var pipelineFile = Path.Join(tempDirectory, "unknown-pipelines.yml");
            using var pipelineFileStream = File.Create(pipelineFile);
        }, 1, 95);
    }
}