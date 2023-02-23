using System.Reflection;

namespace ScorecardGenerator.Test.Checks.ProperDockerfile;

public class TestWithNeighboringCsprojAndDockerfileFixture : TestWithNeighboringFixture
{
    [SetUp]
    public void SetupWithDockerfile()
    {
        Setup();
        
        var resourceName = GetType().FullName;
        var assembly = Assembly.GetExecutingAssembly();
        var resourceStream = assembly.GetManifestResourceStream(resourceName+".Dockerfile");
        
        var tempDirectory = Path.Join(WorkingDirectory, RelativePathToServiceRoot);
        Directory.CreateDirectory(tempDirectory);
        var tempFile = Path.Join(tempDirectory, "Dockerfile");
        using var fileStream = File.Create(tempFile);
        resourceStream!.CopyTo(fileStream);
    }
}