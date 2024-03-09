using ScorecardGenerator.Checks.BuiltForAKS;
using ScorecardGenerator.Test.Helper;
using Serilog;

namespace ScorecardGenerator.Test.Checks.BuiltForAKS;

public class HandlesAllScenarios
{
    private const string PROJECT_FILE_NAME = "test.csproj";
    private static void CreateFilesAndExpectResult(Action<string> createFiles, int expectedDeductionCount, int? expectedFinalScore)
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var check = new Check(logger);
        
        var randomDirectoryName = Guid.NewGuid().ToString();
        try
        {
            var tempDirectory = Path.Join(Path.GetTempPath(), randomDirectoryName);
            Directory.CreateDirectory(tempDirectory);
            {
                createFiles(tempDirectory);
            }
            
            var deductions = check.SetupLoggerAndRun(Path.Join(Path.GetTempPath(), randomDirectoryName, PROJECT_FILE_NAME));
            deductions.CountAndFinalScore(expectedDeductionCount, expectedFinalScore);
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
            var projectFile = Path.Join(tempDirectory, PROJECT_FILE_NAME);
            using var projectFileStream = File.Create(projectFile);
            projectFileStream.Write("<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>"u8);
            
            var pipelineFile = Path.Join(tempDirectory, "azure-pipelines.yml");
            using var pipelineFileStream = File.Create(pipelineFile);
        }, 0, 100);
    }
    
    [Test]
    public void Returns100PointsForBuildPipelinesFile()
    {
        CreateFilesAndExpectResult(tempDirectory =>
        {
            var projectFile = Path.Join(tempDirectory, PROJECT_FILE_NAME);
            using var projectFileStream = File.Create(projectFile);
            projectFileStream.Write("<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>"u8);
            
            var pipelineFile = Path.Join(tempDirectory, "build-pipelines.yml");
            using var pipelineFileStream = File.Create(pipelineFile);
        }, 0, 100);
    }
    
    [Test]
    public void Returns0PointsForOnPremPipelinesFile()
    {
        CreateFilesAndExpectResult(tempDirectory =>
        {
            var projectFile = Path.Join(tempDirectory, PROJECT_FILE_NAME);
            using var projectFileStream = File.Create(projectFile);
            projectFileStream.Write("<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>"u8);
            
            var pipelineFile = Path.Join(tempDirectory, "onprem-pipelines.yml");
            using var pipelineFileStream = File.Create(pipelineFile);
        }, 1, 0);
    }
    
    [Test]
    public void Returns75PointsFor5PipelineFiles()
    {
        CreateFilesAndExpectResult(tempDirectory =>
        {
            var projectFile = Path.Join(tempDirectory, PROJECT_FILE_NAME);
            using var projectFileStream = File.Create(projectFile);
            projectFileStream.Write("<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>"u8);
            
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
            var projectFile = Path.Join(tempDirectory, PROJECT_FILE_NAME);
            using var projectFileStream = File.Create(projectFile);
            projectFileStream.Write("<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>"u8);
        }, 1, 0);
    }
    
    [Test]
    public void Returns0PointsForMissingSDK()
    {
        CreateFilesAndExpectResult(tempDirectory =>
        {
            var projectFile = Path.Join(tempDirectory, PROJECT_FILE_NAME);
            using var projectFileStream = File.Create(projectFile);
            projectFileStream.Write("<Project></Project>"u8);
        }, 1, null);
    }
    
    [Test]
    public void ReturnsNullForProjectFileWithSkippedSDK()
    {
        CreateFilesAndExpectResult(tempDirectory =>
        {
            var projectFile = Path.Join(tempDirectory, PROJECT_FILE_NAME);
            using var projectFileStream = File.Create(projectFile);
            projectFileStream.Write("<Project Sdk=\"Microsoft.NET.Sdk\"></Project>"u8);
        }, 1, null);
    }
    
    [Test]
    public void Returns95PointsForUnknownPipelineFiles()
    {
        CreateFilesAndExpectResult(tempDirectory =>
        {
            var projectFile = Path.Join(tempDirectory, PROJECT_FILE_NAME);
            using var projectFileStream = File.Create(projectFile);
            projectFileStream.Write("<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>"u8);
            
            var pipelineFile = Path.Join(tempDirectory, "unknown-pipelines.yml");
            using var pipelineFileStream = File.Create(pipelineFile);
        }, 1, 95);
    }
}